using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for effort type operations.
/// </summary>
public interface IEffortTypeService
{
    /// <summary>
    /// Get all effort types with usage counts.
    /// </summary>
    /// <param name="activeOnly">If true, only return active effort types.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<EffortTypeDto>> GetEffortTypesAsync(bool activeOnly = false, CancellationToken ct = default);

    /// <summary>
    /// Get a single effort type by ID.
    /// </summary>
    Task<EffortTypeDto?> GetEffortTypeAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Create a new effort type.
    /// </summary>
    Task<EffortTypeDto> CreateEffortTypeAsync(CreateEffortTypeRequest request, CancellationToken ct = default);

    /// <summary>
    /// Update an existing effort type.
    /// </summary>
    Task<EffortTypeDto?> UpdateEffortTypeAsync(string id, UpdateEffortTypeRequest request, CancellationToken ct = default);

    /// <summary>
    /// Delete an effort type. Only succeeds if no records reference it.
    /// </summary>
    Task<bool> DeleteEffortTypeAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Check if an effort type can be deleted (no records reference it).
    /// </summary>
    Task<bool> CanDeleteEffortTypeAsync(string id, CancellationToken ct = default);
}
