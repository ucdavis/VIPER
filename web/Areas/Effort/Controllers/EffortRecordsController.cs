using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for effort record CRUD operations.
/// </summary>
[Route("/api/effort/records")]
[Permission(Allow = $"{EffortPermissions.ViewDept},{EffortPermissions.ViewAllDepartments}")]
public class EffortRecordsController : BaseEffortController
{
    private readonly IEffortRecordService _recordService;
    private readonly IEffortPermissionService _permissionService;

    public EffortRecordsController(
        IEffortRecordService recordService,
        IEffortPermissionService permissionService,
        ILogger<EffortRecordsController> logger) : base(logger)
    {
        _recordService = recordService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Get a single effort record by ID.
    /// </summary>
    [HttpGet("{recordId:int}")]
    public async Task<ActionResult<InstructorEffortRecordDto>> GetRecord(int recordId, CancellationToken ct = default)
    {
        SetExceptionContext("recordId", recordId);

        var record = await _recordService.GetEffortRecordAsync(recordId, ct);
        if (record == null)
        {
            return NotFound($"Record {recordId} not found");
        }

        // Verify user can view this person's effort
        if (!await _permissionService.CanViewPersonEffortAsync(record.PersonId, record.TermCode, ct))
        {
            return NotFound($"Record {recordId} not found");
        }

        return Ok(record);
    }

    /// <summary>
    /// Create a new effort record.
    /// </summary>
    [HttpPost]
    [Permission(Allow = EffortPermissions.CreateEffort)]
    public async Task<ActionResult<object>> CreateRecord([FromBody] CreateEffortRecordRequest request, CancellationToken ct = default)
    {
        SetExceptionContext("personId", request.PersonId);
        SetExceptionContext("termCode", request.TermCode);
        SetExceptionContext("courseId", request.CourseId);

        // Verify authorization for person
        if (!await _permissionService.CanEditPersonEffortAsync(request.PersonId, request.TermCode, ct))
        {
            _logger.LogWarning("User not authorized to edit effort for person {PersonId} in term {TermCode}",
                request.PersonId, request.TermCode);
            return NotFound($"Person {request.PersonId} not found for term {request.TermCode}");
        }

        // Verify term is editable
        if (!await _recordService.CanEditTermAsync(request.TermCode, ct))
        {
            _logger.LogWarning("Term {TermCode} is not open for editing", request.TermCode);
            return BadRequest("This term is not open for editing");
        }

        try
        {
            var (record, warning) = await _recordService.CreateEffortRecordAsync(request, ct);

            _logger.LogInformation("Effort record created: {RecordId} for person {PersonId} in term {TermCode}",
                record.Id, request.PersonId, request.TermCode);

            return CreatedAtAction(nameof(GetRecord), new { recordId = record.Id }, new
            {
                record,
                warning
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create effort record: {Message}", LogSanitizer.SanitizeString(ex.Message));
            return BadRequest(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Database error creating effort record: {Message}",
                LogSanitizer.SanitizeString(ex.InnerException?.Message ?? ex.Message));
            return BadRequest("Failed to create effort record. Please check all field values are valid.");
        }
    }

    /// <summary>
    /// Update an existing effort record.
    /// </summary>
    [HttpPut("{recordId:int}")]
    [Permission(Allow = EffortPermissions.EditEffort)]
    public async Task<ActionResult<object>> UpdateRecord(int recordId, [FromBody] UpdateEffortRecordRequest request, CancellationToken ct = default)
    {
        SetExceptionContext("recordId", recordId);

        // Get record to check authorization
        var existingRecord = await _recordService.GetEffortRecordAsync(recordId, ct);
        if (existingRecord == null)
        {
            return NotFound($"Record {recordId} not found");
        }

        // Verify authorization for person
        if (!await _permissionService.CanEditPersonEffortAsync(existingRecord.PersonId, existingRecord.TermCode, ct))
        {
            _logger.LogWarning("User not authorized to edit effort for person {PersonId} in term {TermCode}",
                existingRecord.PersonId, existingRecord.TermCode);
            return NotFound($"Record {recordId} not found");
        }

        // Verify term is editable
        if (!await _recordService.CanEditTermAsync(existingRecord.TermCode, ct))
        {
            _logger.LogWarning("Term {TermCode} is not open for editing", existingRecord.TermCode);
            return BadRequest("This term is not open for editing");
        }

        try
        {
            var (record, warning) = await _recordService.UpdateEffortRecordAsync(recordId, request, ct);
            if (record == null)
            {
                return NotFound($"Record {recordId} not found");
            }

            _logger.LogInformation("Effort record updated: {RecordId}", recordId);

            return Ok(new
            {
                record,
                warning
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update effort record: {Message}", LogSanitizer.SanitizeString(ex.Message));
            return BadRequest(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Database error updating effort record: {Message}",
                LogSanitizer.SanitizeString(ex.InnerException?.Message ?? ex.Message));
            return BadRequest("Failed to update effort record. Please check all field values are valid.");
        }
    }

    /// <summary>
    /// Delete an effort record.
    /// </summary>
    [HttpDelete("{recordId:int}")]
    [Permission(Allow = EffortPermissions.DeleteEffort)]
    public async Task<ActionResult> DeleteRecord(int recordId, CancellationToken ct = default)
    {
        SetExceptionContext("recordId", recordId);

        // Get record to check authorization
        var existingRecord = await _recordService.GetEffortRecordAsync(recordId, ct);
        if (existingRecord == null)
        {
            return NotFound($"Record {recordId} not found");
        }

        // Verify authorization for person
        if (!await _permissionService.CanEditPersonEffortAsync(existingRecord.PersonId, existingRecord.TermCode, ct))
        {
            _logger.LogWarning("User not authorized to delete effort for person {PersonId} in term {TermCode}",
                existingRecord.PersonId, existingRecord.TermCode);
            return NotFound($"Record {recordId} not found");
        }

        // Verify term is editable
        if (!await _recordService.CanEditTermAsync(existingRecord.TermCode, ct))
        {
            _logger.LogWarning("Term {TermCode} is not open for editing", existingRecord.TermCode);
            return BadRequest("This term is not open for editing");
        }

        var deleted = await _recordService.DeleteEffortRecordAsync(recordId, ct);
        if (!deleted)
        {
            return NotFound($"Record {recordId} not found");
        }

        _logger.LogInformation("Effort record deleted: {RecordId}", recordId);
        return NoContent();
    }

    /// <summary>
    /// Get available courses for creating effort records.
    /// Returns courses grouped by those with existing effort records and all available courses.
    /// </summary>
    [HttpGet("available-courses")]
    public async Task<ActionResult<AvailableCoursesDto>> GetAvailableCourses(
        [FromQuery] int personId,
        [FromQuery] int termCode,
        CancellationToken ct = default)
    {
        SetExceptionContext("personId", personId);
        SetExceptionContext("termCode", termCode);

        // Verify authorization for person
        if (!await _permissionService.CanViewPersonEffortAsync(personId, termCode, ct))
        {
            _logger.LogWarning("User not authorized to view effort for person {PersonId} in term {TermCode}",
                personId, termCode);
            return NotFound($"Person {personId} not found for term {termCode}");
        }

        var courses = await _recordService.GetAvailableCoursesAsync(personId, termCode, ct);
        return Ok(courses);
    }

    /// <summary>
    /// Get active effort types for dropdown.
    /// </summary>
    [HttpGet("effort-types")]
    public async Task<ActionResult<List<EffortTypeOptionDto>>> GetEffortTypes(CancellationToken ct = default)
    {
        var effortTypes = await _recordService.GetEffortTypeOptionsAsync(ct);
        return Ok(effortTypes);
    }

    /// <summary>
    /// Get active roles for dropdown.
    /// </summary>
    [HttpGet("roles")]
    public async Task<ActionResult<List<RoleOptionDto>>> GetRoles(CancellationToken ct = default)
    {
        var roles = await _recordService.GetRoleOptionsAsync(ct);
        return Ok(roles);
    }

    /// <summary>
    /// Check if the current user can edit effort for the given term.
    /// Returns true if term is open or user has EditWhenClosed permission.
    /// </summary>
    [HttpGet("can-edit-term")]
    public async Task<ActionResult<bool>> CanEditTerm([FromQuery] int termCode, CancellationToken ct = default)
    {
        SetExceptionContext("termCode", termCode);

        var canEdit = await _recordService.CanEditTermAsync(termCode, ct);
        return Ok(canEdit);
    }

    /// <summary>
    /// Check if hours or weeks should be used for a given effort type and term.
    /// </summary>
    [HttpGet("uses-weeks")]
    public ActionResult<bool> UsesWeeks([FromQuery] string effortTypeId, [FromQuery] int termCode)
    {
        var usesWeeks = _recordService.UsesWeeks(effortTypeId, termCode);
        return Ok(usesWeeks);
    }
}
