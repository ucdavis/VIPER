namespace Viper.Areas.Scheduler.Models;

/// <summary>
/// Per-execution context handed to every <see cref="Services.IScheduledJob"/>
/// implementation. Background jobs have no HTTP context, so the framework
/// resolves the effective <see cref="ModBy"/> here and the job stamps audit
/// rows with that value rather than reaching for <c>UserHelper.GetCurrentUser()</c>.
/// </summary>
public sealed class ScheduledJobContext
{
    public ScheduledJobContext(TriggerSource triggerSource, string modBy)
    {
        if (string.IsNullOrWhiteSpace(modBy))
        {
            throw new ArgumentException("modBy is required.", nameof(modBy));
        }
        TriggerSource = triggerSource;
        ModBy = modBy;
    }

    public TriggerSource TriggerSource { get; }

    /// <summary>
    /// Audit actor: <c>"__sched"</c> for scheduled runs (see
    /// <c>ISchedulerJobsService.SchedulerActor</c>), the LoginId for manual runs.
    /// </summary>
    public string ModBy { get; }
}
