using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for multi-year merit and evaluation report generation.
/// Combines per-instructor merit activity across multiple years with evaluation data.
/// </summary>
public interface IMeritMultiYearService
{
    /// <summary>
    /// Generate a multi-year merit + evaluation report for a single instructor.
    /// Wraps sp_merit_multiyear, sp_instructor_evals_multiyear, sp_instructor_activity_exclude,
    /// sp_dept_activity_summary, and sp_dept_job_group_count.
    /// </summary>
    Task<MultiYearReport> GetMultiYearReportAsync(
        int personId,
        int startYear,
        int endYear,
        string? excludeClinicalTerms = null,
        string? excludeDidacticTerms = null,
        bool useAcademicYear = false,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a PDF document from a multi-year report.
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(MultiYearReport report);

    /// <summary>
    /// Get the min/max calendar years for an instructor's effort data.
    /// Used to populate year range dropdowns (matching legacy behavior).
    /// </summary>
    Task<InstructorYearRangeDto?> GetInstructorYearRangeAsync(int personId, CancellationToken ct = default);
}
