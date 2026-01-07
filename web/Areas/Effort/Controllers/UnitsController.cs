using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for unit management in the Effort system.
/// All endpoints require ManageUnits permission.
/// </summary>
[Route("/api/effort/units")]
[Permission(Allow = EffortPermissions.ManageUnits)]
public class UnitsController : BaseEffortController
{
    private readonly IUnitService _unitService;
    private readonly IEffortPermissionService _permissionService;

    public UnitsController(
        IUnitService unitService,
        IEffortPermissionService permissionService,
        ILogger<UnitsController> logger) : base(logger)
    {
        _unitService = unitService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Get all units with usage counts.
    /// </summary>
    /// <param name="activeOnly">If true, only return active units.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UnitDto>>> GetUnits([FromQuery] bool activeOnly = false, CancellationToken ct = default)
    {
        var units = await _unitService.GetUnitsAsync(activeOnly, ct);
        return Ok(units);
    }

    /// <summary>
    /// Get a specific unit by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UnitDto>> GetUnit(int id, CancellationToken ct)
    {
        SetExceptionContext("unitId", id);

        var unit = await _unitService.GetUnitAsync(id, ct);

        if (unit == null)
        {
            _logger.LogWarning("Unit not found: {UnitId}", id);
            return NotFound($"Unit {id} not found");
        }

        return Ok(unit);
    }

    /// <summary>
    /// Create a new unit.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UnitDto>> CreateUnit([FromBody] CreateUnitRequest request, CancellationToken ct)
    {
        SetExceptionContext("unitName", request.Name);

        var modifiedBy = _permissionService.GetCurrentPersonId();

        try
        {
            var unit = await _unitService.CreateUnitAsync(request, modifiedBy, ct);
            _logger.LogInformation("Unit created: {UnitName} (ID: {UnitId}) by {ModifiedBy}", unit.Name, unit.Id, modifiedBy);
            return CreatedAtAction(nameof(GetUnit), new { id = unit.Id }, unit);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot create unit '{UnitName}': {Message}", request.Name, ex.Message);
            return Conflict(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Constraint failure while creating unit '{UnitName}'", request.Name);
            return Conflict("Unable to create unit due to a constraint violation.");
        }
    }

    /// <summary>
    /// Update an existing unit.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UnitDto>> UpdateUnit(int id, [FromBody] UpdateUnitRequest request, CancellationToken ct)
    {
        SetExceptionContext("unitId", id);
        SetExceptionContext("unitName", request.Name);

        var modifiedBy = _permissionService.GetCurrentPersonId();

        try
        {
            var unit = await _unitService.UpdateUnitAsync(id, request, modifiedBy, ct);

            if (unit == null)
            {
                _logger.LogWarning("Unit not found for update: {UnitId}", id);
                return NotFound($"Unit {id} not found");
            }

            _logger.LogInformation("Unit updated: {UnitName} (ID: {UnitId}) by {ModifiedBy}", unit.Name, unit.Id, modifiedBy);
            return Ok(unit);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot update unit {UnitId}: {Message}", id, ex.Message);
            return Conflict(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Constraint failure while updating unit {UnitId}", id);
            return Conflict("Unable to update unit due to a constraint violation.");
        }
    }

    /// <summary>
    /// Delete a unit. Only succeeds if no percentages reference it.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteUnit(int id, CancellationToken ct)
    {
        SetExceptionContext("unitId", id);

        try
        {
            var deleted = await _unitService.DeleteUnitAsync(id, ct);

            if (!deleted)
            {
                _logger.LogWarning("Cannot delete unit {UnitId}: unit has related percentages or not found", id);
                return BadRequest("Cannot delete unit: unit has related effort percentages or was not found");
            }

            _logger.LogInformation("Unit deleted: {UnitId}", id);
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Constraint failure while deleting unit {UnitId}", id);
            return BadRequest("Unable to delete unit due to related data.");
        }
    }

    /// <summary>
    /// Check if a unit can be deleted and get usage count.
    /// </summary>
    [HttpGet("{id:int}/can-delete")]
    public async Task<ActionResult<object>> CanDeleteUnit(int id, CancellationToken ct)
    {
        SetExceptionContext("unitId", id);

        var usageCount = await _unitService.GetUsageCountAsync(id, ct);
        var canDelete = usageCount == 0;

        return Ok(new { canDelete, usageCount });
    }
}
