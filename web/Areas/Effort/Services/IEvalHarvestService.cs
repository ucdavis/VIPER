using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for managing evaluation data from the evalHarvest database.
/// Handles CERE-imported evaluations and ad-hoc (manual) evaluations.
/// </summary>
public interface IEvalHarvestService
{
    /// <summary>
    /// Get evaluation status for all instructors on a course (and its children).
    /// Uses CRN + termCode to query evalHarvest.
    /// </summary>
    Task<CourseEvaluationStatusDto> GetCourseEvaluationStatusAsync(
        int courseId, CancellationToken ct = default);

    /// <summary>
    /// Create an ad-hoc evaluation record. Blocked if CERE data exists for the course.
    /// </summary>
    Task<AdHocEvalResultDto> CreateAdHocEvaluationAsync(
        CreateAdHocEvalRequest request, CancellationToken ct = default);

    /// <summary>
    /// Update an ad-hoc evaluation record. Blocked if CERE data exists for the course.
    /// Verifies the quant belongs to the specified course.
    /// </summary>
    Task<AdHocEvalResultDto> UpdateAdHocEvaluationAsync(
        int courseId, int quantId, UpdateAdHocEvalRequest request, CancellationToken ct = default);

    /// <summary>
    /// Delete an ad-hoc evaluation record. Blocked if CERE data exists for the course.
    /// Verifies the quant belongs to the specified course.
    /// </summary>
    Task<bool> DeleteAdHocEvaluationAsync(int courseId, int quantId, CancellationToken ct = default);
}
