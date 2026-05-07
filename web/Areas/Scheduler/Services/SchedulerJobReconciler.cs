using Hangfire;

namespace Viper.Areas.Scheduler.Services;

/// <summary>
/// Drives the reconciler. Two entry points:
///
/// 1. Startup pass via <see cref="RunOnceAsync"/>, scheduled by
///    <see cref="ReconcilerStartupHostedService"/> after Hangfire boots.
/// 2. Hourly recurring Hangfire job <c>__scheduler:reconcile</c>, registered
///    by <see cref="RegisterRecurring"/>. The job is in the protected
///    "__scheduler:" namespace so the API refuses to pause/remove it.
/// </summary>
public class SchedulerJobReconciler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SchedulerJobReconciler> _logger;

    public SchedulerJobReconciler(
        IServiceScopeFactory scopeFactory,
        ILogger<SchedulerJobReconciler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Hangfire entry point for the recurring system job. Resolves the
    /// service inside a fresh scope, since recurring executions have no
    /// ambient request scope.
    /// </summary>
    public async Task RunRecurringAsync()
    {
        await RunOnceAsync(CancellationToken.None);
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISchedulerJobsService>();

        var outcome = await service.ReconcileAsync(ct);

        _logger.LogInformation(
            "Scheduler reconciler pass: split-brain healed={SplitBrainHealed}, system markers deleted={SystemMarkersDeleted}, paused ok={CorrectlyPaused}, active ok={CorrectlyActive}, lost registrations healed={LostRegistrationsHealed}, markers={MarkersExamined}, registrations={RegistrationsExamined}",
            outcome.SplitBrainHealed,
            outcome.SystemMarkersDeleted,
            outcome.CorrectlyPaused,
            outcome.CorrectlyActive,
            outcome.LostRegistrationsHealed,
            outcome.MarkersExamined,
            outcome.RegistrationsExamined);
    }

    /// <summary>
    /// Registers the hourly reconciler as a recurring Hangfire job. Cron is
    /// "0 * * * *" (top of every hour) in the canonical Pacific time zone
    /// the rest of the scheduler uses.
    /// </summary>
    public static void RegisterRecurring(IRecurringJobManager manager)
    {
        manager.AddOrUpdate<SchedulerJobReconciler>(
            ISchedulerJobsService.ReconcileJobId,
            r => r.RunRecurringAsync(),
            "0 * * * *",
            new RecurringJobOptions
            {
                TimeZone = ResolvePacific(),
            });
    }

    private static readonly NLog.Logger _classLogger = NLog.LogManager.GetCurrentClassLogger();

    private static TimeZoneInfo ResolvePacific()
    {
        // Try Windows id first, then IANA. UC Davis runs Windows in prod but
        // a developer machine using IANA aliases (or a future Linux host)
        // shouldn't break startup.
        foreach (var id in new[] { "Pacific Standard Time", "America/Los_Angeles" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the next id alias.
            }
            catch (InvalidTimeZoneException)
            {
                // Try the next id alias.
            }
        }
        _classLogger.Warn(
            "Neither 'Pacific Standard Time' nor 'America/Los_Angeles' resolved on this host; reconciler falling back to UTC.");
        return TimeZoneInfo.Utc;
    }
}

/// <summary>
/// Runs <see cref="SchedulerJobReconciler.RunOnceAsync"/> a single time on
/// startup, after Hangfire's server is up. Registered only when Hangfire
/// itself is wired.
/// </summary>
public class ReconcilerStartupHostedService : IHostedService
{
    private readonly SchedulerJobReconciler _reconciler;
    private readonly ILogger<ReconcilerStartupHostedService> _logger;

    public ReconcilerStartupHostedService(
        SchedulerJobReconciler reconciler,
        ILogger<ReconcilerStartupHostedService> logger)
    {
        _reconciler = reconciler;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Don't block host startup on reconciliation I/O (DB calls + Hangfire
        // storage scans). The hourly recurring job covers persistent drift,
        // and reconciler failure must not crash startup.
        _ = Task.Run(async () =>
        {
            try
            {
                await _reconciler.RunOnceAsync(cancellationToken);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(ex, "Scheduler reconciler startup pass cancelled during shutdown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduler reconciler startup pass failed");
            }
        }, cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
