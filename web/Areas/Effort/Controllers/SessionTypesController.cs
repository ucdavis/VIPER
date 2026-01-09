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
/// API controller for session type management in the Effort system.
/// All endpoints require ManageSessionTypes permission.
/// </summary>
[Route("/api/effort/session-types")]
[Permission(Allow = EffortPermissions.ManageSessionTypes)]
public class SessionTypesController : BaseEffortController
{
    private readonly ISessionTypeService _sessionTypeService;

    public SessionTypesController(
        ISessionTypeService sessionTypeService,
        ILogger<SessionTypesController> logger) : base(logger)
    {
        _sessionTypeService = sessionTypeService;
    }

    /// <summary>
    /// Get all session types with usage counts.
    /// </summary>
    /// <param name="activeOnly">If true, only return active session types.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SessionTypeDto>>> GetSessionTypes([FromQuery] bool activeOnly = false, CancellationToken ct = default)
    {
        var sessionTypes = await _sessionTypeService.GetSessionTypesAsync(activeOnly, ct);
        return Ok(sessionTypes);
    }

    /// <summary>
    /// Get a specific session type by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SessionTypeDto>> GetSessionType(string id, CancellationToken ct)
    {
        SetExceptionContext("sessionTypeId", id);

        var sessionType = await _sessionTypeService.GetSessionTypeAsync(id, ct);

        if (sessionType == null)
        {
            _logger.LogWarning("Session type not found: {SessionTypeId}", LogSanitizer.SanitizeString(id));
            return NotFound($"Session type '{id}' not found");
        }

        return Ok(sessionType);
    }

    /// <summary>
    /// Create a new session type.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SessionTypeDto>> CreateSessionType([FromBody] CreateSessionTypeRequest request, CancellationToken ct)
    {
        SetExceptionContext("sessionTypeId", request.Id);

        try
        {
            var sessionType = await _sessionTypeService.CreateSessionTypeAsync(request, ct);
            _logger.LogInformation("Session type created: {SessionTypeId} ({Description})",
                LogSanitizer.SanitizeString(sessionType.Id), LogSanitizer.SanitizeString(sessionType.Description));
            return CreatedAtAction(nameof(GetSessionType), new { id = sessionType.Id }, sessionType);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot create session type '{SessionTypeId}': {Message}",
                LogSanitizer.SanitizeString(request.Id), LogSanitizer.SanitizeString(ex.Message));
            return Conflict(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Constraint failure while creating session type '{SessionTypeId}'",
                LogSanitizer.SanitizeString(request.Id));
            return Conflict("Unable to create session type due to a constraint violation.");
        }
    }

    /// <summary>
    /// Update an existing session type.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<SessionTypeDto>> UpdateSessionType(string id, [FromBody] UpdateSessionTypeRequest request, CancellationToken ct)
    {
        SetExceptionContext("sessionTypeId", id);

        try
        {
            var sessionType = await _sessionTypeService.UpdateSessionTypeAsync(id, request, ct);

            if (sessionType == null)
            {
                _logger.LogWarning("Session type not found for update: {SessionTypeId}", LogSanitizer.SanitizeString(id));
                return NotFound($"Session type '{id}' not found");
            }

            _logger.LogInformation("Session type updated: {SessionTypeId} ({Description})",
                LogSanitizer.SanitizeString(sessionType.Id), LogSanitizer.SanitizeString(sessionType.Description));
            return Ok(sessionType);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot update session type {SessionTypeId}: {Message}",
                LogSanitizer.SanitizeString(id), LogSanitizer.SanitizeString(ex.Message));
            return Conflict(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Constraint failure while updating session type {SessionTypeId}",
                LogSanitizer.SanitizeString(id));
            return Conflict("Unable to update session type due to a constraint violation.");
        }
    }

    /// <summary>
    /// Delete a session type. Only succeeds if no records reference it.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteSessionType(string id, CancellationToken ct)
    {
        SetExceptionContext("sessionTypeId", id);

        try
        {
            var sessionType = await _sessionTypeService.GetSessionTypeAsync(id, ct);
            if (sessionType == null)
            {
                _logger.LogWarning("Session type not found for delete: {SessionTypeId}", LogSanitizer.SanitizeString(id));
                return NotFound($"Session type '{id}' not found");
            }

            if (!sessionType.CanDelete)
            {
                _logger.LogWarning("Cannot delete session type {SessionTypeId}: session type has {UsageCount} related records",
                    LogSanitizer.SanitizeString(id), sessionType.UsageCount);
                return Conflict($"Cannot delete session type: session type has {sessionType.UsageCount} related effort record(s).");
            }

            var deleted = await _sessionTypeService.DeleteSessionTypeAsync(id, ct);
            if (!deleted)
            {
                return Conflict("Unable to delete session type due to related data.");
            }

            _logger.LogInformation("Session type deleted: {SessionTypeId}", LogSanitizer.SanitizeString(id));
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Constraint failure while deleting session type {SessionTypeId}",
                LogSanitizer.SanitizeString(id));
            return Conflict("Unable to delete session type due to related data.");
        }
    }

    /// <summary>
    /// Check if a session type can be deleted and get usage count.
    /// </summary>
    [HttpGet("{id}/can-delete")]
    public async Task<ActionResult<CanDeleteResponse>> CanDeleteSessionType(string id, CancellationToken ct)
    {
        SetExceptionContext("sessionTypeId", id);

        var sessionType = await _sessionTypeService.GetSessionTypeAsync(id, ct);
        if (sessionType == null)
        {
            _logger.LogWarning("Session type not found for can-delete check: {SessionTypeId}", LogSanitizer.SanitizeString(id));
            return NotFound($"Session type '{id}' not found");
        }

        return Ok(new CanDeleteResponse { CanDelete = sessionType.CanDelete, UsageCount = sessionType.UsageCount });
    }
}
