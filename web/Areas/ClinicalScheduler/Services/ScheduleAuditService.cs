using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.ClinicalScheduler.Constants;
using Viper.Areas.ClinicalScheduler.Models.DTOs.Responses;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models.ClinicalScheduler;

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
            catch (Exception ex) when (ex is DbUpdateException or SqlException or InvalidOperationException)
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
            catch (Exception ex) when (ex is DbUpdateException or SqlException or InvalidOperationException)
            {
                _logger.LogError(ex, "Error retrieving audit history for rotation {RotationId}, week {WeekId}", rotationId, weekId);
                throw new InvalidOperationException($"Failed to retrieve audit history for rotation {rotationId}, week {weekId}. Please try again or contact support if the problem persists.", ex);
            }
        }

        public async Task<List<AuditLogEntryDto>> GetAuditLogAsync(
            int gradYear,
            int? rotationId,
            string? person,
            string? modifiedBy,
            string? area,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Scope to the grad year via vWeek (a week belongs to a grad year there),
                // and left-join names so a missing person/rotation lookup never drops a row.
                var query =
                    from a in _context.ScheduleAudits.AsNoTracking()
                    join vw in _context.VWeeks on a.WeekId equals (int?)vw.WeekId
                    where vw.GradYear == gradYear
                    join rot in _context.Rotations on a.RotationId equals (int?)rot.RotId into rotJoin
                    from rot in rotJoin.DefaultIfEmpty()
                    join ap in _context.Persons on a.MothraId equals ap.IdsMothraId into affectedJoin
                    from ap in affectedJoin.DefaultIfEmpty()
                    join mp in _context.Persons on a.ModifiedBy equals mp.IdsMothraId into modifierJoin
                    from mp in modifierJoin.DefaultIfEmpty()
                    select new { Audit = a, Week = vw, Rotation = rot, Affected = ap, Modifier = mp };

                if (rotationId.HasValue)
                {
                    query = query.Where(x => x.Audit.RotationId == rotationId.Value);
                }
                if (!string.IsNullOrWhiteSpace(area))
                {
                    query = query.Where(x => x.Audit.Area == area);
                }
                if (!string.IsNullOrWhiteSpace(modifiedBy))
                {
                    query = query.Where(x => x.Audit.ModifiedBy == modifiedBy);
                }
                if (fromDate.HasValue)
                {
                    query = query.Where(x => x.Audit.TimeStamp >= fromDate.Value);
                }
                if (toDate.HasValue)
                {
                    query = query.Where(x => x.Audit.TimeStamp <= toDate.Value);
                }
                if (!string.IsNullOrWhiteSpace(person))
                {
                    query = query.Where(x => x.Audit.MothraId == person);
                }

                return await query
                    .OrderByDescending(x => x.Audit.TimeStamp)
                    .Take(2500)
                    .Select(x => new AuditLogEntryDto
                    {
                        ScheduleAuditId = x.Audit.ScheduleAuditId,
                        Area = x.Audit.Area,
                        MothraId = x.Audit.MothraId,
                        PersonName = x.Affected != null ? x.Affected.PersonDisplayFullName : (x.Audit.MothraId ?? string.Empty),
                        Action = x.Audit.Action,
                        RotationId = x.Audit.RotationId,
                        RotationName = x.Rotation != null ? x.Rotation.Name : string.Empty,
                        WeekId = x.Audit.WeekId,
                        WeekNum = x.Week.WeekNum,
                        WeekStart = x.Week.DateStart,
                        ModifiedBy = x.Audit.ModifiedBy,
                        ModifiedByName = x.Modifier != null ? x.Modifier.PersonDisplayFullName : x.Audit.ModifiedBy,
                        TimeStamp = x.Audit.TimeStamp,
                    })
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is DbUpdateException or SqlException or InvalidOperationException)
            {
                _logger.LogError(ex, "Error retrieving audit log for grad year {GradYear}", gradYear);
                throw new InvalidOperationException("Failed to retrieve the audit log. Please try again or contact support if the problem persists.", ex);
            }
        }

        public async Task<List<AuditModifierDto>> GetAuditModifiersAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Left join so a modifier whose MothraId no longer resolves in Persons still
                // appears in the filter, matching the raw-ID fallback in GetAuditLogAsync.
                return await (
                    from a in _context.ScheduleAudits.AsNoTracking()
                    where a.ModifiedBy != ""
                    join p in _context.Persons on a.ModifiedBy equals p.IdsMothraId into modifierJoin
                    from p in modifierJoin.DefaultIfEmpty()
                    select new AuditModifierDto
                    {
                        MothraId = a.ModifiedBy,
                        DisplayName = p != null ? p.PersonDisplayFullName : a.ModifiedBy,
                    })
                    .Distinct()
                    .OrderBy(m => m.DisplayName)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is DbUpdateException or SqlException or InvalidOperationException)
            {
                _logger.LogError(ex, "Error retrieving audit log modifiers");
                throw new InvalidOperationException("Failed to retrieve the list of audit modifiers. Please try again or contact support if the problem persists.", ex);
            }
        }

        public async Task<List<AuditModifierDto>> GetAuditPersonsAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Left join so an affected person whose MothraId no longer resolves in Persons
                // still appears in the filter, matching the raw-ID fallback in GetAuditLogAsync.
                return await (
                    from a in _context.ScheduleAudits.AsNoTracking()
                    where a.MothraId != null
                    join p in _context.Persons on a.MothraId equals p.IdsMothraId into affectedJoin
                    from p in affectedJoin.DefaultIfEmpty()
                    select new AuditModifierDto
                    {
                        MothraId = a.MothraId!,
                        DisplayName = p != null ? p.PersonDisplayFullName : a.MothraId!,
                    })
                    .Distinct()
                    .OrderBy(m => m.DisplayName)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is DbUpdateException or SqlException or InvalidOperationException)
            {
                _logger.LogError(ex, "Error retrieving audit log persons");
                throw new InvalidOperationException("Failed to retrieve the list of audited persons. Please try again or contact support if the problem persists.", ex);
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
            catch (Exception ex) when (ex is DbUpdateException or SqlException or InvalidOperationException)
            {
                _logger.LogError(ex, "Error creating audit entry for action {Action}, {MothraId} on rotation {RotationId}, week {WeekId}",
                    action, LogSanitizer.SanitizeId(mothraId), rotationId, weekId);
                throw new InvalidOperationException($"Failed to create audit entry for action '{action}'. Please try again or contact support if the problem persists.", ex);
            }
        }

    }
}
