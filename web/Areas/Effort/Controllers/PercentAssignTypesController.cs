using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for percent assignment type operations (read-only).
/// Available to users with ViewAllDepartments permission.
/// </summary>
[Route("/api/effort/percent-assign-types")]
[Permission(Allow = $"{EffortPermissions.Base},{EffortPermissions.ViewAllDepartments}")]
public class PercentAssignTypesController : BaseEffortController
{
    private readonly IPercentAssignTypeService _typeService;

    public PercentAssignTypesController(
        IPercentAssignTypeService typeService,
        ILogger<PercentAssignTypesController> logger) : base(logger)
    {
        _typeService = typeService;
    }

    /// <summary>
    /// Get all percent assignment types.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PercentAssignTypeDto>>> GetPercentAssignTypes(
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var types = await _typeService.GetPercentAssignTypesAsync(activeOnly, ct);
        return Ok(types);
    }

    /// <summary>
    /// Get a specific percent assignment type by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PercentAssignTypeDto>> GetPercentAssignType(int id, CancellationToken ct)
    {
        SetExceptionContext("percentAssignTypeId", id);

        var type = await _typeService.GetPercentAssignTypeAsync(id, ct);
        if (type == null)
        {
            _logger.LogWarning("Percent assignment type not found: {Id}", id);
            return NotFound($"Percent assignment type {id} not found");
        }

        return Ok(type);
    }

    /// <summary>
    /// Get distinct class values.
    /// </summary>
    [HttpGet("classes")]
    public async Task<ActionResult<IEnumerable<string>>> GetClasses(CancellationToken ct)
    {
        var classes = await _typeService.GetPercentAssignTypeClassesAsync(ct);
        return Ok(classes);
    }

    /// <summary>
    /// Get all instructors who have a specific percent assignment type assigned.
    /// </summary>
    [HttpGet("{id:int}/instructors")]
    public async Task<ActionResult<InstructorsByPercentAssignTypeResponseDto>> GetInstructorsByType(int id, CancellationToken ct)
    {
        SetExceptionContext("percentAssignTypeId", id);

        var result = await _typeService.GetInstructorsByTypeAsync(id, ct);
        if (result == null)
        {
            _logger.LogWarning("Percent assignment type not found: {Id}", id);
            return NotFound($"Percent assignment type {id} not found");
        }

        return Ok(result);
    }
}
