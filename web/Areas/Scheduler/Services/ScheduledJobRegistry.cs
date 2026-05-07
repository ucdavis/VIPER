namespace Viper.Areas.Scheduler.Services;

/// <summary>
/// One declaration of a scheduled job, materialized from the
/// <see cref="ScheduledJobAttribute"/> on its implementing type. Used by the
/// startup registrar (to wire Hangfire) and by the reconciler (to detect
/// jobs that disappeared from Hangfire's storage and need re-registering).
/// </summary>
public sealed class ScheduledJobMetadata
{
    public ScheduledJobMetadata(
        Type jobType,
        string id,
        string cron,
        string timeZoneId,
        bool isSystem)
    {
        JobType = jobType;
        Id = id;
        Cron = cron;
        TimeZoneId = timeZoneId;
        IsSystem = isSystem;
    }

    public Type JobType { get; }
    public string Id { get; }
    public string Cron { get; }
    public string TimeZoneId { get; }
    public bool IsSystem { get; }
}

/// <summary>
/// Frozen view of every <see cref="IScheduledJob"/> declared in the running
/// process, indexed by recurring-job id. Populated once at startup;
/// downstream consumers (reconciler, admin UI) read it without reflecting
/// over assemblies again.
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
