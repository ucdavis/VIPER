using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;
using Viper.Models.AAUD;
using Viper.Services;

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
        private readonly IEmailService _emailService;
        private readonly EmailNotificationSettings _emailNotificationSettings;
        private readonly IGradYearService _gradYearService;

        public ScheduleEditService(
            ClinicalSchedulerContext context,
            ISchedulePermissionService permissionService,
            IScheduleAuditService auditService,
            ILogger<ScheduleEditService> logger,
            IEmailService emailService,
            IOptions<EmailNotificationSettings> emailNotificationOptions,
            IGradYearService gradYearService,
            IUserHelper? userHelper = null)
        {
            _context = context;
            _permissionService = permissionService;
            _auditService = auditService;
            _logger = logger;
            _emailService = emailService;
            _emailNotificationSettings = emailNotificationOptions.Value;
            _gradYearService = gradYearService;
            _userHelper = userHelper ?? new UserHelper();
        }

        public async Task<List<InstructorSchedule>> AddInstructorAsync(
            string? mothraId,
            int rotationId,
            int[] weekIds,
            int gradYear,
            bool isPrimaryEvaluator = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate permissions and get current user
                var currentUser = await ValidatePermissionsAndGetUserAsync(rotationId, cancellationToken);

                // Validate grad year - only allow current or future years
                var currentGradYear = await _gradYearService.GetCurrentGradYearAsync();
                const int minYear = 2009;
                var maxYear = currentGradYear + 2;

                if (gradYear < minYear || gradYear > maxYear)
                {
                    throw new ArgumentException($"Academic year must be between {minYear} and {maxYear}.", nameof(gradYear));
                }

                if (gradYear < currentGradYear)
                {
                    throw new InvalidOperationException($"Cannot modify schedules for past academic years. Current year is {currentGradYear}, requested year is {gradYear}.");
                }

                // Trim and validate MothraId
                mothraId = mothraId?.Trim();
                if (string.IsNullOrEmpty(mothraId))
                {
                    throw new ArgumentException("MothraId is required", nameof(mothraId));
                }

                // Validate that the person exists in the database
                var personExists = await _context.Persons
                    .AnyAsync(p => p.IdsMothraId == mothraId, cancellationToken);

                if (!personExists)
                {
                    throw new InvalidOperationException($"Person with MothraId {mothraId} not found in the system");
                }

                // Validate that the rotation exists
                var rotationExists = await _context.Rotations
                    .AnyAsync(r => r.RotId == rotationId, cancellationToken);

                if (!rotationExists)
                {
                    throw new InvalidOperationException($"Rotation with ID {rotationId} not found in the system");
                }

                // Validate that all weeks exist and are valid for the grad year
                var existingWeekIds = await _context.Weeks
                    .Where(w => weekIds.Contains(w.WeekId))
                    .Select(w => w.WeekId)
                    .ToListAsync(cancellationToken);

                var missingWeekIds = weekIds.Except(existingWeekIds).ToList();
                if (missingWeekIds.Any())
                {
                    throw new InvalidOperationException($"Week(s) not found in the system: {string.Join(", ", missingWeekIds)}");
                }


                // Check for duplicate scheduling within the same rotation
                var existingSchedules = await _context.InstructorSchedules
                    .Where(s => s.MothraId == mothraId && s.RotationId == rotationId && weekIds.Contains(s.WeekId))
                    .Select(s => new { s.InstructorScheduleId, s.MothraId, s.RotationId, s.WeekId, s.Evaluator })
                    .ToListAsync(cancellationToken);

                if (existingSchedules.Any())
                {
                    var duplicateWeeks = existingSchedules.Select(s => s.WeekId).ToList();
                    var duplicateDetails = string.Join(", ", duplicateWeeks.Select(w => $"Week {w}"));
                    _logger.LogWarning("Instructor {MothraId} is already scheduled for {DuplicateDetails} in rotation {RotationId}",
                        mothraId, duplicateDetails, rotationId);
                    throw new InvalidOperationException($"Instructor {mothraId} is already scheduled for {duplicateDetails} in this rotation");
                }

                // Additional check: Get all existing schedules for this instructor and the specific weeks (regardless of rotation)
                var allExistingForWeeks = await _context.InstructorSchedules
                    .Where(s => s.MothraId == mothraId && weekIds.Contains(s.WeekId))
                    .Select(s => new { s.WeekId, s.RotationId, s.InstructorScheduleId })
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Found {Count} total existing schedules for MothraId='{MothraId}' in weeks [{WeekIds}]: {Details}",
                    allExistingForWeeks.Count, mothraId, string.Join(",", weekIds),
                    string.Join(", ", allExistingForWeeks.Select(s => $"Week {s.WeekId} in Rotation {s.RotationId} (ID: {s.InstructorScheduleId})")));


                // Use Serializable isolation level to prevent race conditions
                using var transaction = await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable, cancellationToken);
                try
                {
                    var createdSchedules = new List<InstructorSchedule>();
                    var removedPrimarySchedules = new Dictionary<int, List<InstructorSchedule>>(); // WeekId -> removed schedules

                    foreach (var weekId in weekIds)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        _logger.LogInformation("Preparing to add InstructorSchedule: MothraId='{MothraId}' (length={Length}), RotationId={RotationId}, WeekId={WeekId}, IsPrimary={IsPrimary}, GradYear={GradYear}",
                            mothraId, mothraId?.Length ?? 0, rotationId, weekId, isPrimaryEvaluator, gradYear);

                        // Double-check for duplicates right before insertion (within transaction)
                        var duplicateCheck = await _context.InstructorSchedules
                            .Where(s => s.MothraId == mothraId && s.RotationId == rotationId && s.WeekId == weekId)
                            .Select(s => new { s.InstructorScheduleId })
                            .FirstOrDefaultAsync(cancellationToken);

                        if (duplicateCheck != null)
                        {
                            _logger.LogError("Race condition detected! Duplicate found during insertion: InstructorScheduleId={ExistingId} for MothraId='{MothraId}', RotationId={RotationId}, WeekId={WeekId}",
                                duplicateCheck.InstructorScheduleId, mothraId, rotationId, weekId);
                            throw new InvalidOperationException($"Instructor {mothraId} is already scheduled for Week {weekId} in this rotation (detected during insertion)");
                        }

                        // If setting as primary evaluator, clear existing primary first (without saving or sending notifications)
                        if (isPrimaryEvaluator)
                        {
                            var removed = await ClearPrimaryEvaluatorAsync(rotationId, weekId, currentUser.MothraId, cancellationToken);
                            if (removed.Any())
                            {
                                removedPrimarySchedules[weekId] = removed;
                            }
                        }

                        // Create new instructor schedule
                        var schedule = new InstructorSchedule
                        {
                            MothraId = mothraId,
                            RotationId = rotationId,
                            WeekId = weekId,
                            Evaluator = isPrimaryEvaluator,
                            ModifiedBy = currentUser.MothraId,
                            ModifiedDate = DateTime.UtcNow
                        };

                        _context.InstructorSchedules.Add(schedule);
                        createdSchedules.Add(schedule);
                    }

                    // Save all schedule entities at once for better performance
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    // Send notifications and log audit entries AFTER successful transaction commit
                    // This ensures we don't send emails for operations that were rolled back

                    // Send notifications for any removed primary evaluators
                    foreach (var (weekId, removedSchedules) in removedPrimarySchedules)
                    {
                        foreach (var removedSchedule in removedSchedules)
                        {
                            await HandlePrimaryEvaluatorRemovalAsync(removedSchedule, currentUser.MothraId, cancellationToken, mothraId);
                        }
                    }

                    // Log audit entries after successful save (so we have InstructorScheduleId)
