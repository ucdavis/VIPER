// ReSharper disable UnusedAutoPropertyAccessor.Global
// Property getters are read by System.Text.Json when this DTO is serialized
// to API responses; ReSharper cannot see that reflection-based usage.
namespace Viper.Areas.Scheduler.Models.DTOs.Responses;

/// <summary>
/// Operator-facing view of a recurring job. Combines Hangfire's recurring-job
/// metadata with our paused-state marker so the admin UI sees a single row per
/// job regardless of whether it is currently registered or paused. No Hangfire
/// types leak across this boundary.
/// </summary>
public class SchedulerJobDto
{
    public string Id { get; set; } = string.Empty;

    public string Cron { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = string.Empty;

    public string Queue { get; set; } = string.Empty;

    public string JobTypeName { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the next scheduled execution; null when paused.</summary>
    public DateTime? NextExecution { get; set; }

    /// <summary>UTC timestamp of the most recent execution attempt.</summary>
    public DateTime? LastExecution { get; set; }

    /// <summary>Hangfire's last-state name (e.g. "Succeeded", "Failed"); null when no run yet.</summary>
    public string? LastJobState { get; set; }

    /// <summary>True when a SchedulerJobState marker exists for this job.</summary>
    public bool IsPaused { get; set; }

    /// <summary>Local timestamp the pause was recorded; null when not paused. Matches SchedulerJobState.PausedAt.</summary>
    public DateTime? PausedAt { get; set; }

    /// <summary>LoginId or "__sched" who recorded the pause; null when not paused.</summary>
    public string? PausedBy { get; set; }

    /// <summary>True for jobs in the reserved "__scheduler:" namespace; UI must hide pause/resume.</summary>
    public bool IsSystem { get; set; }

    /// <summary>Base64 of the rowversion guarding pause/resume; null when not paused.</summary>
    public string? RowVersion { get; set; }
}
