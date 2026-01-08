using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for effort type operations (read-only).
/// </summary>
public interface IEffortTypeService
{
    /// <summary>
    /// Get all effort types, optionally filtered by active status.
    /// </summary>
    Task<List<EffortTypeDto>> GetEffortTypesAsync(bool? activeOnly = null, CancellationToken ct = default);

    /// <summary>
    /// Get a specific effort type by ID.
    /// </summary>
    Task<EffortTypeDto?> GetEffortTypeAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Get the distinct class values for grouping.
    /// </summary>
    Task<List<string>> GetEffortTypeClassesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get all instructors who have a specific effort type assigned.
    /// </summary>
    Task<InstructorsByTypeResponseDto?> GetInstructorsByTypeAsync(int typeId, CancellationToken ct = default);
}
