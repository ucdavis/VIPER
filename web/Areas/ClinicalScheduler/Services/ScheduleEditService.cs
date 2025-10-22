using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;
using Viper.Services;
using VIPER.Areas.ClinicalScheduler.Utilities;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for editing instructor schedules with business logic and validation
    /// </summary>
    public class ScheduleEditService : IScheduleEditService
    {
        private readonly ClinicalSchedulerContext _context;
        private readonly IScheduleAuditService _auditService;
        private readonly ILogger<ScheduleEditService> _logger;
        private readonly IEmailService _emailService;
        private readonly EmailNotificationSettings _emailNotificationSettings;
        private readonly IGradYearService _gradYearService;
        private readonly IPermissionValidator _permissionValidator;
        private readonly IConfiguration _configuration;

        public ScheduleEditService(
            ClinicalSchedulerContext context,
            IScheduleAuditService auditService,
            ILogger<ScheduleEditService> logger,
            IEmailService emailService,
            IOptions<EmailNotificationSettings> emailNotificationOptions,
            IGradYearService gradYearService,
            IPermissionValidator permissionValidator,
            IConfiguration configuration)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
            _emailService = emailService;
            _emailNotificationSettings = emailNotificationOptions.Value;
            _gradYearService = gradYearService;
            _permissionValidator = permissionValidator;
            _configuration = configuration;
        }

        public async Task<List<InstructorSchedule>> AddInstructorAsync(
            string? mothraId,
            int rotationId,
            int[] weekIds,
            int gradYear,
            bool isPrimaryEvaluator = false,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting AddInstructorAsync for {WeekCount} weeks", weekIds.Length);

            try
            {
                // Validate that the rotation exists BEFORE permission check (CodeQL security requirement)
                var rotationExists = await _context.Rotations
                    .AnyAsync(r => r.RotId == rotationId, cancellationToken);

                if (!rotationExists)
                {
                    throw new InvalidOperationException($"Rotation with ID {rotationId} not found in the system");
                }

                // Validate permissions and get current user
                // For adding, check if user is adding themselves with EditOwnSchedule permission
                var currentUser = await _permissionValidator.ValidateEditPermissionAndGetUserAsync(rotationId, mothraId ?? "", cancellationToken);

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
                        LogSanitizer.SanitizeId(mothraId), duplicateDetails, rotationId);
                    throw new InvalidOperationException($"Instructor {mothraId} is already scheduled for {duplicateDetails} in this rotation");
                }

                // Additional check: Get all existing schedules for this instructor and the specific weeks (regardless of rotation)
                var allExistingForWeeks = await _context.InstructorSchedules
                    .Where(s => s.MothraId == mothraId && weekIds.Contains(s.WeekId))
                    .Select(s => new { s.WeekId, s.RotationId, s.InstructorScheduleId })
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Found {Count} total existing schedules for MothraId='{MothraId}' in weeks [{WeekIds}]: {Details}",
                    allExistingForWeeks.Count, LogSanitizer.SanitizeId(mothraId), string.Join(",", weekIds),
                    string.Join(", ", allExistingForWeeks.Select(s => $"Week {s.WeekId} in Rotation {s.RotationId} (ID: {s.InstructorScheduleId})")));


                // Use Serializable isolation level to prevent race conditions
                var result = await ExecuteInTransactionAsync(async (cancellationToken) =>
                {
                    var createdSchedules = new List<InstructorSchedule>();
                    var removedPrimarySchedules = new Dictionary<int, List<InstructorSchedule>>(); // WeekId -> removed schedules

                    foreach (var weekId in weekIds)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        _logger.LogDebug("Preparing to add InstructorSchedule: MothraId='{MothraId}' (length={Length}), RotationId={RotationId}, WeekId={WeekId}, IsPrimary={IsPrimary}, GradYear={GradYear}",
                            LogSanitizer.SanitizeId(mothraId), mothraId?.Length ?? 0, rotationId, weekId, isPrimaryEvaluator, gradYear);

                        // Double-check for duplicates right before insertion (within transaction)
                        var duplicateCheck = await _context.InstructorSchedules
                            .Where(s => s.MothraId == mothraId && s.RotationId == rotationId && s.WeekId == weekId)
                            .Select(s => new { s.InstructorScheduleId })
                            .FirstOrDefaultAsync(cancellationToken);

                        if (duplicateCheck != null)
                        {
                            _logger.LogError("Race condition detected! Duplicate found during insertion: InstructorScheduleId={ExistingId} for MothraId='{MothraId}', RotationId={RotationId}, WeekId={WeekId}",
                                duplicateCheck.InstructorScheduleId, LogSanitizer.SanitizeId(mothraId), rotationId, weekId);
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

                    return (createdSchedules, removedPrimarySchedules);
                }, System.Data.IsolationLevel.Serializable, cancellationToken);

                var (createdSchedules, removedPrimarySchedules) = result;

                // Send notifications and log audit entries AFTER successful transaction commit
                // This ensures we don't send emails for operations that were rolled back
                // Post-transaction operations are wrapped in try-catch to prevent perceived failures
                try
                {
                    // Send notifications for any removed primary evaluators
                    foreach (var (_, removedSchedules) in removedPrimarySchedules)
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
                }
                catch (Exception postTransactionEx)
                {
                    // Log warning but don't fail the operation - the database changes were successful
                    _logger.LogWarning(postTransactionEx, "Post-transaction operations failed for instructor {MothraId} in rotation {RotationId}, but database changes were successful",
                        LogSanitizer.SanitizeId(mothraId), rotationId);
                }

                _logger.LogInformation("Successfully added instructor {MothraId} to rotation {RotationId} for {WeekCount} weeks (Primary: {IsPrimary})",
                    LogSanitizer.SanitizeId(mothraId), rotationId, weekIds.Length, isPrimaryEvaluator);

                return createdSchedules;
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
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Database save failed for MothraId='{MothraId}', RotationId={RotationId}, WeekIds=[{WeekIds}]",
                    LogSanitizer.SanitizeId(mothraId), rotationId, string.Join(",", weekIds));

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

        public async Task<(bool success, bool wasPrimaryEvaluator, string? instructorName)> RemoveInstructorScheduleAsync(
            int instructorScheduleId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting RemoveInstructorScheduleAsync for schedule {ScheduleId}", instructorScheduleId);

            try
            {
                var schedule = await _context.InstructorSchedules
                    .Include(s => s.Rotation)
                    .Include(s => s.Person)
                    .FirstOrDefaultAsync(s => s.InstructorScheduleId == instructorScheduleId, cancellationToken);

                if (schedule == null)
                {
                    _logger.LogWarning("InstructorSchedule {ScheduleId} not found for removal", instructorScheduleId);
                    return (false, false, null);
                }

                // Validate that rotation exists BEFORE permission check (CodeQL security requirement)
                // This validation is defensive - the schedule.Rotation should exist via FK constraint
                var rotationExists = await _context.Rotations
                    .AnyAsync(r => r.RotId == schedule.RotationId, cancellationToken);

                if (!rotationExists)
                {
                    _logger.LogError("Data integrity issue: InstructorSchedule {ScheduleId} references non-existent rotation {RotationId}",
                        instructorScheduleId, schedule.RotationId);
                    throw new InvalidOperationException($"Associated rotation not found for instructor schedule {instructorScheduleId}");
                }

                // Validate permissions and get current user
                // For removal, check if user is editing their own schedule
                var currentUser = await _permissionValidator.ValidateEditPermissionAndGetUserAsync(schedule.RotationId, schedule.MothraId, cancellationToken);

                // Capture whether this was a primary evaluator and instructor name before removal
                var wasPrimaryEvaluator = schedule.Evaluator;

                // Handle instructor name retrieval - if Person is null, try to get it from the MothraId
                string? instructorName = null;
                if (schedule.Person != null)
                {
                    instructorName = !string.IsNullOrEmpty(schedule.Person.PersonDisplayFullName)
                        ? schedule.Person.PersonDisplayFullName
                        : $"{schedule.Person.PersonDisplayLastName}, {schedule.Person.PersonDisplayFirstName}";
                }
                else
                {
                    // If Person wasn't loaded, try to fetch it separately
                    var person = await _context.Persons
                        .FirstOrDefaultAsync(p => p.IdsMothraId == schedule.MothraId, cancellationToken);

                    if (person != null)
                    {
                        instructorName = !string.IsNullOrEmpty(person.PersonDisplayFullName)
                            ? person.PersonDisplayFullName
                            : $"{person.PersonDisplayLastName}, {person.PersonDisplayFirstName}";
                    }
                    else
                    {
                        // Fallback to MothraId if we can't find the person
                        instructorName = schedule.MothraId;
                        _logger.LogWarning("Could not load Person data for MothraId {MothraId} during removal", LogSanitizer.SanitizeId(schedule.MothraId));
                    }
                }

                await ExecuteInTransactionAsync(async (cancellationToken) =>
                {
                    // Remove the schedule
                    _context.InstructorSchedules.Remove(schedule);
                    await _context.SaveChangesAsync(cancellationToken);
                    return true;
                }, cancellationToken);

                // Send notifications and log audit entries AFTER successful transaction commit
                // This ensures we don't send emails for operations that were rolled back
                // Post-transaction operations are wrapped in try-catch to prevent perceived failures
                try
                {
                    await _auditService.LogInstructorRemovedAsync(
                        schedule.MothraId, schedule.RotationId, schedule.WeekId, currentUser.MothraId, cancellationToken);

                    if (wasPrimaryEvaluator)
                    {
                        await HandlePrimaryEvaluatorRemovalAsync(schedule, currentUser.MothraId, cancellationToken);
                    }
                }
                catch (Exception postTransactionEx)
                {
                    // Log warning but don't fail the operation - the database changes were successful
                    _logger.LogWarning(postTransactionEx, "Post-transaction operations failed for instructor removal {ScheduleId}, but database changes were successful",
                        instructorScheduleId);
                }

                _logger.LogInformation("Successfully removed instructor {MothraId} from rotation {RotationId}, week {WeekId} (WasPrimary: {WasPrimary})",
                    LogSanitizer.SanitizeId(schedule.MothraId), schedule.RotationId, schedule.WeekId, wasPrimaryEvaluator);

                return (true, wasPrimaryEvaluator, instructorName);
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
            CancellationToken cancellationToken = default,
            bool requiresPrimaryEvaluator = false)
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
                var currentUser = await _permissionValidator.ValidateEditPermissionAndGetUserAsync(schedule.RotationId, schedule.MothraId, cancellationToken);

                // If already in the desired state, no action needed
                if (schedule.Evaluator == isPrimary)
                {
                    return (true, null);
                }

                var result = await ExecuteInTransactionAsync(async (cancellationToken) =>
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

                    return (previousPrimaryName, removedPrimarySchedules);
                }, cancellationToken);

                var (previousPrimaryName, removedPrimarySchedules) = result;

                // Send notifications and log audit entries AFTER successful transaction commit
                // This ensures we don't send emails for operations that were rolled back
                // Post-transaction operations are wrapped in try-catch to prevent perceived failures
                try
                {
                    if (isPrimary)
                    {
                        // Send notifications for any removed primary evaluators
                        foreach (var removedSchedule in removedPrimarySchedules)
                        {
                            await HandlePrimaryEvaluatorRemovalAsync(removedSchedule, currentUser.MothraId, cancellationToken, schedule.MothraId, requiresPrimaryEvaluator);
                        }

                        await _auditService.LogPrimaryEvaluatorSetAsync(
                            schedule.MothraId, schedule.RotationId, schedule.WeekId, currentUser.MothraId, cancellationToken);
                    }
                    else
                    {
                        await HandlePrimaryEvaluatorRemovalAsync(schedule, currentUser.MothraId, cancellationToken, null, requiresPrimaryEvaluator);
                    }
                }
                catch (Exception postTransactionEx)
                {
                    // Log warning but don't fail the operation - the database changes were successful
                    _logger.LogWarning(postTransactionEx, "Post-transaction operations failed for primary evaluator update {ScheduleId}, but database changes were successful",
                        instructorScheduleId);
                }

                _logger.LogInformation("Set primary evaluator for {MothraId} on rotation {RotationId}, week {WeekId} to {IsPrimary}",
                    LogSanitizer.SanitizeId(schedule.MothraId), schedule.RotationId, schedule.WeekId, isPrimary);

                return (true, previousPrimaryName);
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

                // All instructors can now be removed, including primary evaluators
                return true;
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
                    .Include(s => s.Week)
                        .ThenInclude(w => w.WeekGradYears)
                    .Where(s => s.MothraId == mothraId &&
                                weekIds.Contains(s.WeekId) &&
                                s.Week.WeekGradYears.Any(wgy => wgy.GradYear == gradYear));

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
            string? newPrimaryMothraId = null,
            bool requiresPrimaryEvaluator = false)
        {
            // Log the primary evaluator removal
            await _auditService.LogPrimaryEvaluatorUnsetAsync(
                schedule.MothraId, schedule.RotationId, schedule.WeekId, currentUserMothraId, cancellationToken);

            // Send email notification
            await SendPrimaryEvaluatorRemovedNotificationAsync(schedule, currentUserMothraId, cancellationToken, newPrimaryMothraId, requiresPrimaryEvaluator);
        }

        /// <summary>
        /// Send email notification when a primary evaluator is removed
        /// Matches the ColdFusion system's notification format and recipients
        /// </summary>
        private async Task SendPrimaryEvaluatorRemovedNotificationAsync(InstructorSchedule schedule, string modifiedByMothraId, CancellationToken cancellationToken, string? newPrimaryMothraId = null, bool requiresPrimaryEvaluator = false)
        {
            try
            {
                // Only send email notification if primary evaluator is removed without replacement
                // Do not send email when primary evaluator is replaced by another instructor
                if (!string.IsNullOrEmpty(newPrimaryMothraId))
                {
                    _logger.LogDebug("Skipping email notification for primary evaluator replacement. Old: {OldMothraId}, New: {NewMothraId}, Rotation: {RotationId}, Week: {WeekId}",
                        LogSanitizer.SanitizeId(schedule.MothraId), LogSanitizer.SanitizeId(newPrimaryMothraId), schedule.RotationId, schedule.WeekId);
                    return;
                }
                // Get base URL for links
                var configuredBaseUrl = _configuration["BaseUrl"];
                var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl) ? null : configuredBaseUrl;

                // Get instructor information
                var instructorName = "Unknown Instructor";
                try
                {
                    var instructorPerson = await _context.Persons
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.IdsMothraId == schedule.MothraId, cancellationToken);

                    if (instructorPerson != null)
                    {
                        var fullName = instructorPerson.PersonDisplayFullName ??
                            $"{instructorPerson.PersonDisplayLastName}, {instructorPerson.PersonDisplayFirstName}";

                        // If the constructed name is empty or just ", ", fall back to "Unknown Instructor"
                        if (!string.IsNullOrWhiteSpace(fullName) && fullName.Trim() != ",")
                        {
                            instructorName = fullName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve instructor name for {MothraId} in email notification", LogSanitizer.SanitizeId(schedule.MothraId));
                }

                // Get rotation information - explicitly load if not already loaded
                string rotationName;
                if (schedule.Rotation != null)
                {
                    rotationName = schedule.Rotation.Name ?? "Unknown Rotation";
                }
                else
                {
                    await _context.Entry(schedule)
                        .Reference(s => s.Rotation)
                        .LoadAsync(cancellationToken);
                    rotationName = schedule.Rotation?.Name ?? "Unknown Rotation";
                }

                // Get week information
                var weekNumber = schedule.WeekId.ToString(); // Default fallback
                try
                {
                    var weekGradYear = await _context.WeekGradYears
                        .Include(wgy => wgy.Week)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(wgy => wgy.WeekId == schedule.WeekId, cancellationToken);

                    if (weekGradYear != null)
                    {
                        weekNumber = weekGradYear.WeekNum.ToString();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve week number for {WeekId} in email notification", schedule.WeekId);
                }


                // Get modifier information with email
                var modifierName = modifiedByMothraId; // Fallback
                var modifierEmail = "";
                try
                {
                    var modifierPerson = await _context.Persons
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.IdsMothraId == modifiedByMothraId, cancellationToken);

                    if (modifierPerson != null)
                    {
                        modifierName = modifierPerson.PersonDisplayFullName ??
                            $"{modifierPerson.PersonDisplayLastName}, {modifierPerson.PersonDisplayFirstName}";
                        modifierEmail = modifierPerson.IdsMailId;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve modifier information for {MothraId} in email notification", modifiedByMothraId);
                }

                // Use the passed requiresPrimaryEvaluator parameter (determined by frontend)

                // Build HTML email body with output encoding
                var rotationLinkRaw = baseUrl is null
                    ? $"/ClinicalScheduler/rotation/{schedule.RotationId}"
                    : $"{baseUrl}/ClinicalScheduler/rotation/{schedule.RotationId}";
                var instructorNameHtml = WebUtility.HtmlEncode(instructorName);
                var rotationNameHtml = WebUtility.HtmlEncode(rotationName);
                var weekNumberHtml = WebUtility.HtmlEncode(weekNumber);
                var modifierNameHtml = WebUtility.HtmlEncode(modifierName);
                var modifierEmailHtml = WebUtility.HtmlEncode(modifierEmail);
                var rotationLinkHtml = WebUtility.HtmlEncode(rotationLinkRaw);

                var modifierDisplay = !string.IsNullOrEmpty(modifierEmail)
                    ? $"<a href=\"mailto:{modifierEmailHtml}\">{modifierNameHtml}</a>"
                    : modifierNameHtml;

                var replacementRow = "";
                var warningDiv = requiresPrimaryEvaluator
                    ? @"<table cellpadding=""12"" cellspacing=""0"" border=""0"" style=""background-color: #fff3cd; border: 2px solid #ffc107; margin-top: 16px;"">
                        <tr><td style=""color: #856404; font-size: 14px;""><b>⚠️ Note: This week requires a primary evaluator.</b></td></tr>
                      </table>"
                    : "";

                var emailSubject = $"Primary Evaluator Removed - {rotationName} - Week {weekNumber}";
                var emailTitleHtml = WebUtility.HtmlEncode("Primary Evaluator Removed");

                var emailBody = $@"
<html>
<head>
    <title>Primary Evaluator Removed</title>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; font-size: 14px; line-height: 1.6; color: #333333; background-color: #f8f9fa;"">
    <table cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color: #f8f9fa;"">
        <tr>
            <td style=""padding: 20px;"">
                <table cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""padding: 30px;"">
                            <h3 style=""margin: 0 0 20px 0; color: #022851; font-size: 20px; font-weight: bold;"">{emailTitleHtml}</h3>

                            <table cellpadding=""12"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color: #ffffff; border: 1px solid #e9ecef; border-radius: 4px;"">
                                <tr style=""background-color: #f8f9fa;"">
                                    <td style=""font-weight: bold; color: #495057; width: 140px; border-bottom: 1px solid #e9ecef;"">Instructor:</td>
                                    <td style=""color: #212529; border-bottom: 1px solid #e9ecef;"">{instructorNameHtml}</td>
                                </tr>
                                {replacementRow}
                                <tr style=""background-color: #f8f9fa;"">
                                    <td style=""font-weight: bold; color: #495057; width: 140px; border-bottom: 1px solid #e9ecef;"">Rotation:</td>
                                    <td style=""color: #212529; border-bottom: 1px solid #e9ecef;""><a href=""{rotationLinkHtml}"" style=""color: #007bff; text-decoration: none;"">{rotationNameHtml}</a></td>
                                </tr>
                                <tr>
                                    <td style=""font-weight: bold; color: #495057; width: 140px; border-bottom: 1px solid #e9ecef;"">Week:</td>
                                    <td style=""color: #212529; border-bottom: 1px solid #e9ecef;"">Week {weekNumberHtml}</td>
                                </tr>
                                <tr style=""background-color: #f8f9fa;"">
                                    <td style=""font-weight: bold; color: #495057; width: 140px; border-bottom: 1px solid #e9ecef;"">Modified by:</td>
                                    <td style=""color: #212529; border-bottom: 1px solid #e9ecef;"">{modifierDisplay}</td>
                                </tr>
                            </table>

                            {warningDiv}
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                // Send email notifications to all configured recipients
                var notificationConfig = _emailNotificationSettings.PrimaryEvaluatorRemoved;
                var recipients = notificationConfig.To ?? new List<string>();
                if (recipients.Any())
                {
                    foreach (var recipient in recipients)
                    {
                        await _emailService.SendEmailAsync(
                            to: recipient,
                            subject: emailSubject,
                            body: emailBody,
                            isHtml: true,
                            from: notificationConfig.From
                        );
                    }
                    _logger.LogInformation("Primary evaluator removal notification sent to {Count} recipient(s) for {MothraId} from {RotationName} week {WeekNumber}",
                        recipients.Count, LogSanitizer.SanitizeId(schedule.MothraId), rotationName, weekNumber);
                }
                else
                {
                    _logger.LogInformation("No notification recipients configured for Primary Evaluator Removal; skipped email. Rotation={RotationName}, Week={WeekNumber}", rotationName, weekNumber);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the transaction - email is secondary to the schedule removal
                _logger.LogError(ex, "Failed to send primary evaluator removal notification for {MothraId} from rotation {RotationId} week {WeekId} (rotation: {RotationName})",
                    LogSanitizer.SanitizeId(schedule.MothraId), schedule.RotationId, schedule.WeekId, schedule.Rotation?.Name ?? "Unknown");
            }
        }

        /// <summary>
        /// Executes an operation within a database transaction.
        /// This method can be overridden in tests to bypass transaction handling.
        /// </summary>
        /// <typeparam name="T">The return type of the operation</typeparam>
        /// <param name="operation">The operation to execute within the transaction</param>
        /// <param name="isolationLevel">The isolation level for the transaction</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the operation</returns>
        protected virtual async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            System.Data.IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            try
            {
                var result = await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Executes an operation within a database transaction using the default isolation level.
        /// This method can be overridden in tests to bypass transaction handling.
        /// </summary>
        /// <typeparam name="T">The return type of the operation</typeparam>
        /// <param name="operation">The operation to execute within the transaction</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the operation</returns>
        protected virtual async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            return await ExecuteInTransactionAsync(operation, System.Data.IsolationLevel.Unspecified, cancellationToken);
        }
    }

}
