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
/// API controller for effort type management in the Effort system.
/// All endpoints require ManageEffortTypes permission.
/// </summary>
[Route("/api/effort/effort-types")]
[Permission(Allow = EffortPermissions.ManageEffortTypes)]
public class EffortTypesController : BaseEffortController
{
    private readonly IEffortTypeService _effortTypeService;

    public EffortTypesController(
        IEffortTypeService effortTypeService,
        ILogger<EffortTypesController> logger) : base(logger)
    {
        _effortTypeService = effortTypeService;
    }

    /// <summary>
    /// Get all effort types with usage counts.
    /// </summary>
    /// <param name="activeOnly">If true, only return active effort types.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EffortTypeDto>>> GetEffortTypes([FromQuery] bool activeOnly = false, CancellationToken ct = default)
    {
        var effortTypes = await _effortTypeService.GetEffortTypesAsync(activeOnly, ct);
        return Ok(effortTypes);
    }

    /// <summary>
    /// Get a specific effort type by ID.
    /// </summary>
    /// <remarks>
    /// Uses query parameter instead of path segment because effort type IDs can contain
    /// special characters like "/" (e.g., "D/L", "L/D") which are problematic in URL paths.
    /// </remarks>
    [HttpGet("by-id")]
    public async Task<ActionResult<EffortTypeDto>> GetEffortType([FromQuery] string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("id is required.");
        }

        SetExceptionContext("effortTypeId", id);

        var effortType = await _effortTypeService.GetEffortTypeAsync(id, ct);

        if (effortType == null)
        {
            _logger.LogWarning("Effort type not found: {EffortTypeId}", LogSanitizer.SanitizeString(id));
            return NotFound($"Effort type '{id}' not found");
        }

        return Ok(effortType);
    }

    /// <summary>
    /// Create a new effort type.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EffortTypeDto>> CreateEffortType([FromBody] CreateEffortTypeRequest request, CancellationToken ct)
    {
        SetExceptionContext("effortTypeId", request.Id);

        try
        {
            var effortType = await _effortTypeService.CreateEffortTypeAsync(request, ct);
            _logger.LogInformation("Effort type created: {EffortTypeId} ({Description})",
                LogSanitizer.SanitizeString(effortType.Id), LogSanitizer.SanitizeString(effortType.Description));
            return CreatedAtAction(nameof(GetEffortType), new { id = effortType.Id }, effortType);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot create effort type '{EffortTypeId}': {Message}",
                LogSanitizer.SanitizeString(request.Id), LogSanitizer.SanitizeString(ex.Message));
            return Conflict(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Constraint failure while creating effort type '{EffortTypeId}'",
                LogSanitizer.SanitizeString(request.Id));
            return Conflict("Unable to create effort type due to a constraint violation.");
        }
    }

    /// <summary>
    /// Update an existing effort type.
    /// </summary>
    /// <remarks>
    /// Uses query parameter instead of path segment because effort type IDs can contain
    /// special characters like "/" (e.g., "D/L", "L/D") which are problematic in URL paths.
    /// </remarks>
    [HttpPut("by-id")]
    public async Task<ActionResult<EffortTypeDto>> UpdateEffortType([FromQuery] string id, [FromBody] UpdateEffortTypeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("id is required.");
        }

        SetExceptionContext("effortTypeId", id);

        try
        {
            var effortType = await _effortTypeService.UpdateEffortTypeAsync(id, request, ct);

            if (effortType == null)
            {
                _logger.LogWarning("Effort type not found for update: {EffortTypeId}", LogSanitizer.SanitizeString(id));
                return NotFound($"Effort type '{id}' not found");
            }

            _logger.LogInformation("Effort type updated: {EffortTypeId} ({Description})",
                LogSanitizer.SanitizeString(effortType.Id), LogSanitizer.SanitizeString(effortType.Description));
            return Ok(effortType);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot update effort type {EffortTypeId}: {Message}",
                LogSanitizer.SanitizeString(id), LogSanitizer.SanitizeString(ex.Message));
            return Conflict(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Constraint failure while updating effort type {EffortTypeId}",
                LogSanitizer.SanitizeString(id));
            return Conflict("Unable to update effort type due to a constraint violation.");
        }
    }

    /// <summary>
    /// Delete an effort type. Only succeeds if no records reference it.
    /// </summary>
    /// <remarks>
    /// Uses query parameter instead of path segment because effort type IDs can contain
    /// special characters like "/" (e.g., "D/L", "L/D") which are problematic in URL paths.
    /// </remarks>
    [HttpDelete("by-id")]
    public async Task<ActionResult> DeleteEffortType([FromQuery] string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("id is required.");
        }

        SetExceptionContext("effortTypeId", id);

        try
        {
            var effortType = await _effortTypeService.GetEffortTypeAsync(id, ct);
            if (effortType == null)
            {
                _logger.LogWarning("Effort type not found for delete: {EffortTypeId}", LogSanitizer.SanitizeString(id));
                return NotFound($"Effort type '{id}' not found");
            }

            if (!effortType.CanDelete)
            {
                _logger.LogWarning("Cannot delete effort type {EffortTypeId}: effort type has {UsageCount} related records",
                    LogSanitizer.SanitizeString(id), effortType.UsageCount);
                return Conflict($"Cannot delete effort type: effort type has {effortType.UsageCount} related effort record(s).");
            }

            var deleted = await _effortTypeService.DeleteEffortTypeAsync(id, ct);
            if (!deleted)
            {
                return Conflict("Unable to delete effort type due to related data.");
            }

            _logger.LogInformation("Effort type deleted: {EffortTypeId}", LogSanitizer.SanitizeString(id));
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Constraint failure while deleting effort type {EffortTypeId}",
                LogSanitizer.SanitizeString(id));
            return Conflict("Unable to delete effort type due to related data.");
        }
    }

    /// <summary>
    /// Check if an effort type can be deleted and get usage count.
    /// </summary>
    /// <remarks>
    /// Uses query parameter instead of path segment because effort type IDs can contain
    /// special characters like "/" (e.g., "D/L", "L/D") which are problematic in URL paths.
    /// </remarks>
    [HttpGet("can-delete")]
    public async Task<ActionResult<CanDeleteResponse>> CanDeleteEffortType([FromQuery] string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("id is required.");
        }

        SetExceptionContext("effortTypeId", id);

        var effortType = await _effortTypeService.GetEffortTypeAsync(id, ct);
        if (effortType == null)
        {
            _logger.LogWarning("Effort type not found for can-delete check: {EffortTypeId}", LogSanitizer.SanitizeString(id));
            return NotFound($"Effort type '{id}' not found");
        }

        return Ok(new CanDeleteResponse { CanDelete = effortType.CanDelete, UsageCount = effortType.UsageCount });
    }
}
