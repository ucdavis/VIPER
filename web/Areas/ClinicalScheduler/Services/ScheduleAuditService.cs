using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;
using Viper.Areas.ClinicalScheduler.Constants;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for auditing schedule changes
    /// </summary>
    public class ScheduleAuditService : IScheduleAuditService
    {
        private readonly ClinicalSchedulerContext _context;
        private readonly ILogger<ScheduleAuditService> _logger;

        public ScheduleAuditService(
            ClinicalSchedulerContext context,
            ILogger<ScheduleAuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ScheduleAudit> LogInstructorAddedAsync(
            string mothraId,
            int rotationId,
            int weekId,
            string modifiedByMothraId,
            int instructorScheduleId,
            CancellationToken cancellationToken = default)
        {
            return await CreateAuditEntryAsync(
                mothraId,
                rotationId,
                weekId,
                modifiedByMothraId,
                ScheduleAuditActions.InstructorAdded,
                instructorScheduleId,
                cancellationToken);
        }

        public async Task<ScheduleAudit> LogInstructorRemovedAsync(
            string mothraId,
            int rotationId,
            int weekId,
            string modifiedByMothraId,
            int instructorScheduleId,
            CancellationToken cancellationToken = default)
        {
            return await CreateAuditEntryAsync(
                mothraId,
                rotationId,
                weekId,
                modifiedByMothraId,
                ScheduleAuditActions.InstructorRemoved,
                instructorScheduleId,
                cancellationToken);
        }

        public async Task<ScheduleAudit> LogPrimaryEvaluatorSetAsync(
            string mothraId,
            int rotationId,
            int weekId,
            string modifiedByMothraId,
            int instructorScheduleId,
            CancellationToken cancellationToken = default)
        {
            return await CreateAuditEntryAsync(
                mothraId,
                rotationId,
                weekId,
                modifiedByMothraId,
                ScheduleAuditActions.PrimaryEvaluatorSet,
                instructorScheduleId,
                cancellationToken);
        }

        public async Task<ScheduleAudit> LogPrimaryEvaluatorUnsetAsync(
            string mothraId,
            int rotationId,
            int weekId,
            string modifiedByMothraId,
            int instructorScheduleId,
            CancellationToken cancellationToken = default)
        {
            return await CreateAuditEntryAsync(
                mothraId,
                rotationId,
                weekId,
                modifiedByMothraId,
                ScheduleAuditActions.PrimaryEvaluatorUnset,
                instructorScheduleId,
                cancellationToken);
        }

        public async Task<List<ScheduleAudit>> GetInstructorScheduleAuditHistoryAsync(
            int instructorScheduleId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ScheduleAudits
                    .AsNoTracking()
                    .Where(a => a.InstructorScheduleId == instructorScheduleId)
                    .OrderByDescending(a => a.TimeStamp)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit history for instructor schedule {ScheduleId}", instructorScheduleId);
                throw;
            }
        }

        public async Task<List<ScheduleAudit>> GetRotationWeekAuditHistoryAsync(
            int rotationId,
            int weekId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ScheduleAudits
                    .AsNoTracking()
                    .Where(a => a.RotationId == rotationId && a.WeekId == weekId)
                    .OrderByDescending(a => a.TimeStamp)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit history for rotation {RotationId}, week {WeekId}", rotationId, weekId);
                throw;
            }
        }

        /// <summary>
        /// Create a schedule audit entry with common properties
        /// 
        /// NOTE: This service only audits successful state changes. Failed attempts (unauthorized, 
        /// conflicts, validation errors) are logged in application logs with structured logging and 
        /// correlation IDs but are intentionally not persisted to the audit table to prevent noise 
        /// and maintain focus on actual schedule changes. This follows standard enterprise audit 
        /// patterns where audit tables track state changes rather than attempted changes.
        /// </summary>
        private async Task<ScheduleAudit> CreateAuditEntryAsync(
            string mothraId,
            int rotationId,
            int weekId,
            string modifiedByMothraId,
            string action,
                        int? relatedId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var auditEntry = new ScheduleAudit
                {
                    MothraId = mothraId,
                    RotationId = rotationId,
                    WeekId = weekId,
                    Action = action,
                    InstructorScheduleId = relatedId,
                    TimeStamp = DateTime.UtcNow
                };

                // Store the modifier's MothraId (matches database schema and legacy implementation)
                auditEntry.ModifiedBy = modifiedByMothraId;

                _context.ScheduleAudits.Add(auditEntry);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Created audit entry: {Action} for {MothraId} on rotation {RotationId}, week {WeekId} by {ModifiedBy}",
                    action, mothraId, rotationId, weekId, modifiedByMothraId);

                return auditEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit entry for action {Action}, {MothraId} on rotation {RotationId}, week {WeekId}",
                    action, mothraId, rotationId, weekId);
                throw;
            }
        }

    }
}