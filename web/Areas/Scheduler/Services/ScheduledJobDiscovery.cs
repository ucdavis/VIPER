using System.Reflection;
using Hangfire;

namespace Viper.Areas.Scheduler.Services;

/// <summary>
/// Discovers <see cref="IScheduledJob"/> implementations annotated with
/// <see cref="ScheduledJobAttribute"/> and produces a registry plus DI
/// registrations for each.
/// </summary>
public static class ScheduledJobDiscovery
{
    /// <summary>
    /// Scans the supplied assemblies for concrete classes that implement
    /// <see cref="IScheduledJob"/> and carry a <see cref="ScheduledJobAttribute"/>.
    /// Validates the <c>__scheduler:</c> prefix invariant, registers each job
    /// type and the runner with DI, and returns the resulting metadata.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a job violates the prefix invariant (system flag must
    /// match the id prefix) or when two jobs declare the same id.
    /// </exception>
    public static IReadOnlyList<ScheduledJobMetadata> RegisterScheduledJobs(
        IServiceCollection services,
        IEnumerable<Assembly> assemblies)
    {
        var found = new Dictionary<string, ScheduledJobMetadata>(StringComparer.Ordinal);

        foreach (var asm in assemblies)
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }
                if (!typeof(IScheduledJob).IsAssignableFrom(type))
                {
                    continue;
                }

                var attr = type.GetCustomAttribute<ScheduledJobAttribute>();
                if (attr == null)
                {
                    throw new InvalidOperationException(
                        $"{type.FullName} implements IScheduledJob but is missing [ScheduledJob]; either add the attribute or unregister the type.");
                }

                ValidateSystemPrefix(type, attr);

                if (found.TryGetValue(attr.Id, out var existing))
                {
                    throw new InvalidOperationException(
                        $"Duplicate scheduled-job id '{attr.Id}' on {type.FullName} (already declared by {existing.JobType.FullName}).");
                }

                services.AddScoped(type);
                found[attr.Id] = new ScheduledJobMetadata(
                    type,
                    attr.Id,
                    attr.Cron,
                    attr.TimeZoneId,
                    attr.IsSystem);
            }
        }

        var snapshot = new System.Collections.ObjectModel.ReadOnlyDictionary<string, ScheduledJobMetadata>(found);
        services.AddSingleton<IScheduledJobRegistry>(new ScheduledJobRegistry(snapshot));
        services.AddTransient<ScheduledJobRunner>();

        return [.. found.Values];
    }

    /// <summary>
    /// Calls <see cref="IRecurringJobManager.AddOrUpdate"/> for each declared
    /// job. Idempotent: safe to invoke on every startup, and the reconciler
    /// reuses it to heal lost registrations. When <paramref name="autoSchedule"/>
    /// is false, every job is registered with <see cref="Cron.Never"/> so it
    /// is visible in the dashboard but never fires on a schedule (operators
    /// can still use "Trigger now" or <c>BackgroundJob.Enqueue</c>).
    /// </summary>
    public static void RegisterRecurringJobs(
        IRecurringJobManager manager,
        IEnumerable<ScheduledJobMetadata> jobs,
        bool autoSchedule = true)
    {
        foreach (var meta in jobs)
        {
            var cron = autoSchedule ? meta.Cron : Cron.Never();
            // PerformContext is null in the expression; Hangfire substitutes
            // the real context at execution time (same pattern as IJobCancellationToken).
            manager.AddOrUpdate<ScheduledJobRunner>(
                meta.Id,
                runner => runner.RunAsync(meta.Id, JobCancellationToken.Null, null!),
                cron,
                new RecurringJobOptions
                {
                    TimeZone = ResolveTimeZone(meta.TimeZoneId),
                });
        }
    }

    private static void ValidateSystemPrefix(Type type, ScheduledJobAttribute attr)
    {
        var hasPrefix = attr.Id.StartsWith(ISchedulerJobsService.SystemJobPrefix, StringComparison.Ordinal);
        if (attr.IsSystem && !hasPrefix)
        {
            throw new InvalidOperationException(
                $"{type.FullName} declares IsSystem=true but its id '{attr.Id}' does not start with '{ISchedulerJobsService.SystemJobPrefix}'.");
        }
        if (!attr.IsSystem && hasPrefix)
        {
            throw new InvalidOperationException(
                $"{type.FullName} uses the reserved '{ISchedulerJobsService.SystemJobPrefix}' prefix without IsSystem=true.");
        }
    }

    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException ex)
        {
            _logger.Warn(ex, "Scheduled job timezone '{0}' not found; falling back to UTC.", id);
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException ex)
        {
            _logger.Warn(ex, "Scheduled job timezone '{0}' is invalid; falling back to UTC.", id);
            return TimeZoneInfo.Utc;
        }
    }
}
