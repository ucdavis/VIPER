using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for percentage assignment operations.
/// </summary>
public interface IPercentageService
{
    /// <summary>
    /// Get all percentage assignments for a person.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of percentage assignments.</returns>
    Task<List<PercentageDto>> GetPercentagesForPersonAsync(int personId, CancellationToken ct = default);

    /// <summary>
    /// Get a single percentage assignment by ID.
    /// </summary>
    /// <param name="id">The percentage ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The percentage assignment or null if not found.</returns>
    Task<PercentageDto?> GetPercentageAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Create a new percentage assignment.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created percentage assignment.</returns>
    Task<PercentageDto> CreatePercentageAsync(CreatePercentageRequest request, CancellationToken ct = default);

    /// <summary>
    /// Update an existing percentage assignment.
    /// </summary>
    /// <param name="id">The percentage ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated percentage assignment or null if not found.</returns>
    Task<PercentageDto?> UpdatePercentageAsync(int id, UpdatePercentageRequest request, CancellationToken ct = default);

    /// <summary>
    /// Delete a percentage assignment.
    /// </summary>
    /// <param name="id">The percentage ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeletePercentageAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Get percentage assignments for a person within a date range.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="start">Start date (inclusive).</param>
    /// <param name="end">End date (inclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of percentage assignments that overlap with the date range.</returns>
    Task<List<PercentageDto>> GetPercentagesForPersonByDateRangeAsync(
        int personId, DateTime start, DateTime end, CancellationToken ct = default);

    /// <summary>
    /// Validate a percentage assignment before saving.
    /// </summary>
    /// <param name="request">The create request to validate.</param>
    /// <param name="excludeId">Optional percentage ID to exclude from overlap detection (for updates).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result with errors and warnings.</returns>
    Task<PercentageValidationResult> ValidatePercentageAsync(
        CreatePercentageRequest request, int? excludeId = null, CancellationToken ct = default);
}
