namespace Viper.Areas.Scheduler.Services;

/// <summary>
/// One declaration of a scheduled job, materialized from the
/// <see cref="ScheduledJobAttribute"/> on its implementing type. Used by the
/// startup registrar to wire Hangfire.
/// </summary>
public sealed class ScheduledJobMetadata
{
    public ScheduledJobMetadata(
        Type jobType,
        string id,
        string cron,
        string timeZoneId)
    {
        JobType = jobType;
        Id = id;
        Cron = cron;
        TimeZoneId = timeZoneId;
    }

    public Type JobType { get; }
    public string Id { get; }
    public string Cron { get; }
    public string TimeZoneId { get; }
}

/// <summary>
/// Frozen view of every <see cref="IScheduledJob"/> declared in the running
/// process, indexed by recurring-job id. Populated once at startup.
/// </summary>
public interface IScheduledJobRegistry
{
    IReadOnlyDictionary<string, ScheduledJobMetadata> JobsById { get; }
}

public sealed class ScheduledJobRegistry : IScheduledJobRegistry
{
    public ScheduledJobRegistry(IReadOnlyDictionary<string, ScheduledJobMetadata> jobsById)
    {
        JobsById = jobsById;
    }

    public IReadOnlyDictionary<string, ScheduledJobMetadata> JobsById { get; }
}
