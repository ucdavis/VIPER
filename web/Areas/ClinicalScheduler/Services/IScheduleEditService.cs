using Viper.Models.ClinicalScheduler;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for editing instructor schedules with business logic and validation
    /// </summary>
    public interface IScheduleEditService
    {
        /// <summary>
        /// Add an instructor to a rotation for specific weeks
        /// </summary>
        /// <param name="mothraId">Instructor's MothraID</param>
        /// <param name="rotationId">Rotation ID</param>
        /// <param name="weekIds">Array of week IDs to schedule</param>
        /// <param name="isPrimaryEvaluator">Whether this instructor should be the primary evaluator</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of created InstructorSchedule entries</returns>
        Task<List<InstructorSchedule>> AddInstructorAsync(
            string mothraId,
            int rotationId,
            int[] weekIds,
            bool isPrimaryEvaluator = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove an instructor schedule assignment
        /// </summary>
        /// <param name="instructorScheduleId">InstructorSchedule ID to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully removed</returns>
        Task<bool> RemoveInstructorScheduleAsync(
            int instructorScheduleId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Set or unset an instructor as the primary evaluator for a rotation/week
        /// </summary>
        /// <param name="instructorScheduleId">InstructorSchedule ID</param>
        /// <param name="isPrimary">True to set as primary, false to unset</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully updated</returns>
        Task<bool> SetPrimaryEvaluatorAsync(
            int instructorScheduleId,
            bool isPrimary,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if an instructor can be removed from a schedule (not primary evaluator)
        /// </summary>
        /// <param name="instructorScheduleId">InstructorSchedule ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if can be safely removed</returns>
        Task<bool> CanRemoveInstructorAsync(
            int instructorScheduleId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get conflicting schedules for an instructor on specific weeks
        /// </summary>
        /// <param name="mothraId">Instructor's MothraID</param>
        /// <param name="weekIds">Week IDs to check</param>
        /// <param name="excludeRotationId">Rotation ID to exclude from conflict check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of conflicting InstructorSchedule entries</returns>
        Task<List<InstructorSchedule>> GetScheduleConflictsAsync(
            string mothraId,
            int[] weekIds,
            int? excludeRotationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all instructors currently scheduled for a rotation and specific weeks.
        /// </summary>
        /// <param name="rotationId">Rotation ID</param>
        /// <param name="weekIds">Week IDs</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of InstructorSchedule entries</returns>
        Task<List<InstructorSchedule>> GetScheduledInstructorsAsync(
            int rotationId,
            int[] weekIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically set a primary evaluator for multiple weeks, clearing any existing primary evaluators
        /// </summary>
        /// <param name="mothraId">Instructor's MothraID to make primary</param>
        /// <param name="rotationId">Rotation ID</param>
        /// <param name="weekIds">Week IDs where this instructor should be primary</param>
        /// <param name="modifiedByMothraId">User making the change</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully updated</returns>
        Task<bool> SetPrimaryEvaluatorForMultipleWeeksAsync(
            string mothraId,
            int rotationId,
            int[] weekIds,
            string modifiedByMothraId,
            CancellationToken cancellationToken = default);
    }
}