using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for merit and promotion summary report generation.
/// </summary>
public interface IMeritSummaryService
{
    /// <summary>
    /// Generate a merit summary report for a single term.
    /// Shows department-level totals and averages grouped by job group.
    /// </summary>
    Task<MeritSummaryReport> GetMeritSummaryReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a merit summary report for an academic year (e.g., "2024-2025").
    /// </summary>
    Task<MeritSummaryReport> GetMeritSummaryReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a PDF document from a merit summary report.
    /// One legal-landscape page per (job group, department) pair; each page
    /// holds totals + faculty-count + averages rows.
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(MeritSummaryReport report);

    /// <summary>
    /// Generate an Excel workbook from a merit summary report. One worksheet
    /// per (job group, department) pair. Sheets are summary-only so no
    /// structured table is built.
    /// </summary>
    MemoryStream GenerateReportExcel(MeritSummaryReport report);
}
