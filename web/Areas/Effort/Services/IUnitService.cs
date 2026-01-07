using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for unit operations.
/// </summary>
public interface IUnitService
{
    /// <summary>
    /// Get all units with usage counts.
    /// </summary>
    /// <param name="activeOnly">If true, only return active units.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<UnitDto>> GetUnitsAsync(bool activeOnly = false, CancellationToken ct = default);

    /// <summary>
    /// Get a single unit by ID.
    /// </summary>
    Task<UnitDto?> GetUnitAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Create a new unit.
    /// </summary>
    Task<UnitDto> CreateUnitAsync(CreateUnitRequest request, int modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Update an existing unit.
    /// </summary>
    Task<UnitDto?> UpdateUnitAsync(int id, UpdateUnitRequest request, int modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Delete a unit. Only succeeds if no percentages reference it.
    /// </summary>
    Task<bool> DeleteUnitAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Check if a unit can be deleted (no percentages reference it).
    /// </summary>
    Task<bool> CanDeleteUnitAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Get the count of percentages referencing a unit.
    /// </summary>
    Task<int> GetUsageCountAsync(int id, CancellationToken ct = default);
}
