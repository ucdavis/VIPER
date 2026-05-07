using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for the scheduled clinical weeks report.
/// </summary>
public interface IClinicalScheduleService
{
    Task<ScheduledCliWeeksReport> GetScheduledCliWeeksReportAsync(
        int termCode,
        CancellationToken ct = default);

    Task<ScheduledCliWeeksReport> GetScheduledCliWeeksReportByYearAsync(
        string academicYear,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a PDF document from a scheduled clinical weeks report.
    /// Landscape for multi-term, portrait for single-term; instructor rows
    /// list services per term cell.
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(ScheduledCliWeeksReport report);

    /// <summary>
    /// Generate an Excel workbook from a scheduled clinical weeks report.
    /// Single worksheet, instructor rows promoted to a structured Excel Table.
    /// </summary>
    MemoryStream GenerateReportExcel(ScheduledCliWeeksReport report);
}
