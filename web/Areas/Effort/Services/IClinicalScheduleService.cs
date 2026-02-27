using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

public interface IClinicalScheduleService
{
    Task<ScheduledCliWeeksReport> GetScheduledCliWeeksReportAsync(
        int termCode,
        CancellationToken ct = default);

    Task<ScheduledCliWeeksReport> GetScheduledCliWeeksReportByYearAsync(
        string academicYear,
        CancellationToken ct = default);

    Task<byte[]> GenerateReportPdfAsync(ScheduledCliWeeksReport report);
}
