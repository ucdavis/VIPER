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
/// API controller for percentage assignment CRUD operations.
/// </summary>
[Route("/api/effort/percentages")]
[Permission(Allow = $"{EffortPermissions.ViewDept},{EffortPermissions.ViewAllDepartments}")]
public class PercentagesController : BaseEffortController
{
    private readonly IPercentageService _percentageService;
    private readonly IEffortPermissionService _permissionService;

    public PercentagesController(
        IPercentageService percentageService,
        IEffortPermissionService permissionService,
        ILogger<PercentagesController> logger) : base(logger)
    {
        _percentageService = percentageService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Get all percentage assignments for a person.
    /// </summary>
    [HttpGet("person/{personId:int}")]
    public async Task<ActionResult<List<PercentageDto>>> GetPercentagesForPerson(int personId, CancellationToken ct = default)
    {
        SetExceptionContext("personId", personId);

        _logger.LogDebug("Getting percentages for person {PersonId}", LogSanitizer.SanitizeId(personId.ToString()));

        if (!await _permissionService.CanViewPersonPercentagesAsync(personId, ct))
        {
            _logger.LogWarning("User not authorized to view percentages for person {PersonId}", LogSanitizer.SanitizeId(personId.ToString()));
            return NotFound($"Person {personId} not found");
        }

        var percentages = await _percentageService.GetPercentagesForPersonAsync(personId, ct);
        return Ok(percentages);
    }

    /// <summary>
    /// Get a single percentage assignment by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PercentageDto>> GetPercentage(int id, CancellationToken ct = default)
    {
        SetExceptionContext("id", id);

        _logger.LogDebug("Getting percentage {Id}", LogSanitizer.SanitizeId(id.ToString()));

        var percentage = await _percentageService.GetPercentageAsync(id, ct);
        if (percentage == null)
        {
            return NotFound($"Percentage {id} not found");
        }

        if (!await _permissionService.CanViewPersonPercentagesAsync(percentage.PersonId, ct))
        {
            _logger.LogWarning("User not authorized to view percentage {Id}", LogSanitizer.SanitizeId(id.ToString()));
            return NotFound($"Percentage {id} not found");
        }

        return Ok(percentage);
    }

    /// <summary>
    /// Create a new percentage assignment.
    /// </summary>
    [HttpPost]
    [Permission(Allow = EffortPermissions.EditInstructor)]
    public async Task<ActionResult<PercentageSaveResponse>> CreatePercentage([FromBody] CreatePercentageRequest request, CancellationToken ct = default)
    {
        SetExceptionContext("personId", request.PersonId);

        _logger.LogDebug("Creating percentage for person {PersonId}", LogSanitizer.SanitizeId(request.PersonId.ToString()));

        if (!await _permissionService.CanEditPersonPercentagesAsync(request.PersonId, ct))
        {
            _logger.LogWarning("User not authorized to edit percentages for person {PersonId}", LogSanitizer.SanitizeId(request.PersonId.ToString()));
            return NotFound($"Person {request.PersonId} not found");
        }

        var validation = await _percentageService.ValidatePercentageAsync(request, null, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Validation failed for percentage creation: {Errors}", string.Join(", ", validation.Errors));
            return BadRequest(validation.Errors);
        }

        try
        {
            var percentage = await _percentageService.CreatePercentageAsync(request, ct);

            _logger.LogInformation("Percentage created: {Id} for person {PersonId}", percentage.Id, request.PersonId);

            return CreatedAtAction(nameof(GetPercentage), new { id = percentage.Id }, new PercentageSaveResponse
            {
                Result = percentage,
                Warnings = validation.Warnings
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create percentage: {Message}", LogSanitizer.SanitizeString(ex.Message));
            return BadRequest(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            _logger.LogWarning(ex, "Database error creating percentage: {Message}",
                LogSanitizer.SanitizeString(innerMessage));

            // Provide user-friendly messages for common constraint violations
            var userMessage = ParseDbUpdateExceptionMessage(innerMessage);
            return BadRequest(userMessage);
        }
    }

    /// <summary>
    /// Validate a percentage assignment without saving.
    /// </summary>
    [HttpPost("validate")]
    [Permission(Allow = EffortPermissions.EditInstructor)]
    public async Task<ActionResult<PercentageValidationResult>> ValidatePercentage([FromBody] CreatePercentageRequest request, CancellationToken ct = default)
    {
        SetExceptionContext("personId", request.PersonId);

        _logger.LogDebug("Validating percentage for person {PersonId}", LogSanitizer.SanitizeId(request.PersonId.ToString()));

        if (!await _permissionService.CanEditPersonPercentagesAsync(request.PersonId, ct))
        {
            _logger.LogWarning("User not authorized to edit percentages for person {PersonId}", LogSanitizer.SanitizeId(request.PersonId.ToString()));
            return NotFound($"Person {request.PersonId} not found");
        }

        var validation = await _percentageService.ValidatePercentageAsync(request, null, ct);
        return Ok(validation);
    }

    /// <summary>
    /// Update an existing percentage assignment.
    /// </summary>
    [HttpPut("{id:int}")]
    [Permission(Allow = EffortPermissions.EditInstructor)]
    public async Task<ActionResult<PercentageSaveResponse>> UpdatePercentage(int id, [FromBody] UpdatePercentageRequest request, CancellationToken ct = default)
    {
        SetExceptionContext("id", id);

        _logger.LogDebug("Updating percentage {Id}", LogSanitizer.SanitizeId(id.ToString()));

        var existingPercentage = await _percentageService.GetPercentageAsync(id, ct);
        if (existingPercentage == null)
        {
            return NotFound($"Percentage {id} not found");
        }

        if (!await _permissionService.CanEditPersonPercentagesAsync(existingPercentage.PersonId, ct))
        {
            _logger.LogWarning("User not authorized to edit percentage {Id}", LogSanitizer.SanitizeId(id.ToString()));
            return NotFound($"Percentage {id} not found");
        }

        var validationRequest = new CreatePercentageRequest
        {
            PersonId = existingPercentage.PersonId,
            PercentAssignTypeId = request.PercentAssignTypeId,
            UnitId = request.UnitId,
            Modifier = request.Modifier,
            Comment = request.Comment,
            PercentageValue = request.PercentageValue,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Compensated = request.Compensated
        };

        var validation = await _percentageService.ValidatePercentageAsync(validationRequest, id, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Validation failed for percentage update: {Errors}", string.Join(", ", validation.Errors));
            return BadRequest(validation.Errors);
        }

        try
        {
            var percentage = await _percentageService.UpdatePercentageAsync(id, request, ct);
            if (percentage == null)
            {
                return NotFound($"Percentage {id} not found");
            }

            _logger.LogInformation("Percentage updated: {Id}", id);

            return Ok(new PercentageSaveResponse
            {
                Result = percentage,
                Warnings = validation.Warnings
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update percentage: {Message}", LogSanitizer.SanitizeString(ex.Message));
            return BadRequest(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            _logger.LogWarning(ex, "Database error updating percentage: {Message}",
                LogSanitizer.SanitizeString(innerMessage));

            // Provide user-friendly messages for common constraint violations
            var userMessage = ParseDbUpdateExceptionMessage(innerMessage);
            return BadRequest(userMessage);
        }
    }

    /// <summary>
    /// Delete a percentage assignment.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Permission(Allow = EffortPermissions.EditInstructor)]
    public async Task<ActionResult> DeletePercentage(int id, CancellationToken ct = default)
    {
        SetExceptionContext("id", id);

        _logger.LogDebug("Deleting percentage {Id}", LogSanitizer.SanitizeId(id.ToString()));

        var existingPercentage = await _percentageService.GetPercentageAsync(id, ct);
        if (existingPercentage == null)
        {
            return NotFound($"Percentage {id} not found");
        }

        if (!await _permissionService.CanEditPersonPercentagesAsync(existingPercentage.PersonId, ct))
        {
            _logger.LogWarning("User not authorized to delete percentage {Id}", LogSanitizer.SanitizeId(id.ToString()));
            return NotFound($"Percentage {id} not found");
        }

        var deleted = await _percentageService.DeletePercentageAsync(id, ct);
        if (!deleted)
        {
            return NotFound($"Percentage {id} not found");
        }

        _logger.LogInformation("Percentage deleted: {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Parse database exception messages into user-friendly error messages.
    /// SECURITY: Be careful not to expose sensitive internal schema details in user-facing messages.
    /// </summary>
    private static string ParseDbUpdateExceptionMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return "Failed to save changes. Please check all field values are valid.";
        }

        // FK constraint violations
        if (message.Contains("FK_Percentages_Units", StringComparison.OrdinalIgnoreCase))
        {
            return "The selected unit is invalid or no longer exists.";
        }
        if (message.Contains("FK_Percentages_PercentAssignTypes", StringComparison.OrdinalIgnoreCase))
        {
            return "The selected type is invalid or no longer exists.";
        }
        if (message.Contains("FK_Percentages_ModifiedBy", StringComparison.OrdinalIgnoreCase))
        {
            return "Unable to save changes. Your user account may not be properly configured. Please contact support.";
        }

        // Check constraint violations
        if (message.Contains("CK_Percentages_Percentage", StringComparison.OrdinalIgnoreCase))
        {
            return "Percentage must be between 0 and 100.";
        }
        if (message.Contains("CK_Percentages_DateRange", StringComparison.OrdinalIgnoreCase))
        {
            return "End date cannot be before start date.";
        }

        // String truncation
        if (message.Contains("String or binary data would be truncated", StringComparison.OrdinalIgnoreCase))
        {
            return "One of the text fields exceeds the maximum allowed length.";
        }

        return "Failed to save changes. Please check all field values are valid.";
    }
}
