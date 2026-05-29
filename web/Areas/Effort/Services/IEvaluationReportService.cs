using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for instructor evaluation report generation.
/// Reports are sourced from the EvalHarvest database and grouped by department.
/// </summary>
public interface IEvaluationReportService
{
    /// <summary>
    /// Generate an evaluation summary report for a single term.
    /// Shows weighted average per instructor.
    /// </summary>
    Task<EvalSummaryReport> GetEvalSummaryReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate an evaluation summary report for an academic year (e.g., "2024-2025").
    /// </summary>
    Task<EvalSummaryReport> GetEvalSummaryReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate an evaluation detail report for a single term.
    /// Shows course-level data with averages and medians.
    /// </summary>
    Task<EvalDetailReport> GetEvalDetailReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate an evaluation detail report for an academic year (e.g., "2024-2025").
    /// </summary>
    Task<EvalDetailReport> GetEvalDetailReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a PDF document from an evaluation summary report.
    /// One page per department, instructor weighted averages followed by a
    /// department-average row.
    /// </summary>
    Task<byte[]> GenerateSummaryPdfAsync(EvalSummaryReport report);

    /// <summary>
    /// Generate a PDF document from an evaluation detail report.
    /// One landscape page per department, course-level rows per instructor
    /// followed by instructor-average and department-average summary rows.
    /// </summary>
    Task<byte[]> GenerateDetailPdfAsync(EvalDetailReport report);

    /// <summary>
    /// Generate an Excel workbook from an evaluation summary report.
    /// One worksheet per department; instructor rows are promoted to a
    /// structured Excel Table while the department-average row stays outside.
    /// </summary>
    MemoryStream GenerateEvalSummaryExcel(EvalSummaryReport report);

    /// <summary>
    /// Generate an Excel workbook from an evaluation detail report.
    /// One worksheet per department. Data isn't promoted to a structured
    /// table because instructor-average and department-average rows
    /// interleave the course detail rows.
    /// </summary>
    MemoryStream GenerateEvalDetailExcel(EvalDetailReport report);
}
