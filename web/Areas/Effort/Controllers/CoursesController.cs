using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for course operations in the Effort system.
/// </summary>
[Route("/api/effort/courses")]
[Permission(Allow = $"{EffortPermissions.ViewDept},{EffortPermissions.ViewAllDepartments}")]
public class CoursesController : BaseEffortController
{
    private readonly ICourseService _courseService;
    private readonly IEffortPermissionService _permissionService;

    public CoursesController(
        ICourseService courseService,
        IEffortPermissionService permissionService,
        ILogger<CoursesController> logger) : base(logger)
    {
        _courseService = courseService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Verifies the user is authorized to access a course by its department.
    /// Returns null if authorized, or a NotFound result if not.
    /// Note: Authorization for target departments in Create/Import/Update uses inline checks
    /// with different error responses to prevent department enumeration attacks.
    /// </summary>
    private async Task<(CourseDto? course, ActionResult? errorResult)> GetAuthorizedCourseAsync(int id, CancellationToken ct)
    {
        var course = await _courseService.GetCourseAsync(id, ct);
        if (course == null || !await _permissionService.CanViewDepartmentAsync(course.CustDept, ct))
        {
            return (null, NotFound($"Course {id} not found"));
        }
        return (course, null);
    }

    /// <summary>
    /// Get all courses for a term, optionally filtered by department.
    /// Non-admin users are restricted to their authorized departments.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses(
        [FromQuery] int termCode,
        [FromQuery] string? dept = null,
        CancellationToken ct = default)
    {
        SetExceptionContext("termCode", termCode);
        if (!string.IsNullOrEmpty(dept))
        {
            SetExceptionContext("dept", dept);
        }

        // If department specified, verify user can view it
        if (!string.IsNullOrEmpty(dept) && !await _permissionService.CanViewDepartmentAsync(dept, ct))
        {
            // Return empty list instead of 403 to prevent department enumeration
            return Ok(Array.Empty<CourseDto>());
        }

        // For non-full-access users, filter to authorized departments
        if (!await _permissionService.HasFullAccessAsync(ct))
        {
            var authorizedDepts = await _permissionService.GetAuthorizedDepartmentsAsync(ct);
            if (authorizedDepts.Count == 0)
            {
                return Ok(Array.Empty<CourseDto>());
            }

            // If no dept specified, get courses for all authorized departments
            if (string.IsNullOrEmpty(dept))
            {
                var allCourses = new List<CourseDto>();
                foreach (var authorizedDept in authorizedDepts)
                {
                    var deptCourses = await _courseService.GetCoursesAsync(termCode, authorizedDept, ct);
                    allCourses.AddRange(deptCourses);
                }
                return Ok(allCourses.OrderBy(c => c.SubjCode).ThenBy(c => c.CrseNumb));
            }
        }

        var courses = await _courseService.GetCoursesAsync(termCode, dept, ct);
        return Ok(courses);
    }

    /// <summary>
    /// Get a single course by ID.
    /// Non-admin users can only view courses in their authorized departments.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseDto>> GetCourse(int id, CancellationToken ct = default)
    {
        SetExceptionContext("courseId", id);

        var course = await _courseService.GetCourseAsync(id, ct);
        if (course == null)
        {
            _logger.LogWarning("Course not found: {CourseId}", id);
            return NotFound($"Course {id} not found");
        }

        // Verify user can view the course's custodial department
        // Return 404 (not 403) to prevent enumeration attacks
        if (!await _permissionService.CanViewDepartmentAsync(course.CustDept, ct))
        {
            _logger.LogWarning("User not authorized to view course {CourseId} in department {Dept}", id, course.CustDept);
            return NotFound($"Course {id} not found");
        }

        return Ok(course);
    }

    /// <summary>
    /// Search for courses in Banner by subject code, course number, and/or CRN.
    /// </summary>
    [HttpGet("search")]
    [Permission(Allow = EffortPermissions.ImportCourse)]
    public async Task<ActionResult<IEnumerable<BannerCourseDto>>> SearchBannerCourses(
        [FromQuery] int termCode,
        [FromQuery] string? subjCode = null,
        [FromQuery] string? crseNumb = null,
        [FromQuery] string? crn = null,
        CancellationToken ct = default)
    {
        SetExceptionContext("termCode", termCode);

        if (string.IsNullOrWhiteSpace(subjCode) && string.IsNullOrWhiteSpace(crseNumb) && string.IsNullOrWhiteSpace(crn))
        {
            return BadRequest("At least one search parameter (subjCode, crseNumb, or crn) is required");
        }

        var courses = await _courseService.SearchBannerCoursesAsync(termCode, subjCode, crseNumb, crn, ct);
        return Ok(courses);
    }

