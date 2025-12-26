using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;
using Viper.Areas.ClinicalScheduler.Constants;
using Viper.Classes.Utilities;

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
            string? mothraId,
            int rotationId,
            int weekId,
            string modifiedByMothraId,
            CancellationToken cancellationToken = default)
        {
            return await CreateAuditEntryAsync(
                mothraId!,
                rotationId,
                weekId,
                modifiedByMothraId,
                ScheduleAuditActions.InstructorAdded,
                cancellationToken);
        }

        public async Task<ScheduleAudit> LogInstructorRemovedAsync(
            string mothraId,
            int rotationId,
            int weekId,
            string modifiedByMothraId,
            CancellationToken cancellationToken = default)
        {
            return await CreateAuditEntryAsync(
                mothraId,
                rotationId,
                weekId,
                modifiedByMothraId,
                ScheduleAuditActions.InstructorRemoved,
                cancellationToken);
        }

        public async Task<ScheduleAudit> LogPrimaryEvaluatorSetAsync(
            string mothraId,
            int rotationId,
            int weekId,
            string modifiedByMothraId,
            CancellationToken cancellationToken = default)
        {
            return await CreateAuditEntryAsync(
                mothraId,
                rotationId,
                weekId,
                modifiedByMothraId,
                ScheduleAuditActions.PrimaryEvaluatorSet,
                cancellationToken);
        }

        public async Task<ScheduleAudit> LogPrimaryEvaluatorUnsetAsync(
            string mothraId,
            int rotationId,
            int weekId,
            string modifiedByMothraId,
            CancellationToken cancellationToken = default)
        {
            return await CreateAuditEntryAsync(
                mothraId,
                rotationId,
                weekId,
                modifiedByMothraId,
                ScheduleAuditActions.PrimaryEvaluatorUnset,
                cancellationToken);
        }

        public async Task<List<ScheduleAudit>> GetInstructorScheduleAuditHistoryAsync(
            int instructorScheduleId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Since InstructorScheduleId doesn't exist in the audit table,
                // we need to get the instructor schedule details first
                var schedule = await _context.InstructorSchedules
                    .AsNoTracking()
                    .Where(s => s.InstructorScheduleId == instructorScheduleId)
                    .Select(s => new { s.MothraId, s.RotationId, s.WeekId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (schedule == null)
                {
                    return new List<ScheduleAudit>();
                }

                // Find audit entries matching the rotation, week, and instructor
                return await _context.ScheduleAudits
                    .AsNoTracking()
                    .Where(a => a.RotationId == schedule.RotationId
                        && a.WeekId == schedule.WeekId
                        && a.MothraId == schedule.MothraId)
                    .OrderByDescending(a => a.TimeStamp)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit history for instructor schedule {ScheduleId}", instructorScheduleId);
                throw new InvalidOperationException($"Failed to retrieve audit history for instructor schedule {instructorScheduleId}. Please try again or contact support if the problem persists.", ex);
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
                throw new InvalidOperationException($"Failed to retrieve audit history for rotation {rotationId}, week {weekId}. Please try again or contact support if the problem persists.", ex);
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
                    Area = ScheduleAuditAreas.Clinicians,
                    TimeStamp = DateTime.Now,
                    ModifiedBy = modifiedByMothraId
                };

                _context.ScheduleAudits.Add(auditEntry);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Created audit entry: {Action} for {MothraId} on rotation {RotationId}, week {WeekId} by {ModifiedBy}",
                    action, LogSanitizer.SanitizeId(mothraId), rotationId, weekId, LogSanitizer.SanitizeId(modifiedByMothraId));

                return auditEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit entry for action {Action}, {MothraId} on rotation {RotationId}, week {WeekId}",
                    action, LogSanitizer.SanitizeId(mothraId), rotationId, weekId);
                throw new InvalidOperationException($"Failed to create audit entry for action '{action}'. Please try again or contact support if the problem persists.", ex);
            }
        }

    }
}
