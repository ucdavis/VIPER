using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for school summary report generation.
/// </summary>
public interface ISchoolSummaryService
{
    /// <summary>
    /// Generate a school-wide summary report for a single term.
    /// Aggregates all departments with faculty counts, effort totals, and averages.
    /// </summary>
    Task<SchoolSummaryReport> GetSchoolSummaryReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a school-wide summary report for an academic year (e.g., "2024-2025").
    /// </summary>
    Task<SchoolSummaryReport> GetSchoolSummaryReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a PDF document from a school summary report.
    /// Single landscape page; each department contributes a totals row + a
    /// faculty-count row + a faculty-with-CLI/averages row, followed by
    /// grand-total rows.
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(SchoolSummaryReport report);

    /// <summary>
    /// Generate an Excel workbook from a school summary report. Single
    /// worksheet. Data isn't promoted to a structured table because each
    /// department block emits totals + faculty-count + averages rows of
    /// differing shape.
    /// </summary>
    MemoryStream GenerateReportExcel(SchoolSummaryReport report);
}
