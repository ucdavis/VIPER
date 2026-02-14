using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for teaching activity report generation.
/// </summary>
public interface ITeachingActivityService
{
    /// <summary>
    /// Generate a grouped teaching activity report for a single term.
    /// Calls effort.sp_effort_general_report and transforms the results into
    /// a hierarchical structure grouped by department, instructor, and course.
    /// </summary>
    Task<TeachingActivityReport> GetTeachingActivityReportAsync(
        int termCode,
        string? department = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a grouped teaching activity report for an academic year (e.g., "2024-2025").
    /// Resolves term codes in the academic year range, calls the SP for each, and merges results.
    /// </summary>
    Task<TeachingActivityReport> GetTeachingActivityReportByYearAsync(
        string academicYear,
        string? department = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a PDF document from a teaching activity report.
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(TeachingActivityReport report);
}
