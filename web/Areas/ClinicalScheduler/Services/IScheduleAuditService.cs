using Viper.Models.ClinicalScheduler;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for auditing schedule changes
    /// </summary>
    public interface IScheduleAuditService
    {
        /// <summary>
        /// Log an instructor schedule addition
        /// </summary>
        /// <param name="mothraId">Instructor's MothraID</param>
        /// <param name="rotationId">Rotation ID</param>
        /// <param name="weekId">Week ID</param>
        /// <param name="modifiedByMothraId">User who made the change</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created audit entry</returns>
        Task<ScheduleAudit> LogInstructorAddedAsync(
            string mothraId,
            int rotationId,
            int weekId,
            string modifiedByMothraId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Log an instructor schedule removal
        /// </summary>
        /// <param name="mothraId">Instructor's MothraID</param>
        /// <param name="rotationId">Rotation ID</param>
        /// <param name="weekId">Week ID</param>
        /// <param name="modifiedByMothraId">User who made the change</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created audit entry</returns>
        Task<ScheduleAudit> LogInstructorRemovedAsync(
            string mothraId,
            int rotationId,
            int weekId,
            string modifiedByMothraId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Log when an instructor is made the primary evaluator
        /// </summary>
        /// <param name="mothraId">Instructor's MothraID</param>
        /// <param name="rotationId">Rotation ID</param>
        /// <param name="weekId">Week ID</param>
        /// <param name="modifiedByMothraId">User who made the change</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created audit entry</returns>
        Task<ScheduleAudit> LogPrimaryEvaluatorSetAsync(
            string mothraId,
            int rotationId,
            int weekId,
            string modifiedByMothraId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Log when an instructor is removed as primary evaluator
        /// </summary>
        /// <param name="mothraId">Instructor's MothraID</param>
        /// <param name="rotationId">Rotation ID</param>
        /// <param name="weekId">Week ID</param>
        /// <param name="modifiedByMothraId">User who made the change</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created audit entry</returns>
        Task<ScheduleAudit> LogPrimaryEvaluatorUnsetAsync(
            string mothraId,
            int rotationId,
            int weekId,
            string modifiedByMothraId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get audit history for an instructor schedule entry
        /// </summary>
        /// <param name="instructorScheduleId">InstructorSchedule ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of audit entries</returns>
        Task<List<ScheduleAudit>> GetInstructorScheduleAuditHistoryAsync(
            int instructorScheduleId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get audit history for a rotation and week combination
        /// </summary>
        /// <param name="rotationId">Rotation ID</param>
        /// <param name="weekId">Week ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of audit entries</returns>
        Task<List<ScheduleAudit>> GetRotationWeekAuditHistoryAsync(
            int rotationId,
            int weekId,
            CancellationToken cancellationToken = default);

    }

}