using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for department summary report generation.
/// </summary>
public interface IDeptSummaryService
{
    /// <summary>
    /// Generate a department summary report for a single term.
    /// Shows one row per instructor with effort type totals, plus department totals and averages.
    /// </summary>
    Task<DeptSummaryReport> GetDeptSummaryReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a department summary report for an academic year (e.g., "2024-2025").
    /// </summary>
    Task<DeptSummaryReport> GetDeptSummaryReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a PDF document from a department summary report.
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(DeptSummaryReport report);
}
