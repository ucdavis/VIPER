using Viper.Areas.Scheduler.Models.DTOs.Responses;

namespace Viper.Areas.Scheduler.Services;

/// <summary>
/// Operator-facing API for the scheduler. Combines Hangfire's recurring-job
/// state with our SchedulerJobState markers so callers see one row per job
/// regardless of paused / active status. No Hangfire types leak across this
/// boundary.
/// </summary>
public interface ISchedulerJobsService
{
    /// <summary>Reserved id prefix for protected scheduler-infrastructure jobs.</summary>
    public const string SystemJobPrefix = "__scheduler:";

    /// <summary>Stable id of the hourly reconciler job.</summary>
    public const string ReconcileJobId = SystemJobPrefix + "reconcile";

    /// <summary>
    /// Stamp used on rows authored by scheduled (non-HTTP) executions. Kept
    /// at seven characters because the legacy <c>tblRoleMembers.ModBy</c>
    /// column is <c>varchar(8)</c> (mirrors the existing <c>"__system"</c>
    /// convention, distinct enough to filter on).
    /// </summary>
    public const string SchedulerActor = "__sched";

    Task<List<SchedulerJobDto>> ListJobsAsync(CancellationToken ct = default);

    Task<SchedulerJobDto?> GetJobAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Marker-first pause: writes the marker, then removes the recurring
    /// registration. If the registration removal step throws, the result has
    /// <c>DeregistrationPending = true</c> (HTTP 202) and the reconciler
    /// completes the deregistration on its next pass.
    /// </summary>
    /// <exception cref="SchedulerSystemJobProtectedException">id is in the protected prefix.</exception>
    /// <exception cref="SchedulerConcurrencyException">expectedRowVersion did not match.</exception>
    Task<PauseResumeResultDto> PauseJobAsync(
        string id,
        string modBy,
        byte[]? expectedRowVersion,
        CancellationToken ct = default);

    /// <summary>
    /// Registration-first resume: rehydrates the recurring registration from
    /// the marker, then deletes the marker. Repeats are idempotent.
    /// </summary>
    /// <exception cref="SchedulerSystemJobProtectedException">id is in the protected prefix.</exception>
    /// <exception cref="SchedulerJobNotFoundException">no marker row exists.</exception>
    /// <exception cref="SchedulerConcurrencyException">expectedRowVersion is null or did not match.</exception>
    Task<PauseResumeResultDto> ResumeJobAsync(
        string id,
        byte[]? expectedRowVersion,
        CancellationToken ct = default);

    /// <summary>
    /// Heals split-brain state between Hangfire's recurring-job set and the
    /// SchedulerJobState markers. Safe to run repeatedly. Called once at
    /// startup and hourly via the __scheduler:reconcile recurring job.
    /// </summary>
    Task<ReconcilerOutcomeDto> ReconcileAsync(CancellationToken ct = default);
}
