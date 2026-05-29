using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for percent assignment type operations (read-only).
/// </summary>
public interface IPercentAssignTypeService
{
    /// <summary>
    /// Get all percent assignment types, optionally filtered by active status.
    /// </summary>
    Task<List<PercentAssignTypeDto>> GetPercentAssignTypesAsync(bool activeOnly = false, CancellationToken ct = default);

    /// <summary>
    /// Get a specific percent assignment type by ID.
    /// </summary>
    Task<PercentAssignTypeDto?> GetPercentAssignTypeAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Get the distinct class values for grouping.
    /// </summary>
    Task<List<string>> GetPercentAssignTypeClassesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get all instructors who have a specific percent assignment type assigned.
    /// </summary>
    Task<InstructorsByPercentAssignTypeResponseDto?> GetInstructorsByTypeAsync(int typeId, CancellationToken ct = default);
}
