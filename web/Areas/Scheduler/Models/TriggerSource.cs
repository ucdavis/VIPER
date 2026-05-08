namespace Viper.Areas.Scheduler.Models;

/// <summary>
/// Why a scheduled job is running. Carried on <see cref="ScheduledJobContext"/>
/// so adapters can stamp audit rows differently for nightly recurring runs vs
/// admin-triggered preview/manual runs.
/// </summary>
public enum TriggerSource
{
    /// <summary>Recurring execution driven by Hangfire's cron timer.</summary>
    Scheduled,

    /// <summary>Admin-initiated execution (preview, ad-hoc trigger).</summary>
    Manual,
}