#pragma warning disable S3267 // Loop contains async operations that cannot be simplified to Select
                    foreach (var schedule in createdSchedules)
                    {
                        await _auditService.LogInstructorAddedAsync(
                            mothraId!, rotationId, schedule.WeekId, currentUser.MothraId, cancellationToken);

                        if (isPrimaryEvaluator)
                        {
                            await _auditService.LogPrimaryEvaluatorSetAsync(
                                mothraId!, rotationId, schedule.WeekId, currentUser.MothraId, cancellationToken);
                        }
                    }
#pragma warning restore S3267

                    // Log summary instead of per-week logs
                    _logger.LogInformation("Added instructor {MothraId} to rotation {RotationId} for {WeekCount} weeks (Primary: {IsPrimary})",
                        mothraId, rotationId, weekIds.Length, isPrimaryEvaluator);

                    return createdSchedules;
                }
                catch (Exception saveEx)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    _logger.LogError(saveEx, "Database save failed for MothraId='{MothraId}', RotationId={RotationId}, WeekIds=[{WeekIds}]",
                        mothraId, rotationId, string.Join(",", weekIds));

                    // Check if this is a database constraint violation (typically a duplicate key error)
                    var errorMessage = saveEx.Message?.ToLower();
                    var innerMessage = saveEx.InnerException?.Message?.ToLower();

                    if ((errorMessage != null && (errorMessage.Contains("duplicate") || errorMessage.Contains("unique") || errorMessage.Contains("constraint") || errorMessage.Contains("violation of primary key"))) ||
                        (innerMessage != null && (innerMessage.Contains("duplicate") || innerMessage.Contains("unique") || innerMessage.Contains("constraint") || innerMessage.Contains("violation of primary key"))))
                    {
                        throw new InvalidOperationException($"Instructor {mothraId} appears to already be scheduled for one or more of the specified weeks. Please refresh the page and try again.", saveEx);
                    }

                    // For other database errors, wrap with context
                    throw new InvalidOperationException($"Database operation failed while adding instructor {mothraId} to rotation {rotationId}. Please try again or contact support if the problem persists.", saveEx);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Re-throw UnauthorizedAccessException without wrapping
                throw;
            }
            catch (InvalidOperationException)
            {
                // Re-throw InvalidOperationException without wrapping (includes "already scheduled" messages)
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding instructor {MothraId} to rotation {RotationId} for weeks {WeekIds}",
                    mothraId, rotationId, string.Join(",", weekIds));
                throw new InvalidOperationException($"Failed to add instructor {mothraId} to rotation {rotationId}. Please try again or contact support if the problem persists.", ex);
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

                // Validate permissions and get current user
                var currentUser = await ValidatePermissionsAndGetUserAsync(schedule.RotationId, cancellationToken);

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
                    // Remove the schedule
                    _context.InstructorSchedules.Remove(schedule);
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    // Send notifications and log audit entries AFTER successful
                    // transaction commit
                    // This ensures we don't send emails for operations that were
                    // rolled back
                    await _auditService.LogInstructorRemovedAsync(
                        schedule.MothraId, schedule.RotationId, schedule.WeekId, currentUser.MothraId, cancellationToken);

                    if (schedule.Evaluator)
                    {
                        await HandlePrimaryEvaluatorRemovalAsync(schedule, currentUser.MothraId, cancellationToken);
                    }

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
            catch (UnauthorizedAccessException)
            {
                // Re-throw UnauthorizedAccessException without wrapping
                throw;
            }
            catch (InvalidOperationException)
            {
                // Re-throw InvalidOperationException without wrapping (includes "Cannot remove primary evaluator" message)
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing instructor schedule {ScheduleId}", instructorScheduleId);
                throw new InvalidOperationException($"Failed to remove instructor schedule. Please try again or contact support if the problem persists.", ex);
            }
        }

        public async Task<(bool success, string? previousPrimaryName)> SetPrimaryEvaluatorAsync(
            int instructorScheduleId,
            bool isPrimary,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var schedule = await _context.InstructorSchedules
                    .Include(s => s.Rotation)
                    .Include(s => s.Person)
                    .Include(s => s.Week)
                    .FirstOrDefaultAsync(s => s.InstructorScheduleId == instructorScheduleId, cancellationToken);

                if (schedule == null)
                {
                    _logger.LogWarning("InstructorSchedule {ScheduleId} not found for primary evaluator update", instructorScheduleId);
                    return (false, null);
                }

                // Validate permissions and get current user
                var currentUser = await ValidatePermissionsAndGetUserAsync(schedule.RotationId, cancellationToken);

                // If already in the desired state, no action needed
                if (schedule.Evaluator == isPrimary)
                {
                    return (true, null);
                }

                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    string? previousPrimaryName = null;
                    List<InstructorSchedule> removedPrimarySchedules = [];

                    if (isPrimary)
                    {
                        // Find existing primary evaluator before clearing
                        var existingPrimary = await _context.InstructorSchedules
                            .Include(s => s.Person)
                            .Where(s => s.RotationId == schedule.RotationId && s.WeekId == schedule.WeekId && s.Evaluator)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (existingPrimary != null && existingPrimary.Person != null)
                        {
                            previousPrimaryName = existingPrimary.Person.PersonDisplayFullName ??
                                $"{existingPrimary.Person.PersonDisplayLastName}, {existingPrimary.Person.PersonDisplayFirstName}";
                        }

                        // Clear any existing primary evaluator for this rotation/week (without saving or sending notifications)
                        removedPrimarySchedules = await ClearPrimaryEvaluatorAsync(schedule.RotationId, schedule.WeekId, currentUser.MothraId, cancellationToken);

                        schedule.Evaluator = true;
                        schedule.ModifiedBy = currentUser.MothraId;
                        schedule.ModifiedDate = DateTime.UtcNow;
                    }
                    else
                    {
                        schedule.Evaluator = false;
                        schedule.ModifiedBy = currentUser.MothraId;
                        schedule.ModifiedDate = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    // Send notifications and log audit entries AFTER successful transaction commit
                    // This ensures we don't send emails for operations that were rolled back
                    if (isPrimary)
                    {
                        // Send notifications for any removed primary evaluators
                        foreach (var removedSchedule in removedPrimarySchedules)
                        {
                            await HandlePrimaryEvaluatorRemovalAsync(removedSchedule, currentUser.MothraId, cancellationToken, schedule.MothraId);
                        }

                        await _auditService.LogPrimaryEvaluatorSetAsync(
                            schedule.MothraId, schedule.RotationId, schedule.WeekId, currentUser.MothraId, cancellationToken);
                    }
                    else
                    {
                        await HandlePrimaryEvaluatorRemovalAsync(schedule, currentUser.MothraId, cancellationToken);
                    }

                    _logger.LogInformation("Set primary evaluator for {MothraId} on rotation {RotationId}, week {WeekId} to {IsPrimary}",
                        schedule.MothraId, schedule.RotationId, schedule.WeekId, isPrimary);

                    return (true, previousPrimaryName);
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
                throw new InvalidOperationException($"Failed to update primary evaluator status. Please try again or contact support if the problem persists.", ex);
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

        /// <summary>
        /// Gets other rotation schedules for an instructor during specified weeks.
        /// This is informational only - instructors can be scheduled for multiple rotations in the same week.
        /// </summary>
        /// <param name="mothraId">Instructor's MothraID</param>
        /// <param name="weekIds">Array of week IDs to check</param>
        /// <param name="gradYear">Graduate year</param>
        /// <param name="excludeRotationId">Optional rotation ID to exclude from results</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of instructor schedules for other rotations during the specified weeks</returns>
        public async Task<List<InstructorSchedule>> GetOtherRotationSchedulesAsync(
            string? mothraId,
            int[] weekIds,
            int gradYear,
            int? excludeRotationId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.InstructorSchedules
                    .AsNoTracking()
                    .Include(s => s.Rotation)  // Include rotation data for name lookup
                    .Join(_context.WeekGradYears,
                        schedule => schedule.WeekId,
                        weekGradYear => weekGradYear.WeekId,
                        (schedule, weekGradYear) => new { schedule, weekGradYear })
                    .Where(joined => joined.schedule.MothraId == mothraId &&
                                   weekIds.Contains(joined.schedule.WeekId) &&
                                   joined.weekGradYear.GradYear == gradYear)
                    .Select(joined => joined.schedule);

                if (excludeRotationId.HasValue)
                {
                    query = query.Where(s => s.RotationId != excludeRotationId.Value);
                }

                return await query.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking other rotation schedules for {MothraId} on weeks {WeekIds} for grad year {GradYear}",
                    mothraId, string.Join(",", weekIds), gradYear);
                throw new InvalidOperationException($"Failed to retrieve other rotation schedules. Please try again or contact support if the problem persists.", ex);
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
                throw new InvalidOperationException($"Failed to retrieve scheduled instructors. Please try again or contact support if the problem persists.", ex);
            }
        }


        /// <summary>
        /// Validates that the current user has edit permissions for the specified rotation
        /// </summary>
        /// <param name="rotationId">Rotation ID to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The current authenticated user</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user lacks permission or is not authenticated</exception>
        private async Task<AaudUser> ValidatePermissionsAndGetUserAsync(int rotationId, CancellationToken cancellationToken)
        {
            if (!await _permissionService.HasEditPermissionForRotationAsync(rotationId, cancellationToken))
            {
                throw new UnauthorizedAccessException($"User does not have permission to edit rotation {rotationId}");
            }

            var currentUser = _userHelper.GetCurrentUser();
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("No authenticated user found");
            }

            return currentUser;
        }

        /// <summary>
        /// Clear the primary evaluator flag for all instructors on a specific rotation/week
        ///
        /// NOTE: Primary evaluator uniqueness is enforced at the application level (not database constraint).
        /// This method ensures only one primary evaluator exists per rotation/week by clearing existing primaries
        /// before setting a new one.
        ///
        /// IMPORTANT: This method does NOT save changes or send notifications. The caller is responsible for
        /// saving changes within their transaction and sending notifications after commit.
        /// </summary>
        /// <returns>List of schedules that had their primary evaluator flag removed</returns>
        private async Task<List<InstructorSchedule>> ClearPrimaryEvaluatorAsync(int rotationId, int weekId, string modifiedByMothraId, CancellationToken cancellationToken)
        {
            var existingPrimary = await _context.InstructorSchedules
                .Include(s => s.Person)
                .Include(s => s.Rotation)
                .Include(s => s.Week)
                .Where(s => s.RotationId == rotationId && s.WeekId == weekId && s.Evaluator)
                .ToListAsync(cancellationToken);

            foreach (var schedule in existingPrimary)
            {
                cancellationToken.ThrowIfCancellationRequested();
                schedule.Evaluator = false;
                schedule.ModifiedBy = modifiedByMothraId;
                schedule.ModifiedDate = DateTime.UtcNow;
            }

            if (existingPrimary.Any())
            {
                _logger.LogDebug("Clearing primary evaluator flag for {Count} instructors on rotation {RotationId}, week {WeekId}",
                    existingPrimary.Count, rotationId, weekId);
            }

            return existingPrimary;
        }

        /// <summary>
        /// Handles the complete process of removing a primary evaluator status,
        /// including audit logging and email notifications
        /// </summary>
        private async Task HandlePrimaryEvaluatorRemovalAsync(
            InstructorSchedule schedule,
            string currentUserMothraId,
            CancellationToken cancellationToken,
            string? newPrimaryMothraId = null)
        {
            // Log the primary evaluator removal
            await _auditService.LogPrimaryEvaluatorUnsetAsync(
                schedule.MothraId, schedule.RotationId, schedule.WeekId, currentUserMothraId, cancellationToken);

            // Send email notification
            await SendPrimaryEvaluatorRemovedNotificationAsync(schedule, currentUserMothraId, cancellationToken, newPrimaryMothraId);
        }

        /// <summary>
        /// Send email notification when a primary evaluator is removed
        /// Matches the ColdFusion system's notification format and recipients
        /// </summary>
        private async Task SendPrimaryEvaluatorRemovedNotificationAsync(InstructorSchedule schedule, string modifiedByMothraId, CancellationToken cancellationToken, string? newPrimaryMothraId = null)
        {
            try
            {
                // Get instructor name - attempt to lookup, but gracefully handle if not found
                var instructorText = "";
                try
                {
                    var person = await _context.Persons
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.IdsMothraId == schedule.MothraId, cancellationToken);

                    if (person != null)
                    {
                        instructorText = $"({person.PersonDisplayFirstName} {person.PersonDisplayLastName}) ";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve instructor name for {MothraId} in email notification", schedule.MothraId);
                }

                // Get rotation name - explicitly load if not already loaded
                string rotationName;
                if (schedule.Rotation != null)
                {
                    rotationName = schedule.Rotation.Name;
                }
                else
                {
                    // Lazy-load rotation if navigation property is null
                    await _context.Entry(schedule)
                        .Reference(s => s.Rotation)
                        .LoadAsync(cancellationToken);
                    rotationName = schedule.Rotation?.Name ?? "Unknown Rotation";
                }

                // Get week information - use week start date to find week number from WeekGradYear
                var weekDisplay = schedule.WeekId.ToString(); // Default fallback
                try
                {
                    var weekGradYear = await _context.WeekGradYears
                        .Include(wgy => wgy.Week)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(wgy => wgy.WeekId == schedule.WeekId, cancellationToken);

                    if (weekGradYear != null)
                    {
                        weekDisplay = weekGradYear.WeekNum.ToString();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve week number for {WeekId} in email notification", schedule.WeekId);
                }

                // Get new primary evaluator name if this is a replacement
                var newPrimaryText = "";
                if (!string.IsNullOrEmpty(newPrimaryMothraId))
                {
                    try
                    {
                        var newPrimaryPerson = await _context.Persons
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.IdsMothraId == newPrimaryMothraId, cancellationToken);

                        if (newPrimaryPerson != null)
                        {
                            newPrimaryText = newPrimaryPerson.PersonDisplayFullName ??
                                $"{newPrimaryPerson.PersonDisplayLastName}, {newPrimaryPerson.PersonDisplayFirstName}";
                        }
                        else
                        {
                            newPrimaryText = newPrimaryMothraId; // Fallback to MothraId
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not retrieve new primary evaluator name for {MothraId} in email notification", newPrimaryMothraId);
                        newPrimaryText = newPrimaryMothraId; // Fallback to MothraId
                    }
                }

                // Get current user name for the email
                var currentUserText = "";
                try
                {
                    var currentUserPerson = await _context.Persons
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.IdsMothraId == modifiedByMothraId, cancellationToken);

                    if (currentUserPerson != null)
                    {
                        currentUserText = currentUserPerson.PersonDisplayFullName ??
                            $"{currentUserPerson.PersonDisplayLastName}, {currentUserPerson.PersonDisplayFirstName}";
                    }
                    else
                    {
                        currentUserText = modifiedByMothraId; // Fallback to MothraId
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve current user name for {MothraId} in email notification", modifiedByMothraId);
                    currentUserText = modifiedByMothraId; // Fallback to MothraId
                }

                // Format email body - different message for replacement vs removal, including who made the change
                var emailBody = !string.IsNullOrEmpty(newPrimaryMothraId)
                    ? $"Primary evaluator {instructorText}removed from {rotationName} week {weekDisplay} and replaced by {newPrimaryText} by {currentUserText}"
                    : $"Primary evaluator {instructorText}removed from {rotationName} week {weekDisplay} by {currentUserText}";

                // Send email notifications to all configured recipients
                var notificationConfig = _emailNotificationSettings.PrimaryEvaluatorRemoved;
                foreach (var recipient in notificationConfig.To)
                {
                    await _emailService.SendEmailAsync(
                        to: recipient,
                        subject: notificationConfig.Subject,
                        body: emailBody,
                        isHtml: false,
                        from: notificationConfig.From
                    );
                }

                _logger.LogInformation("Primary evaluator removal notification sent for {MothraId} from {RotationName} week {WeekDisplay}",
                    schedule.MothraId, rotationName, weekDisplay);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the transaction - email is secondary to the schedule removal
                _logger.LogError(ex, "Failed to send primary evaluator removal notification for {MothraId} from rotation {RotationId} week {WeekId}",
                    schedule.MothraId, schedule.RotationId, schedule.WeekId);
            }
        }
    }
}
