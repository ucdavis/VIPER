namespace Viper.Areas.Scheduler.Models.Entities;

/// <summary>
/// Marker row that records the original definition of a recurring Hangfire job
/// while it is paused. Hangfire has no native pause; we deregister the job and
/// keep its definition here so a later resume can rehydrate it. Lives in the
/// same HangFire schema as Hangfire's own tables to colocate scheduler state
/// without crossing a DB boundary.
/// </summary>
public class SchedulerJobState
{
    /// <summary>Hangfire recurring-job key (matches its string id).</summary>
    public string RecurringJobId { get; set; } = string.Empty;

    public string Cron { get; set; } = string.Empty;

    public string Queue { get; set; } = string.Empty;

    /// <summary>IANA / Windows time zone id; explicit per scheduler conventions.</summary>
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>Fully qualified type name used to rehydrate the job on resume.</summary>
    public string JobTypeName { get; set; } = string.Empty;

    /// <summary>JSON payload of the job's arguments (Hangfire serialization).</summary>
    public string SerializedArgs { get; set; } = string.Empty;

    /// <summary>Local timestamp the pause was recorded (DateTimeKind.Local per project DB convention).</summary>
    public DateTime PausedAt { get; set; }

    /// <summary>LoginId of the operator, or "__scheduler" for system pauses.</summary>
    public string PausedBy { get; set; } = string.Empty;

    /// <summary>SQL Server rowversion for optimistic concurrency on pause/resume.</summary>
    public byte[] RowVersion { get; set; } = [];
}
