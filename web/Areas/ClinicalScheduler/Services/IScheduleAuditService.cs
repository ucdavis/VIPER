using Viper.Areas.ClinicalScheduler.Models.DTOs.Responses;
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
            string? mothraId,
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

        /// <summary>
        /// Get a filtered, display-ready audit log for a grad year.
        /// Mirrors the legacy "Schedule Changes Audit log" page query.
        /// </summary>
        /// <param name="gradYear">Grad year to scope results to (via the week's grad year)</param>
        /// <param name="rotationId">Optional rotation filter</param>
        /// <param name="termCode">Optional term (semester) filter, scoped to the grad year</param>
        /// <param name="person">Optional MothraID of the affected student/clinician</param>
        /// <param name="modifiedBy">Optional MothraID of the user who made the change</param>
        /// <param name="area">Optional area filter (Students / Clinicians)</param>
        /// <param name="fromDate">Optional inclusive lower bound on the change timestamp</param>
        /// <param name="toDate">Optional inclusive upper bound on the change timestamp</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Up to 2500 enriched audit entries, newest first</returns>
        Task<List<AuditLogEntryDto>> GetAuditLogAsync(
            int gradYear,
            int? rotationId,
            int? termCode,
            string? person,
            string? modifiedBy,
            string? area,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the distinct terms (semesters) that fall within a grad year, for the audit
        /// trail "Term" filter.
        /// </summary>
        /// <param name="gradYear">Grad year to scope the terms to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Distinct terms ordered chronologically</returns>
        Task<List<AuditTermDto>> GetAuditTermsAsync(
            int gradYear,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the distinct set of users who have made an audited schedule change,
        /// used to populate the audit trail "Modified By" filter.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Distinct modifiers ordered by display name</returns>
        Task<List<AuditModifierDto>> GetAuditModifiersAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the distinct set of affected students/clinicians that appear in the audit
        /// trail, used to populate the audit trail "Person" filter.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Distinct affected persons ordered by display name</returns>
        Task<List<AuditModifierDto>> GetAuditPersonsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the display-ready change history for a single rotation + week, newest first.
        /// Powers the inline per-week history popover in the Schedule-by-Rotation grid.
        /// </summary>
        /// <param name="rotationId">Rotation the week belongs to</param>
        /// <param name="weekId">Week to scope the history to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Enriched audit entries for the rotation/week, newest first</returns>
        Task<List<AuditLogEntryDto>> GetRotationWeekAuditAsync(
            int rotationId,
            int weekId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the display-ready change history for a single clinician + week, newest first
        /// (across all rotations that week). Powers the inline per-week history popover in
        /// the Schedule-by-Clinician grid.
        /// </summary>
        /// <param name="mothraId">MothraID of the affected clinician</param>
        /// <param name="weekId">Week to scope the history to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Enriched audit entries for the clinician/week, newest first</returns>
        Task<List<AuditLogEntryDto>> GetClinicianWeekAuditAsync(
            string mothraId,
            int weekId,
            CancellationToken cancellationToken = default);

    }

}
