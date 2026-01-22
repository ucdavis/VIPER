using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for effort record CRUD operations.
/// </summary>
public interface IEffortRecordService
{
    /// <summary>
    /// Get a single effort record by ID.
    /// </summary>
    Task<InstructorEffortRecordDto?> GetEffortRecordAsync(int recordId, CancellationToken ct = default);

    /// <summary>
    /// Create a new effort record.
    /// </summary>
    /// <returns>The created record and any warning message (e.g., role coercion).</returns>
    Task<(InstructorEffortRecordDto Record, string? Warning)> CreateEffortRecordAsync(CreateEffortRecordRequest request, CancellationToken ct = default);

    /// <summary>
    /// Update an existing effort record.
    /// </summary>
    /// <returns>The updated record and any warning message (e.g., role coercion), or null if not found.</returns>
    Task<(InstructorEffortRecordDto? Record, string? Warning)> UpdateEffortRecordAsync(int recordId, UpdateEffortRecordRequest request, CancellationToken ct = default);

    /// <summary>
    /// Delete an effort record.
    /// </summary>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteEffortRecordAsync(int recordId, CancellationToken ct = default);

    /// <summary>
    /// Get available courses for creating effort records.
    /// Returns courses grouped by those with existing effort records and all available courses.
    /// </summary>
    Task<AvailableCoursesDto> GetAvailableCoursesAsync(int personId, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Get active effort types for dropdown.
    /// </summary>
    Task<List<EffortTypeOptionDto>> GetEffortTypeOptionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get active roles for dropdown.
    /// </summary>
    Task<List<RoleOptionDto>> GetRoleOptionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if the current user can edit effort for the given term.
    /// Returns true if term is open or user has EditWhenClosed permission.
    /// </summary>
    Task<bool> CanEditTermAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Determine if hours or weeks should be used for a given effort type and term.
    /// CLI uses weeks only when termCode >= ClinicalAsWeeksStartTermCode.
    /// </summary>
    bool UsesWeeks(string effortTypeId, int termCode);
}
