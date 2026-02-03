using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for self-service course operations in the Effort system.
/// This controller is for instructors to search and import their own courses from Banner.
/// Requires VerifyEffort permission (self-service access).
/// </summary>
[Route("/api/effort/courses/self-service")]
[Permission(Allow = EffortPermissions.VerifyEffort)]
public class CoursesSelfServiceController : BaseEffortController
{
    private readonly ICourseService _courseService;
    private readonly IEffortPermissionService _permissionService;
    private readonly ICourseClassificationService _classificationService;

    public CoursesSelfServiceController(
        ICourseService courseService,
        IEffortPermissionService permissionService,
        ICourseClassificationService classificationService,
        ILogger<CoursesSelfServiceController> logger) : base(logger)
    {
        _courseService = courseService;
        _permissionService = permissionService;
        _classificationService = classificationService;
    }

    /// <summary>
    /// Search for Banner courses that the current user is an instructor for.
    /// This is for self-service import - only returns courses where the user is listed as instructor.
    /// Excludes DVM/VET courses which must be imported by staff.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<BannerCourseDto>>> SearchBannerCoursesForSelf(
        [FromQuery] int termCode,
        [FromQuery] string? subjCode = null,
        [FromQuery] string? crseNumb = null,
        [FromQuery] string? seqNumb = null,
        [FromQuery] string? crn = null,
        CancellationToken ct = default)
    {
        SetExceptionContext("termCode", termCode);

        if (string.IsNullOrWhiteSpace(subjCode) && string.IsNullOrWhiteSpace(crseNumb) &&
            string.IsNullOrWhiteSpace(seqNumb) && string.IsNullOrWhiteSpace(crn))
        {
            return BadRequest("At least one search parameter (subjCode, crseNumb, seqNumb, or crn) is required");
        }

        try
        {
            var allCourses = await _courseService.SearchBannerCoursesAsync(termCode, subjCode, crseNumb, seqNumb, crn, ct);

            // Filter out DVM/VET courses first (no DB call needed)
            var nonDvmCourses = allCourses
                .Where(course => !_classificationService.IsDvmCourse(course.SubjCode))
                .ToList();

            // Batch query: get all CRNs where user is instructor (3 queries total instead of 3N)
            var personId = _permissionService.GetCurrentPersonId();
            var courseCrns = nonDvmCourses.Select(c => c.Crn).ToList();
            var instructorCrns = await _courseService.GetInstructorCrnsAsync(termCode, personId, courseCrns, ct);

            // Filter in-memory using the HashSet (O(1) lookup per course)
            var filteredCourses = nonDvmCourses
                .Where(course => instructorCrns.Contains(course.Crn.Trim()))
                .ToList();

            return Ok(filteredCourses);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            if (ex.Message.Contains("UCDBanner") || ex.Message.Contains("OPENQUERY") || ex.Message.Contains("linked server"))
            {
                _logger.LogError(ex, "Banner linked server connection failed");
                return StatusCode(503, "Banner system is temporarily unavailable. Please try again later.");
            }

            if (ex.Message.Contains("Could not find stored procedure") || ex.Number == 2812)
            {
                _logger.LogError(ex, "Banner search stored procedure not found");
                return StatusCode(503, "Banner search is not configured. Please contact support.");
            }

            _logger.LogError(ex, "Database error during Banner course search");
            return StatusCode(500, "An error occurred while searching Banner. Please try again.");
        }
    }

    /// <summary>
    /// Import a course from Banner for self-service (instructors importing their own courses).
    /// This is idempotent - if the course already exists, it returns the existing course.
    /// DVM/VET courses cannot be imported by instructors.
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<ImportCourseForSelfResult>> ImportCourseForSelf(
        [FromBody] ImportCourseRequest request,
        CancellationToken ct = default)
    {
        if (request == null)
        {
            return BadRequest("Request body is required");
        }

        SetExceptionContext("termCode", request.TermCode);
        SetExceptionContext("crn", request.Crn);

        // Check term editability
        if (!await _permissionService.IsTermEditableAsync(request.TermCode, ct))
        {
            return BadRequest("Term is not open for editing");
        }

        // Fetch course from Banner to validate
        var bannerCourse = await _courseService.GetBannerCourseAsync(request.TermCode, request.Crn, ct);
        if (bannerCourse == null)
        {
            _logger.LogWarning("Course {Crn} not found in Banner for term {TermCode}",
                LogSanitizer.SanitizeId(request.Crn), request.TermCode);
            return NotFound($"Course with CRN {request.Crn} not found in Banner for term {request.TermCode}");
        }

        // Reject DVM/VET courses
        if (_classificationService.IsDvmCourse(bannerCourse.SubjCode))
        {
            _logger.LogWarning("Instructor attempted to import DVM/VET course {Crn}",
                LogSanitizer.SanitizeId(request.Crn));
            return BadRequest("DVM and VET courses cannot be imported by instructors. Please contact your department administrator.");
        }

        // Verify user is an instructor for this course
        var personId = _permissionService.GetCurrentPersonId();
        if (!await _courseService.IsInstructorForCourseAsync(request.TermCode, request.Crn, personId, ct))
        {
            _logger.LogWarning("User {PersonId} is not an instructor for course {Crn}",
                personId, LogSanitizer.SanitizeId(request.Crn));
            return BadRequest("You are not listed as an instructor for this course in Banner");
        }

        // Import the course (idempotent)
        var result = await _courseService.ImportCourseForSelfAsync(request.TermCode, request.Crn, request.Units, ct);

        if (!result.Success)
        {
            _logger.LogWarning("Failed to import course {Crn}: {Error}",
                LogSanitizer.SanitizeId(request.Crn), LogSanitizer.SanitizeString(result.Error ?? "Unknown error"));
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Self-service course import: {Crn} for term {TermCode}, WasExisting={WasExisting}",
            LogSanitizer.SanitizeId(request.Crn), request.TermCode, result.WasExisting);

        return Ok(result);
    }
}
