namespace Viper.Areas.Scheduler.Services;

/// <summary>
/// Declares the cron schedule and recurring-job id of an <see cref="IScheduledJob"/>.
/// The discovery pass at startup reads this attribute and registers the job
/// with Hangfire.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ScheduledJobAttribute : Attribute
{
    public ScheduledJobAttribute(string id, string cron)
    {
        Id = id;
        Cron = cron;
    }

    /// <summary>Recurring-job id.</summary>
    public string Id { get; }

    /// <summary>Hangfire cron expression (5 or 6 field).</summary>
    public string Cron { get; }

    /// <summary>
    /// IANA or Windows time zone id. Defaults to Pacific (UC Davis); set
    /// explicitly when a job needs a different reference time zone.
    /// </summary>
    public string TimeZoneId { get; set; } = "Pacific Standard Time";
}
