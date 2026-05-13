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
    /// <summary>
    /// Audit actor stamped on rows produced by scheduler-triggered runs.
    /// 7 chars to fit the legacy <c>tblRoleMembers.ModBy varchar(8)</c>
    /// column while staying distinct from the existing <c>"__system"</c>
    /// convention.
    /// </summary>
    public const string SchedulerActor = "__sched";

    private readonly PerformContext? _performContext;

    public ScheduledJobContext(string modBy, PerformContext? performContext = null)
    {
        if (string.IsNullOrWhiteSpace(modBy))
        {
            throw new ArgumentException("modBy is required.", nameof(modBy));
        }
        ModBy = modBy;
        _performContext = performContext;
    }

    /// <summary>
    /// Audit actor. Always <see cref="SchedulerActor"/> today; if a manual
    /// trigger path is added later it can pass a real LoginId here.
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
