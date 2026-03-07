using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Exceptions;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for instructor operations in the Effort system.
/// </summary>
[Route("/api/effort/instructors")]
[Permission(Allow = $"{EffortPermissions.ViewDept},{EffortPermissions.ViewAllDepartments}")]
public class InstructorsController : BaseEffortController
{
    private readonly IInstructorService _instructorService;
    private readonly IEffortPermissionService _permissionService;

    public InstructorsController(
        IInstructorService instructorService,
        IEffortPermissionService permissionService,
        ILogger<InstructorsController> logger) : base(logger)
    {
        _instructorService = instructorService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Verifies the user is authorized to access an instructor by their department.
    /// Returns null if authorized, or a NotFound result if not.
    /// </summary>
    private async Task<(PersonDto? instructor, ActionResult? errorResult)> GetAuthorizedInstructorAsync(
        int personId, int termCode, CancellationToken ct)
    {
        var instructor = await _instructorService.GetInstructorAsync(personId, termCode, ct);
        if (instructor == null || !await _permissionService.CanViewDepartmentAsync(instructor.EffortDept, ct))
        {
            return (null, NotFound($"Instructor not found"));
        }
        return (instructor, null);
    }

    /// <summary>
    /// Get all instructors for a term, optionally filtered by department.
    /// Non-admin users are restricted to their authorized departments.
    /// When meritOnly is true, restricts to merit-eligible job groups.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PersonDto>>> GetInstructors(
        [FromQuery] int termCode = 0,
        [FromQuery] string? dept = null,
        [FromQuery] bool meritOnly = false,
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
            return Ok(Array.Empty<PersonDto>());
        }

        // For non-full-access users, filter to authorized departments
        if (!await _permissionService.HasFullAccessAsync(ct))
        {
            var authorizedDepts = await _permissionService.GetAuthorizedDepartmentsAsync(ct);
            if (authorizedDepts.Count == 0)
            {
                return Ok(Array.Empty<PersonDto>());
            }

            // If no dept specified, get instructors for all authorized departments in a single query
            if (string.IsNullOrEmpty(dept))
            {
                var allInstructors = await _instructorService.GetInstructorsByDepartmentsAsync(termCode, authorizedDepts, meritOnly, ct);
                return Ok(allInstructors);
            }
        }

