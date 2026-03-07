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
    /// </summary>
    Task<byte[]> GenerateMeritDetailPdfAsync(MeritDetailReport report);

    /// <summary>
    /// Generate a PDF document from a merit average report.
    /// </summary>
    Task<byte[]> GenerateMeritAveragePdfAsync(MeritAverageReport report);
}
