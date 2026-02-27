using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for Year Statistics ("Lairmore Report") generation.
/// </summary>
public interface IYearStatisticsService
{
    /// <summary>
    /// Generate the Year Statistics report for an academic year (e.g., "2024-2025").
    /// Returns all 4 sub-reports: SVM, DVM, Resident, Undergrad/Grad.
    /// </summary>
    Task<YearStatisticsReport> GetYearStatisticsReportAsync(
        string academicYear,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a PDF document from a year statistics report.
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(YearStatisticsReport report);
}
