using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for effort type operations (read-only).
/// Available to users with ViewAllDepartments permission.
/// </summary>
[Route("/api/effort/types")]
[Permission(Allow = $"{EffortPermissions.Base},{EffortPermissions.ViewAllDepartments}")]
public class EffortTypesController : BaseEffortController
{
    private readonly IEffortTypeService _typeService;

    public EffortTypesController(
        IEffortTypeService typeService,
        ILogger<EffortTypesController> logger) : base(logger)
    {
        _typeService = typeService;
    }

    /// <summary>
    /// Get all effort types.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EffortTypeDto>>> GetEffortTypes(
        [FromQuery] bool? activeOnly,
        CancellationToken ct)
    {
        var types = await _typeService.GetEffortTypesAsync(activeOnly, ct);
        return Ok(types);
    }

    /// <summary>
    /// Get a specific effort type by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EffortTypeDto>> GetEffortType(int id, CancellationToken ct)
    {
        SetExceptionContext("effortTypeId", id);

        var type = await _typeService.GetEffortTypeAsync(id, ct);
        if (type == null)
        {
            _logger.LogWarning("Effort type not found: {Id}", id);
            return NotFound($"Effort type {id} not found");
        }

        return Ok(type);
    }

    /// <summary>
    /// Get distinct class values.
    /// </summary>
    [HttpGet("classes")]
    public async Task<ActionResult<IEnumerable<string>>> GetClasses(CancellationToken ct)
    {
        var classes = await _typeService.GetEffortTypeClassesAsync(ct);
        return Ok(classes);
    }

    /// <summary>
    /// Get all instructors who have a specific effort type assigned.
    /// </summary>
    [HttpGet("{id:int}/instructors")]
    public async Task<ActionResult<InstructorsByTypeResponseDto>> GetInstructorsByType(int id, CancellationToken ct)
    {
        SetExceptionContext("effortTypeId", id);

        var result = await _typeService.GetInstructorsByTypeAsync(id, ct);
        if (result == null)
        {
            _logger.LogWarning("Effort type not found: {Id}", id);
            return NotFound($"Effort type {id} not found");
        }

        return Ok(result);
    }
}