    /// <summary>
    /// Import a course from Banner into the Effort system.
    /// </summary>
    [HttpPost("import")]
    [Permission(Allow = EffortPermissions.ImportCourse)]
    public async Task<ActionResult<CourseDto>> ImportCourse([FromBody] ImportCourseRequest request, CancellationToken ct = default)
    {
        SetExceptionContext("termCode", request.TermCode);
        SetExceptionContext("crn", request.Crn);

        // Validate: Course exists in Banner
        var bannerCourse = await _courseService.GetBannerCourseAsync(request.TermCode, request.Crn, ct);
        if (bannerCourse == null)
        {
            _logger.LogWarning("Course {Crn} not found in Banner for term {TermCode}", request.Crn, request.TermCode);
            return NotFound($"Course with CRN {request.Crn} not found in Banner for term {request.TermCode}");
        }

        // Validate: Units in range for variable-unit courses
        var isVariable = bannerCourse.UnitType == "V";
        decimal units;
        if (isVariable && request.Units.HasValue)
        {
            units = request.Units.Value;
            if (units < bannerCourse.UnitLow || units > bannerCourse.UnitHigh)
            {
                _logger.LogWarning("Units {Units} out of range [{Low}-{High}] for course {Crn}",
                    units, bannerCourse.UnitLow, bannerCourse.UnitHigh, request.Crn);
                return BadRequest($"Units {units} must be between {bannerCourse.UnitLow} and {bannerCourse.UnitHigh}");
            }
        }
        else
        {
            units = bannerCourse.UnitLow;
        }

        // Validate: Course not already imported with same units
        if (await _courseService.CourseExistsAsync(request.TermCode, request.Crn, units, ct))
        {
            _logger.LogWarning("Course {Crn} with {Units} units already exists for term {TermCode}",
                request.Crn, units, request.TermCode);
            return Conflict($"Course {bannerCourse.SubjCode} {bannerCourse.CrseNumb} with {units} units already exists for this term");
        }

        var targetDept = _courseService.GetCustodialDepartmentForBannerCode(bannerCourse.DeptCode);
        if (!await _permissionService.CanViewDepartmentAsync(targetDept, ct))
        {
            _logger.LogWarning("User not authorized for department {CustDept} when importing course {Crn}",
                targetDept, request.Crn);
            return NotFound($"Course with CRN {request.Crn} not found in Banner for term {request.TermCode}");
        }

        try
        {
            var course = await _courseService.ImportCourseFromBannerAsync(request, bannerCourse, ct);
            _logger.LogInformation("Course imported: {Crn} for term {TermCode}",
                request.Crn, request.TermCode);
            return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Database error importing course {Crn}: {Message}", request.Crn, ex.InnerException?.Message ?? ex.Message);
            return BadRequest("Failed to import course. Please check all field values are valid.");
        }
    }

    /// <summary>
    /// Manually create a course in the Effort system.
    /// </summary>
    [HttpPost]
    [Permission(Allow = EffortPermissions.EditCourse)]
    public async Task<ActionResult<CourseDto>> CreateCourse([FromBody] CreateCourseRequest request, CancellationToken ct = default)
    {
        SetExceptionContext("termCode", request.TermCode);
        SetExceptionContext("crn", request.Crn);

        // Validate: Custodial department is valid
        if (!_courseService.IsValidCustodialDepartment(request.CustDept))
        {
            _logger.LogWarning("Invalid custodial department: {CustDept}", request.CustDept);
            return BadRequest($"Invalid custodial department: {request.CustDept}");
        }

        if (!await _permissionService.CanViewDepartmentAsync(request.CustDept, ct))
        {
            _logger.LogWarning("User not authorized for department {CustDept}", request.CustDept);
            return BadRequest($"Invalid custodial department: {request.CustDept}");
        }

        // Validate: Course not already exists with same CRN and units
        if (await _courseService.CourseExistsAsync(request.TermCode, request.Crn, request.Units, ct))
        {
            _logger.LogWarning("Course {Crn} with {Units} units already exists for term {TermCode}",
                request.Crn, request.Units, request.TermCode);
            return Conflict($"Course with CRN {request.Crn} and {request.Units} units already exists for this term");
        }

        try
        {
            var course = await _courseService.CreateCourseAsync(request, ct);
            _logger.LogInformation("Course created manually: {SubjCode} {CrseNumb} for term {TermCode}",
                request.SubjCode, request.CrseNumb, request.TermCode);
            return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Database error creating course: {Message}", ex.InnerException?.Message ?? ex.Message);
            return BadRequest("Failed to save course. Please check all field values are valid.");
        }
    }

