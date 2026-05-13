using Hangfire;
using Hangfire.Server;
using Viper.Areas.Scheduler.Models;
using Viper.Classes.Utilities;

namespace Viper.Areas.Scheduler.Services;

/// <summary>
/// Hangfire-side dispatcher. Hangfire cannot serialize a method call against
/// an interface type, so every recurring registration targets this concrete
/// class. The runner resolves the actual <see cref="IScheduledJob"/> from DI
/// by id at execution time and hands it a <see cref="ScheduledJobContext"/>
/// stamped with the system actor.
/// </summary>
public sealed class ScheduledJobRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledJobRunner> _logger;

    public ScheduledJobRunner(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledJobRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Hangfire invokes this method with the scheduled job's id. The
    /// <see cref="IJobCancellationToken"/> parameter is replaced by Hangfire
    /// at runtime with one tied to the server's shutdown signal so jobs can
    /// honor cooperative cancellation. The <see cref="PerformContext"/> is
    /// also injected by Hangfire and threaded into <see cref="ScheduledJobContext"/>
    /// so jobs can write to the dashboard console via <c>context.WriteLine</c>.
    /// A fresh DI scope is created per execution so each run gets its own
    /// DbContext.
    /// </summary>
    public async Task RunAsync(
        string jobId,
        IJobCancellationToken cancellationToken,
        PerformContext? performContext)
    {
        using var scope = _scopeFactory.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IScheduledJobRegistry>();
        if (!registry.JobsById.TryGetValue(jobId, out var metadata))
        {
            _logger.LogWarning(
                "ScheduledJobRunner invoked with unknown id {JobId}; no IScheduledJob registered. Skipping.",
                LogSanitizer.SanitizeString(jobId));
            return;
        }

        var job = (IScheduledJob)scope.ServiceProvider.GetRequiredService(metadata.JobType);
        var context = new ScheduledJobContext(
            ScheduledJobContext.SchedulerActor,
            performContext);

        await job.RunAsync(context, cancellationToken.ShutdownToken);
    }
}