        var instructors = await _instructorService.GetInstructorsAsync(termCode, dept, meritOnly, ct);
        return Ok(instructors);
    }

    /// <summary>
    /// Get a single instructor by person ID and term code.
    /// Non-admin users can only view instructors in their authorized departments.
    /// </summary>
    [HttpGet("{personId:int}")]
    public async Task<ActionResult<PersonDto>> GetInstructor(
        int personId,
        [FromQuery] int termCode,
        CancellationToken ct = default)
    {
        SetExceptionContext("personId", personId);
        SetExceptionContext("termCode", termCode);

        var (instructor, errorResult) = await GetAuthorizedInstructorAsync(personId, termCode, ct);
        if (errorResult != null) return errorResult;

        return Ok(instructor);
    }

    /// <summary>
    /// Search for possible instructors in AAUD who can be added to the term.
    /// Returns employees not already in the Effort system for this term.
    /// </summary>
    [HttpGet("search")]
    [Permission(Allow = EffortPermissions.ImportInstructor)]
    public async Task<ActionResult<IEnumerable<AaudPersonDto>>> SearchPossibleInstructors(
        [FromQuery] int termCode,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        SetExceptionContext("termCode", termCode);

        var instructors = await _instructorService.SearchPossibleInstructorsAsync(termCode, q, ct);

        // Filter to authorized departments for non-full-access users
        if (!await _permissionService.HasFullAccessAsync(ct))
        {
            var authorizedDepts = await _permissionService.GetAuthorizedDepartmentsAsync(ct);
            instructors = [.. instructors
                .Where(i => i.EffortDept != null && authorizedDepts.Contains(i.EffortDept, StringComparer.OrdinalIgnoreCase))];
        }

        return Ok(instructors);
    }

    /// <summary>
    /// Add an instructor to the Effort system for a term.
    /// </summary>
    [HttpPost]
    [Permission(Allow = EffortPermissions.ImportInstructor)]
    public async Task<ActionResult<PersonDto>> CreateInstructor(
        [FromBody] CreateInstructorRequest request,
        CancellationToken ct = default)
    {
        SetExceptionContext("personId", request.PersonId);
        SetExceptionContext("termCode", request.TermCode);

        // Check if instructor already exists
        if (await _instructorService.InstructorExistsAsync(request.PersonId, request.TermCode, ct))
        {
            _logger.LogWarning("Instructor {PersonId} already exists for term {TermCode}",
                request.PersonId, request.TermCode);
            return Conflict($"Instructor already exists for this term");
        }

        // Verify the user is authorized to add instructors to the resolved department
        var resolvedDept = await _instructorService.ResolveInstructorDepartmentAsync(request.PersonId, request.TermCode, ct);
        if (resolvedDept == null)
        {
            return NotFound("Person not found.");
        }

        if (!await _permissionService.CanViewDepartmentAsync(resolvedDept, ct))
        {
            _logger.LogWarning("User not authorized to add instructor to department {Dept}", resolvedDept);
            return BadRequest("Not authorized to add instructors to this department");
        }

        try
        {
            var instructor = await _instructorService.CreateInstructorAsync(request, ct);
            _logger.LogInformation("Instructor created: {PersonId} for term {TermCode}",
                request.PersonId, request.TermCode);
            return CreatedAtAction(
                nameof(GetInstructor),
                new { personId = instructor.PersonId, termCode = instructor.TermCode },
                instructor);
        }
        catch (InstructorAlreadyExistsException ex)
        {
            // Race condition: another request created the instructor between our pre-check and database write
            _logger.LogWarning(ex, "Instructor {PersonId} already exists for term {TermCode} (race condition)",
                ex.PersonId, ex.TermCode);
            return Conflict("Instructor already exists for this term");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error creating instructor: {Message}", LogSanitizer.SanitizeString(ex.Message));
            return BadRequest(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Database error creating instructor: {Message}",
                LogSanitizer.SanitizeString(ex.InnerException?.Message ?? ex.Message));
            return BadRequest("Failed to create instructor. Please check all values are valid.");
        }
    }

    /// <summary>
    /// Update an instructor's details.
    /// </summary>
    [HttpPut("{personId:int}")]
    [Permission(Allow = EffortPermissions.EditInstructor)]
    public async Task<ActionResult<PersonDto>> UpdateInstructor(
        int personId,
        [FromQuery] int termCode,
        [FromBody] UpdateInstructorRequest request,
        CancellationToken ct = default)
    {
        SetExceptionContext("personId", personId);
        SetExceptionContext("termCode", termCode);

        var (existingInstructor, errorResult) = await GetAuthorizedInstructorAsync(personId, termCode, ct);
        if (errorResult != null) return errorResult;

        // If changing department, verify user can access target department
        if (existingInstructor != null &&
            !string.Equals(existingInstructor.EffortDept, request.EffortDept, StringComparison.OrdinalIgnoreCase) &&
            !await _permissionService.CanViewDepartmentAsync(request.EffortDept, ct))
        {
            return NotFound("Instructor not found");
        }

        try
        {
            var instructor = await _instructorService.UpdateInstructorAsync(personId, termCode, request, ct);
            if (instructor == null)
            {
                _logger.LogWarning("Instructor not found for update: {PersonId}", personId);
                return NotFound("Instructor not found");
            }

            _logger.LogInformation("Instructor updated: {PersonId} for term {TermCode}", personId, termCode);
            return Ok(instructor);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid instructor update data: {Message}", LogSanitizer.SanitizeString(ex.Message));
            return BadRequest(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Database error updating instructor: {Message}",
                LogSanitizer.SanitizeString(ex.InnerException?.Message ?? ex.Message));
            return BadRequest("Failed to update instructor. Please check all values are valid.");
        }
    }

    /// <summary>
    /// Delete an instructor and all associated effort records.
    /// </summary>
    [HttpDelete("{personId:int}")]
    [Permission(Allow = EffortPermissions.DeleteInstructor)]
    public async Task<ActionResult> DeleteInstructor(
        int personId,
        [FromQuery] int termCode,
        CancellationToken ct = default)
    {
        SetExceptionContext("personId", personId);
        SetExceptionContext("termCode", termCode);

        var (_, errorResult) = await GetAuthorizedInstructorAsync(personId, termCode, ct);
        if (errorResult != null) return errorResult;

        var deleted = await _instructorService.DeleteInstructorAsync(personId, termCode, ct);
        if (!deleted)
        {
            _logger.LogWarning("Instructor not found for delete: {PersonId}", personId);
            return NotFound("Instructor not found");
        }

        _logger.LogInformation("Instructor deleted: {PersonId} for term {TermCode}", personId, termCode);
        return NoContent();
    }

    /// <summary>
    /// Check if an instructor can be deleted and get the count of associated effort records.
    /// </summary>
    [HttpGet("{personId:int}/can-delete")]
    [Permission(Allow = EffortPermissions.DeleteInstructor)]
    public async Task<ActionResult<object>> CanDeleteInstructor(
        int personId,
        [FromQuery] int termCode,
        CancellationToken ct = default)
    {
        SetExceptionContext("personId", personId);
        SetExceptionContext("termCode", termCode);

        var (_, errorResult) = await GetAuthorizedInstructorAsync(personId, termCode, ct);
        if (errorResult != null) return errorResult;

        var (canDelete, recordCount) = await _instructorService.CanDeleteInstructorAsync(personId, termCode, ct);
        return Ok(new { canDelete, recordCount });
    }

    /// <summary>
    /// Get the list of valid department codes with grouping.
    /// Non-admin users only see their authorized departments.
    /// </summary>
    [HttpGet("departments")]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments(CancellationToken ct = default)
    {
        var allDepartments = _instructorService.GetDepartments();

        // Full access users see all departments
        if (await _permissionService.HasFullAccessAsync(ct))
        {
            return Ok(allDepartments);
        }

        // Non-admin users only see their authorized departments
        var authorizedDepts = await _permissionService.GetAuthorizedDepartmentsAsync(ct);
        var filteredDepts = allDepartments
            .Where(d => authorizedDepts.Contains(d.Code, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return Ok(filteredDepts);
    }

    /// <summary>
    /// Get all report units for the multi-select dropdown.
    /// </summary>
    [HttpGet("report-units")]
    public async Task<ActionResult<IEnumerable<ReportUnitDto>>> GetReportUnits(CancellationToken ct = default)
    {
        var units = await _instructorService.GetReportUnitsAsync(ct);
        return Ok(units);
    }

    /// <summary>
    /// Get effort records for an instructor in a specific term.
    /// </summary>
    [HttpGet("{personId:int}/effort")]
    public async Task<ActionResult<IEnumerable<InstructorEffortRecordDto>>> GetInstructorEffortRecords(
        int personId,
        [FromQuery] int termCode,
        CancellationToken ct = default)
    {
        SetExceptionContext("personId", personId);
        SetExceptionContext("termCode", termCode);

        var (_, errorResult) = await GetAuthorizedInstructorAsync(personId, termCode, ct);
        if (errorResult != null) return errorResult;

        var records = await _instructorService.GetInstructorEffortRecordsAsync(personId, termCode, ct);
        return Ok(records);
    }

    /// <summary>
    /// Get all title codes from the dictionary database for the dropdown.
    /// </summary>
    [HttpGet("title-codes")]
    public async Task<ActionResult<IEnumerable<TitleCodeDto>>> GetTitleCodes(CancellationToken ct = default)
    {
        var titleCodes = await _instructorService.GetTitleCodesAsync(ct);
        return Ok(titleCodes);
    }

    /// <summary>
    /// Get job groups currently in use, optionally filtered by term and department.
    /// </summary>
    [HttpGet("job-groups")]
    public async Task<ActionResult<IEnumerable<JobGroupDto>>> GetJobGroups(
        [FromQuery] int? termCode = null,
        [FromQuery] string? department = null,
        CancellationToken ct = default)
    {
        var jobGroups = await _instructorService.GetJobGroupsAsync(termCode, department, ct);
        return Ok(jobGroups);
    }
}
