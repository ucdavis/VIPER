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
        IReadOnlyList<string>? departments = null,
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
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a PDF document from a teaching activity report
    /// (grouped layout, one page per department).
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(TeachingActivityReport report);

    /// <summary>
    /// Generate a PDF document from a teaching activity report in individual
    /// (per-instructor) layout.
    /// </summary>
    Task<byte[]> GenerateIndividualReportPdfAsync(TeachingActivityReport report);

    /// <summary>
    /// Generate an Excel workbook from a teaching activity report
    /// (grouped layout, one worksheet per department). Instructor-totals
    /// rows interleave the course rows so no single structured table is
    /// built.
    /// </summary>
    MemoryStream GenerateReportExcel(TeachingActivityReport report);

    /// <summary>
    /// Generate an Excel workbook from a teaching activity report
    /// (individual layout, one worksheet per instructor). Course rows are
    /// promoted to a structured Excel Table; the instructor-totals row stays
    /// outside it.
    /// </summary>
    MemoryStream GenerateIndividualReportExcel(TeachingActivityReport report);
}
