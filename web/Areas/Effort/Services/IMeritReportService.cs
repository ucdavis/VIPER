using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for merit and promotion report generation.
/// </summary>
public interface IMeritReportService
{
    /// <summary>
    /// Generate a merit detail report for a single term.
    /// Shows course-level effort data grouped by department and instructor.
    /// </summary>
    Task<MeritDetailReport> GetMeritDetailReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a merit detail report for an academic year (e.g., "2024-2025").
    /// </summary>
    Task<MeritDetailReport> GetMeritDetailReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a merit average report for a single term.
    /// Shows instructor effort totals grouped by job group and department with averages.
    /// </summary>
    Task<MeritAverageReport> GetMeritAverageReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a merit average report for an academic year (e.g., "2024-2025").
    /// </summary>
    Task<MeritAverageReport> GetMeritAverageReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a PDF document from a merit detail report.
    /// One landscape page per department; courses listed per instructor with
    /// instructor-totals and department-totals rows.
    /// </summary>
    Task<byte[]> GenerateMeritDetailPdfAsync(MeritDetailReport report);

    /// <summary>
    /// Generate a PDF document from a merit average report.
    /// One legal-landscape page per (job group, department) pair; per-term
    /// effort by instructor with totals/averages summary rows.
    /// </summary>
    Task<byte[]> GenerateMeritAveragePdfAsync(MeritAverageReport report);

    /// <summary>
    /// Generate an Excel workbook from a merit detail report. One worksheet
    /// per department. Data isn't promoted to a structured table because
    /// course rows are interleaved with instructor-totals and
    /// department-totals rows.
    /// </summary>
    MemoryStream GenerateMeritDetailExcel(MeritDetailReport report);

    /// <summary>
    /// Generate an Excel workbook from a merit average report. One worksheet
    /// per (job group, department) pair. Per-term rows interleave with
    /// instructor-totals so no structured table is built.
    /// </summary>
    MemoryStream GenerateMeritAverageExcel(MeritAverageReport report);
}
