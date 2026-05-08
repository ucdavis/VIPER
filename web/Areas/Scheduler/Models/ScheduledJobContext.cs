using Hangfire.Console;
using Hangfire.Server;

namespace Viper.Areas.Scheduler.Models;

/// <summary>
/// Per-execution context handed to every <see cref="Services.IScheduledJob"/>
/// implementation. Background jobs have no HTTP context, so the framework
/// resolves the effective <see cref="ModBy"/> here and the job stamps audit
/// rows with that value rather than reaching for <c>UserHelper.GetCurrentUser()</c>.
/// </summary>
public sealed class ScheduledJobContext
{
    private readonly PerformContext? _performContext;

    public ScheduledJobContext(TriggerSource triggerSource, string modBy, PerformContext? performContext = null)
    {
        if (string.IsNullOrWhiteSpace(modBy))
        {
            throw new ArgumentException("modBy is required.", nameof(modBy));
        }
        TriggerSource = triggerSource;
        ModBy = modBy;
        _performContext = performContext;
    }

    public TriggerSource TriggerSource { get; }

    /// <summary>
    /// Audit actor: <c>"__sched"</c> for scheduled runs (see
    /// <c>ISchedulerJobsService.SchedulerActor</c>), the LoginId for manual runs.
    /// </summary>
    public string ModBy { get; }

    /// <summary>
    /// Writes a line to the job's Hangfire dashboard console pane. No-op when
    /// the job runs outside Hangfire (unit tests, manual invocation), so jobs
    /// can call this unconditionally without nullable-context noise.
    /// </summary>
    public void WriteLine(string message)
    {
        _performContext?.WriteLine(message);
    }
}