    /// <summary>
    /// Update an existing course (full update - requires EditCourse permission).
    /// </summary>
    [HttpPut("{id:int}")]
    [Permission(Allow = EffortPermissions.EditCourse)]
    public async Task<ActionResult<CourseDto>> UpdateCourse(int id, [FromBody] UpdateCourseRequest request, CancellationToken ct = default)
    {
        SetExceptionContext("courseId", id);

        var (existingCourse, errorResult) = await GetAuthorizedCourseAsync(id, ct);
        if (errorResult != null) return errorResult;

        if (existingCourse != null &&
            !string.Equals(existingCourse.CustDept, request.CustDept, StringComparison.OrdinalIgnoreCase) &&
            !await _permissionService.CanViewDepartmentAsync(request.CustDept, ct))
        {
            return NotFound($"Course {id} not found");
        }

        try
        {
            var course = await _courseService.UpdateCourseAsync(id, request, ct);
            if (course == null)
            {
                _logger.LogWarning("Course not found for update: {CourseId}", id);
                return NotFound($"Course {id} not found");
            }

            _logger.LogInformation("Course updated: {CourseId}", id);
            return Ok(course);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid course update data: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Database error updating course: {Message}", ex.InnerException?.Message ?? ex.Message);
            return BadRequest("Failed to update course. Please check all field values are valid.");
        }
    }

    /// <summary>
    /// Update only the enrollment for an R-course.
    /// This is a restricted endpoint for users with ManageRCourseEnrollment permission.
    /// </summary>
    [HttpPatch("{id:int}/enrollment")]
    [Permission(Allow = EffortPermissions.ManageRCourseEnrollment)]
    public async Task<ActionResult<CourseDto>> UpdateCourseEnrollment(int id, [FromBody] UpdateEnrollmentRequest request, CancellationToken ct = default)
    {
        SetExceptionContext("courseId", id);

        var (_, errorResult) = await GetAuthorizedCourseAsync(id, ct);
        if (errorResult != null) return errorResult;

        try
        {
            var course = await _courseService.UpdateCourseEnrollmentAsync(id, request.Enrollment, ct);
            if (course == null)
            {
                _logger.LogWarning("Course not found for enrollment update: {CourseId}", id);
                return NotFound($"Course {id} not found");
            }

            _logger.LogInformation("Course enrollment updated: {CourseId}", id);
            return Ok(course);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot update enrollment for course {CourseId}: {Message}", id, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Delete a course and all associated effort records.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Permission(Allow = EffortPermissions.DeleteCourse)]
    public async Task<ActionResult> DeleteCourse(int id, CancellationToken ct = default)
    {
        SetExceptionContext("courseId", id);

        var (_, errorResult) = await GetAuthorizedCourseAsync(id, ct);
        if (errorResult != null) return errorResult;

        var deleted = await _courseService.DeleteCourseAsync(id, ct);
        if (!deleted)
        {
            _logger.LogWarning("Course not found for delete: {CourseId}", id);
            return NotFound($"Course {id} not found");
        }

        _logger.LogInformation("Course deleted: {CourseId}", id);
        return NoContent();
    }

    /// <summary>
    /// Check if a course can be deleted and get the count of associated effort records.
    /// </summary>
    [HttpGet("{id:int}/can-delete")]
    [Permission(Allow = EffortPermissions.DeleteCourse)]
    public async Task<ActionResult<object>> CanDeleteCourse(int id, CancellationToken ct = default)
    {
        SetExceptionContext("courseId", id);

        var (_, errorResult) = await GetAuthorizedCourseAsync(id, ct);
        if (errorResult != null) return errorResult;

        var (canDelete, recordCount) = await _courseService.CanDeleteCourseAsync(id, ct);
        return Ok(new { canDelete, recordCount });
    }

    /// <summary>
    /// Get the list of valid custodial department codes.
    /// Non-admin users only see their authorized departments.
    /// </summary>
    [HttpGet("departments")]
    public async Task<ActionResult<IEnumerable<string>>> GetDepartments(CancellationToken ct = default)
    {
        var allDepartments = _courseService.GetValidCustodialDepartments();

        // Full access users see all departments
        if (await _permissionService.HasFullAccessAsync(ct))
        {
            return Ok(allDepartments);
        }

        // Non-admin users only see their authorized departments
        var authorizedDepts = await _permissionService.GetAuthorizedDepartmentsAsync(ct);
        var filteredDepts = allDepartments
            .Where(d => authorizedDepts.Contains(d, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return Ok(filteredDepts);
    }
}
