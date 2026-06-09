using Viper.Areas.Scheduler.Models;

namespace Viper.Areas.Scheduler.Services;

/// <summary>
/// Contract every cron-driven background job in VIPER implements. The
/// scheduler discovers <c>IScheduledJob</c> registrations at startup and
/// wires each one into Hangfire based on its <see cref="ScheduledJobAttribute"/>.
/// Implementations should not call <c>UserHelper.GetCurrentUser()</c>; the
/// effective audit actor is supplied via the per-run context.
/// </summary>
public interface IScheduledJob
{
    /// <summary>Executes one run of the scheduled job.</summary>
    /// <param name="context">Per-run trigger source and audit actor.</param>
    /// <param name="ct">Cancellation token honored by long-running jobs.</param>
    Task RunAsync(ScheduledJobContext context, CancellationToken ct);
}
