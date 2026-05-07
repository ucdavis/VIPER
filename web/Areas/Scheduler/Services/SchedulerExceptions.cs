namespace Viper.Areas.Scheduler.Services;

/// <summary>
/// Pause or resume was attempted on a job in the reserved "__scheduler:" namespace.
/// Controllers translate this to HTTP 403 with code "system_job_not_pausable".
/// </summary>
public class SchedulerSystemJobProtectedException : InvalidOperationException
{
    public SchedulerSystemJobProtectedException(string id)
        : base($"System job '{id}' cannot be paused or resumed.")
    {
    }
}

/// <summary>
/// Resume was called on a job with no marker row. Controllers translate to HTTP 404.
/// </summary>
public class SchedulerJobNotFoundException : InvalidOperationException
{
    public SchedulerJobNotFoundException(string id)
        : base($"No paused-state marker found for recurring job '{id}'.")
    {
    }
}

/// <summary>
/// Optimistic-concurrency conflict: the supplied rowversion did not match the
/// current marker. Controllers translate to HTTP 409.
/// </summary>
public class SchedulerConcurrencyException : InvalidOperationException
{
    public SchedulerConcurrencyException(string id)
        : base($"Concurrent update detected for '{id}'; refresh and retry.")
    {
    }
}
