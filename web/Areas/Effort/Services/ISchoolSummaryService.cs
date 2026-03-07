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
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(SchoolSummaryReport report);
}
