using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for editing instructor schedules with business logic and validation
    /// </summary>
    public class ScheduleEditService : IScheduleEditService
    {
        private readonly ClinicalSchedulerContext _context;
        private readonly ISchedulePermissionService _permissionService;
        private readonly IScheduleAuditService _auditService;
        private readonly ILogger<ScheduleEditService> _logger;
        private readonly IUserHelper _userHelper;

        public ScheduleEditService(
            ClinicalSchedulerContext context,
            ISchedulePermissionService permissionService,
            IScheduleAuditService auditService,
            ILogger<ScheduleEditService> logger,
            IUserHelper? userHelper = null)
        {
            _context = context;
            _permissionService = permissionService;
            _auditService = auditService;
            _logger = logger;
            _userHelper = userHelper ?? new UserHelper();
        }

        public async Task<List<InstructorSchedule>> AddInstructorAsync(
            string mothraId,
            int rotationId,
            int[] weekIds,
            bool isPrimaryEvaluator = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check permissions
                if (!await _permissionService.HasEditPermissionForRotationAsync(rotationId, cancellationToken))
                {
                    throw new UnauthorizedAccessException($"User does not have permission to edit rotation {rotationId}");
                }

                var currentUser = _userHelper.GetCurrentUser();
                if (currentUser == null)
                {
                    throw new UnauthorizedAccessException("No authenticated user found");
                }

                // Check for conflicts
                var conflicts = await GetScheduleConflictsAsync(mothraId, weekIds, rotationId, cancellationToken);
                if (conflicts.Any())
                {
                    var conflictDetails = string.Join(", ", conflicts.Select(c => $"Week {c.WeekId} on Rotation {c.RotationId}"));
                    throw new InvalidOperationException($"Instructor {mothraId} has scheduling conflicts: {conflictDetails}");
                }

                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var createdSchedules = new List<InstructorSchedule>();

                    foreach (var weekId in weekIds)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // If setting as primary evaluator, clear existing primary first
                        if (isPrimaryEvaluator)
                        {
                            await ClearPrimaryEvaluatorAsync(rotationId, weekId, cancellationToken);
                        }

                        // Create new instructor schedule
                        var schedule = new InstructorSchedule
                        {
                            MothraId = mothraId,
                            RotationId = rotationId,
                            WeekId = weekId,
                            Evaluator = isPrimaryEvaluator
                        };

                        _context.InstructorSchedules.Add(schedule);
                        createdSchedules.Add(schedule);
                    }

                    // Save all schedule entities at once for better performance
                    await _context.SaveChangesAsync(cancellationToken);

                    // Log audit entries after successful save (so we have InstructorScheduleId)
                    foreach (var schedule in createdSchedules)
                    {
                        await _auditService.LogInstructorAddedAsync(
                            mothraId, rotationId, schedule.WeekId, currentUser.MothraId, schedule.InstructorScheduleId, cancellationToken);

                        if (isPrimaryEvaluator)
                        {
                            await _auditService.LogPrimaryEvaluatorSetAsync(
                                mothraId, rotationId, schedule.WeekId, currentUser.MothraId, schedule.InstructorScheduleId, cancellationToken);
                        }
                    }

                    // Log summary instead of per-week logs
                    _logger.LogInformation("Added instructor {MothraId} to rotation {RotationId} for {WeekCount} weeks (Primary: {IsPrimary})",
                        mothraId, rotationId, weekIds.Length, isPrimaryEvaluator);

                    await transaction.CommitAsync(cancellationToken);
                    return createdSchedules;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding instructor {MothraId} to rotation {RotationId} for weeks {WeekIds}",
                    mothraId, rotationId, string.Join(",", weekIds));
                throw;
            }
        }

        public async Task<bool> RemoveInstructorScheduleAsync(
            int instructorScheduleId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var schedule = await _context.InstructorSchedules
                    .Include(s => s.Rotation)
                    .FirstOrDefaultAsync(s => s.InstructorScheduleId == instructorScheduleId, cancellationToken);

                if (schedule == null)
                {
                    _logger.LogWarning("InstructorSchedule {ScheduleId} not found for removal", instructorScheduleId);
                    return false;
                }

                // Check permissions
                if (!await _permissionService.HasEditPermissionForRotationAsync(schedule.RotationId, cancellationToken))
                {
                    throw new UnauthorizedAccessException($"User does not have permission to edit rotation {schedule.RotationId}");
                }

                var currentUser = _userHelper.GetCurrentUser();
                if (currentUser == null)
                {
                    throw new UnauthorizedAccessException("No authenticated user found");
                }

                // Check if can be removed (not primary evaluator, or there are other instructors for this rotation/week)
                if (schedule.Evaluator)
                {
                    var otherInstructors = await _context.InstructorSchedules
                        .Where(s => s.RotationId == schedule.RotationId && s.WeekId == schedule.WeekId &&
                                   s.InstructorScheduleId != instructorScheduleId)
                        .AnyAsync(cancellationToken);

                    if (!otherInstructors)
                    {
                        throw new InvalidOperationException($"Cannot remove primary evaluator {schedule.MothraId} - they are the only instructor for this rotation/week. Assign another instructor as primary first.");
                    }
                }

                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Log removal before deleting
                    await _auditService.LogInstructorRemovedAsync(
                        schedule.MothraId, schedule.RotationId, schedule.WeekId, currentUser.MothraId, instructorScheduleId, cancellationToken);

                    if (schedule.Evaluator)
                    {
                        await _auditService.LogPrimaryEvaluatorUnsetAsync(
                            schedule.MothraId, schedule.RotationId, schedule.WeekId, currentUser.MothraId, instructorScheduleId, cancellationToken);
                    }

                    _context.InstructorSchedules.Remove(schedule);
                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogDebug("Removed instructor {MothraId} from rotation {RotationId}, week {WeekId} (WasPrimary: {WasPrimary})",
                        schedule.MothraId, schedule.RotationId, schedule.WeekId, schedule.Evaluator);

                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing instructor schedule {ScheduleId}", instructorScheduleId);
                throw;
            }
        }

        public async Task<bool> SetPrimaryEvaluatorAsync(
            int instructorScheduleId,
            bool isPrimary,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var schedule = await _context.InstructorSchedules
                    .Include(s => s.Rotation)
                    .FirstOrDefaultAsync(s => s.InstructorScheduleId == instructorScheduleId, cancellationToken);

                if (schedule == null)
                {
                    _logger.LogWarning("InstructorSchedule {ScheduleId} not found for primary evaluator update", instructorScheduleId);
                    return false;
                }

                // Check permissions
                if (!await _permissionService.HasEditPermissionForRotationAsync(schedule.RotationId, cancellationToken))
                {
                    throw new UnauthorizedAccessException($"User does not have permission to edit rotation {schedule.RotationId}");
                }

                var currentUser = _userHelper.GetCurrentUser();
                if (currentUser == null)
                {
                    throw new UnauthorizedAccessException("No authenticated user found");
                }

                // If already in the desired state, no action needed
                if (schedule.Evaluator == isPrimary)
                {
                    return true;
                }

                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    if (isPrimary)
                    {
                        // Clear any existing primary evaluator for this rotation/week
                        await ClearPrimaryEvaluatorAsync(schedule.RotationId, schedule.WeekId, cancellationToken);

                        schedule.Evaluator = true;
                        await _auditService.LogPrimaryEvaluatorSetAsync(
                            schedule.MothraId, schedule.RotationId, schedule.WeekId, currentUser.MothraId, instructorScheduleId, cancellationToken);
                    }
                    else
                    {
                        schedule.Evaluator = false;
                        await _auditService.LogPrimaryEvaluatorUnsetAsync(
                            schedule.MothraId, schedule.RotationId, schedule.WeekId, currentUser.MothraId, instructorScheduleId, cancellationToken);
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Set primary evaluator for {MothraId} on rotation {RotationId}, week {WeekId} to {IsPrimary}",
                        schedule.MothraId, schedule.RotationId, schedule.WeekId, isPrimary);

                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary evaluator for instructor schedule {ScheduleId} to {IsPrimary}", instructorScheduleId, isPrimary);
                throw;
            }
        }

        public async Task<bool> CanRemoveInstructorAsync(
            int instructorScheduleId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var schedule = await _context.InstructorSchedules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.InstructorScheduleId == instructorScheduleId, cancellationToken);

                if (schedule == null)
                {
                    return false;
                }

                // If not a primary evaluator, can always remove
                if (!schedule.Evaluator)
                {
                    return true;
                }

                // If primary evaluator, check if there are other instructors for this rotation/week
                var hasOtherInstructors = await _context.InstructorSchedules
                    .AsNoTracking()
                    .Where(s => s.RotationId == schedule.RotationId && s.WeekId == schedule.WeekId &&
                               s.InstructorScheduleId != instructorScheduleId)
                    .AnyAsync(cancellationToken);

                return hasOtherInstructors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if instructor schedule {ScheduleId} can be removed", instructorScheduleId);
                return false;
            }
        }

        public async Task<List<InstructorSchedule>> GetScheduleConflictsAsync(
            string mothraId,
            int[] weekIds,
            int? excludeRotationId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.InstructorSchedules
                    .AsNoTracking()
                    .Where(s => s.MothraId == mothraId && weekIds.Contains(s.WeekId));

                if (excludeRotationId.HasValue)
                {
                    query = query.Where(s => s.RotationId != excludeRotationId.Value);
                }

                return await query.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking schedule conflicts for {MothraId} on weeks {WeekIds}", mothraId, string.Join(",", weekIds));
                throw;
            }
        }

        /// <summary>
        /// Get all instructors currently scheduled for a rotation during specific weeks.
        /// Reserved for future bulk operations and conflict detection features per project plan.
        /// </summary>
        /// <param name="rotationId">Rotation ID</param>
        /// <param name="weekIds">Array of week IDs</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of instructor schedules for the specified rotation and weeks</returns>
        public async Task<List<InstructorSchedule>> GetScheduledInstructorsAsync(
            int rotationId,
            int[] weekIds,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.InstructorSchedules
                    .AsNoTracking()
                    .Where(s => s.RotationId == rotationId && weekIds.Contains(s.WeekId))
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scheduled instructors for rotation {RotationId} on weeks {WeekIds}", rotationId, string.Join(",", weekIds));
                throw;
            }
        }

        /// <summary>
        /// Sets an instructor as the primary evaluator for multiple weeks atomically within a single transaction.
        /// Validates that the instructor is scheduled for all specified weeks and clears existing primary evaluators.
        /// </summary>
        /// <param name="mothraId">The MothraID of the instructor to set as primary evaluator</param>
        /// <param name="rotationId">The rotation ID</param>
        /// <param name="weekIds">Array of week IDs where the instructor should be set as primary</param>
        /// <param name="modifiedByMothraId">MothraID of the user making the change</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, throws exceptions for validation failures or conflicts</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user lacks edit permission for the rotation</exception>
        /// <exception cref="InvalidOperationException">Thrown when instructor is not scheduled for all specified weeks</exception>
        public async Task<bool> SetPrimaryEvaluatorForMultipleWeeksAsync(
            string mothraId,
            int rotationId,
            int[] weekIds,
            string modifiedByMothraId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check permissions
                if (!await _permissionService.HasEditPermissionForRotationAsync(rotationId, cancellationToken))
                {
                    throw new UnauthorizedAccessException($"User does not have permission to edit rotation {rotationId}");
                }

                // Validate that the instructor is actually scheduled for these weeks
                var instructorSchedules = await _context.InstructorSchedules
                    .Where(s => s.MothraId == mothraId && s.RotationId == rotationId && weekIds.Contains(s.WeekId))
                    .ToListAsync(cancellationToken);

                if (instructorSchedules.Count != weekIds.Length)
                {
                    var scheduledWeeks = instructorSchedules.Select(s => s.WeekId).ToArray();
                    var missingWeeks = weekIds.Except(scheduledWeeks).ToArray();
                    throw new InvalidOperationException($"Instructor {mothraId} is not scheduled for weeks: {string.Join(", ", missingWeeks)}");
                }

                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Clear existing primary evaluators for all affected weeks
                    foreach (var weekId in weekIds)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await ClearPrimaryEvaluatorAsync(rotationId, weekId, cancellationToken);
                    }

                    // Set the instructor as primary evaluator for all weeks
                    foreach (var schedule in instructorSchedules)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        schedule.Evaluator = true;

                        // Log the change
                        await _auditService.LogPrimaryEvaluatorSetAsync(
                            mothraId, rotationId, schedule.WeekId, modifiedByMothraId, schedule.InstructorScheduleId, cancellationToken);
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Set {MothraId} as primary evaluator for rotation {RotationId} across {WeekCount} weeks",
                        mothraId, rotationId, weekIds.Length);

                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary evaluator for {MothraId} on rotation {RotationId} for weeks {WeekIds}",
                    mothraId, rotationId, string.Join(",", weekIds));
                throw;
            }
        }

        /// <summary>
        /// Clear the primary evaluator flag for all instructors on a specific rotation/week
        ///
        /// NOTE: Primary evaluator uniqueness is enforced at the application level (not database constraint).
        /// This method ensures only one primary evaluator exists per rotation/week by clearing existing primaries
        /// before setting a new one.
        /// </summary>
        private async Task ClearPrimaryEvaluatorAsync(int rotationId, int weekId, CancellationToken cancellationToken)
        {
            var existingPrimary = await _context.InstructorSchedules
                .Where(s => s.RotationId == rotationId && s.WeekId == weekId && s.Evaluator)
                .ToListAsync(cancellationToken);

            foreach (var schedule in existingPrimary)
            {
                cancellationToken.ThrowIfCancellationRequested();
                schedule.Evaluator = false;
            }

            if (existingPrimary.Any())
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Cleared primary evaluator flag for {Count} instructors on rotation {RotationId}, week {WeekId}",
                    existingPrimary.Count, rotationId, weekId);
            }
        }
    }
}