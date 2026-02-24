using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for zero effort report generation.
/// </summary>
public interface IZeroEffortService
{
    /// <summary>
    /// Generate a report of instructors with courses assigned but zero effort recorded.
    /// </summary>
    Task<ZeroEffortReport> GetZeroEffortReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a zero effort report across all terms in an academic year.
    /// </summary>
    Task<ZeroEffortReport> GetZeroEffortReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        CancellationToken ct = default);
}
