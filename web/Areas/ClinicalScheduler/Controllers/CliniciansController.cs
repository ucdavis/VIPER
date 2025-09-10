using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Areas.Curriculum.Services;
using Viper.Models.ClinicalScheduler;
using Web.Authorization;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("ClinicalScheduler")]
    [Permission(Allow = ClinicalSchedulePermissions.Base)]
    public class CliniciansController : BaseClinicalSchedulerController
    {
        #region Constants

        /// <summary>
        /// Number of grad years to look back for active clinicians (current + previous year)
        /// </summary>
        private const int ACTIVE_CLINICIANS_GRAD_YEARS_BACK = 1;

        #endregion

        private readonly ClinicalSchedulerContext _context;
        private readonly IWeekService _weekService;
        private readonly IPersonService _personService;
        private readonly IUserHelper _userHelper;

        public CliniciansController(ClinicalSchedulerContext context, ILogger<CliniciansController> logger,
            IGradYearService gradYearService, IWeekService weekService, IPersonService personService,
            IUserHelper? userHelper = null)
            : base(gradYearService, logger)
        {
            _context = context;
            _weekService = weekService;
            _personService = personService;
            _userHelper = userHelper ?? new UserHelper();
        }

        /// <summary>
        /// Get a list of clinicians based on the specified filters
        /// </summary>
        /// <param name="year">Year to filter clinicians by (optional, defaults to current year behavior)</param>
        /// <param name="includeAllAffiliates">If true, includes all active employee affiliates from AAUD; if false, only returns clinicians who have been scheduled</param>
        /// <param name="viewContext">The view context (clinician or rotation) to determine permission filtering</param>
        /// <returns>List of clinicians with their basic info</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClinicians([FromQuery] int? year = null, [FromQuery] bool includeAllAffiliates = false, [FromQuery] string? viewContext = null)
        {
            var currentGradYear = await GetCurrentGradYearAsync();
            var targetYear = year ?? currentGradYear;

            // Normalize and validate viewContext
            var normalizedViewContext = NormalizeViewContext(viewContext);

            _logger.LogDebug("GetClinicians endpoint called with year: {Year}, includeAllAffiliates: {IncludeAllAffiliates}, viewContext: {ViewContext} (normalized: {NormalizedViewContext})", targetYear, includeAllAffiliates, viewContext, normalizedViewContext);

            if (targetYear >= currentGradYear)
            {
                // Current or future year: use PersonService to get clinicians from Clinical Scheduler data
                _logger.LogDebug("Using PersonService to get clinicians for current/future year {Year} (includeAllAffiliates: {IncludeAllAffiliates})", targetYear, includeAllAffiliates);

                // Fetch clinicians based on includeAllAffiliates flag
                // true = all active employee affiliates from AAUD database
                // false = only clinicians who have been scheduled (better data quality for scheduling purposes)
                var clinicians = await GetAllCliniciansAsync(includeAllAffiliates);

                // Filter clinicians based on user permissions and view context
                var filteredClinicians = FilterCliniciansByPermissions(clinicians, normalizedViewContext);

                // Convert to the expected response format
                var result = filteredClinicians.Select(c => new
                {
                    MothraId = c.MothraId,
                    FullName = c.FullName,
                    FirstName = c.FirstName,
                    LastName = c.LastName
                }).ToList();

                _logger.LogDebug("Retrieved {TotalClinicianCount} unique clinicians from PersonService (filtered to {FilteredCount} based on permissions for {ViewContext} view)",
                    clinicians.Count, result.Count, viewContext ?? "default");
                return Ok(result);
            }
            else
            {
                // Past year: use PersonService to get clinicians for specific year
                _logger.LogDebug("Using PersonService to get clinicians for past year {Year}", targetYear);

                var clinicians = await _personService.GetCliniciansByYearAsync(targetYear, HttpContext.RequestAborted);

                // Filter clinicians based on user permissions and view context
                var filteredClinicians = FilterCliniciansByPermissions(clinicians, normalizedViewContext);

                // Convert to the expected response format
                var result = filteredClinicians.Select(c => new
                {
                    MothraId = c.MothraId,
                    FullName = c.FullName,
                    FirstName = c.FirstName,
                    LastName = c.LastName
                }).ToList();

                _logger.LogDebug("Found {ClinicianCount} clinicians for year {Year} (filtered to {FilteredCount} based on permissions for {ViewContext} view)",
                    clinicians.Count, targetYear, result.Count, viewContext ?? "default");
                return Ok(result);
            }
        }


        /// <summary>
        /// Get the schedule for a specific clinician
        /// </summary>
        /// <param name="mothraId">The MothraId of the clinician</param>
        /// <param name="year">The grad year to filter by (optional)</param>
        /// <returns>Schedule information for the clinician grouped by semester</returns>
        [HttpGet("{mothraId}/schedule")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClinicianSchedule(string mothraId, [FromQuery] int? year = null)
        {
            var correlationId = HttpContext.TraceIdentifier ?? Guid.NewGuid().ToString();

            try
            {
                // Check if user has permission to view this clinician's schedule
                var hasPermission = CheckClinicianScheduleViewPermission(mothraId);
                if (!hasPermission)
                {
                    _logger.LogWarning("User does not have permission to view schedule for clinician {MothraId}", mothraId);
                    return Forbid("You do not have permission to view this clinician's schedule.");
                }
                // Use grad year logic instead of calendar year
                var targetYear = await GetTargetYearAsync(year);
                _logger.LogDebug("Fetching schedule for clinician {MothraId}, grad year: {Year}", mothraId, targetYear);

                // Get weeks for the grad year using vWeek view (contains correct week numbers)
                var vWeeks = await _weekService.GetWeeksAsync(targetYear, includeExtendedRotation: true);

                _logger.LogDebug("Retrieved {WeekCount} weeks for year {Year}, unique WeekIds: {UniqueCount}",
                    vWeeks.Count, targetYear, vWeeks.Select(w => w.WeekId).Distinct().Count());

                if (!vWeeks.Any())
                {
                    _logger.LogWarning("No weeks found for grad year {Year}", targetYear);

                    // Use PersonService to get clinician info (consistent with GetClinicians endpoint)
                    var clinicianResult = await BuildClinicianInfoAsync(mothraId);

                    return Ok(new
                    {
                        clinician = clinicianResult,
                        gradYear = targetYear,
                        schedulesBySemester = new List<object>()
                    });
                }

                // Get instructor schedules for this clinician using grad year filtering
                // Filter by weeks that belong to the target grad year (ensure unique week IDs)
                var weekIds = vWeeks.Select(w => w.WeekId).Distinct().ToList();

                // Load schedules filtered by both mothraId and the specific weeks for this grad year
                var schedules = await _context.InstructorSchedules
                    .AsNoTracking()
                    .Include(i => i.Week) // Week navigation works fine
                    .Include(i => i.Person) // Person navigation for name data
                    .Where(i => i.MothraId == mothraId && weekIds.Contains(i.WeekId))
                    .OrderBy(i => i.Week.DateStart)
                    .ToListAsync();

                // Get unique rotation IDs from schedules and load rotation data in batch
                var rotationIds = schedules.Select(s => s.RotationId).Distinct().ToList();
                var rotations = await LoadRotationsByIdsAsync(rotationIds);

                // Get clinician info - if no schedules, we still need to provide clinician details
                object clinicianInfo;

                if (!schedules.Any())
                {
                    _logger.LogDebug("No schedules found for clinician {MothraId} - returning empty schedule", mothraId);

                    // Use PersonService to get clinician info (same data source as GetClinicians endpoint)
                    clinicianInfo = await BuildClinicianInfoAsync(mothraId);
                }
                else
                {
                    // Get clinician info from first schedule record, but use PersonService as primary source
                    var firstSchedule = schedules.FirstOrDefault();
                    if (firstSchedule == null)
                    {
                        // Defensive fallback - this shouldn't happen given the Any() check above
                        _logger.LogWarning("Unexpected: No schedules found for clinician {MothraId} for year {Year} after confirming schedules exist. CorrelationId: {CorrelationId}", mothraId, targetYear, correlationId);
                        clinicianInfo = new
                        {
                            mothraId = mothraId,
                            fullName = $"Clinician {mothraId}",
                            firstName = "",
                            lastName = "",
                            role = (string?)null
                        };
                    }
                    else
                    {
                        // Try PersonService first for consistent name data
                        var clinicianFromPersonService = await GetClinicianByMothraIdAsync(mothraId);

                        if (clinicianFromPersonService != null)
                        {
                            clinicianInfo = BuildClinicianInfoFromPersonService(clinicianFromPersonService, mothraId, firstSchedule.Role);
                        }
                        else
                        {
                            // Fallback to schedule Person data
                            clinicianInfo = new
                            {
                                mothraId = mothraId,
                                fullName = firstSchedule.Person?.PersonDisplayFullName ?? "Unknown",
                                firstName = firstSchedule.Person?.PersonDisplayFirstName ?? "Unknown",
                                lastName = firstSchedule.Person?.PersonDisplayLastName ?? "Unknown",
                                role = firstSchedule.Role
                            };
                        }
                    }
                }

                // Always show ALL weeks for the grad year with assignments where they exist
                // Create a lookup for existing schedules by weekId (should be unique now after grad year filtering)
                _logger.LogDebug("Retrieved {ScheduleCount} schedules for clinician {MothraId}", schedules.Count, mothraId);

                // Debug: Check for duplicate WeekIds in schedules
                var weekIdCounts = schedules.GroupBy(s => s.WeekId).Where(g => g.Count() > 1).ToList();
                if (weekIdCounts.Any())
                {
                    _logger.LogWarning("Found duplicate WeekIds in schedules for clinician {MothraId}: {DuplicateWeekIds}",
                        mothraId, string.Join(", ", weekIdCounts.Select(g => $"WeekId {g.Key} appears {g.Count()} times")));

                    // Log detailed info about duplicates
                    foreach (var group in weekIdCounts)
                    {
                        var duplicateSchedules = group.ToList();
                        _logger.LogWarning("Duplicate WeekId {WeekId} schedules: {ScheduleDetails}",
                            group.Key,
                            string.Join(" | ", duplicateSchedules.Select(s => $"ScheduleId={s.InstructorScheduleId}, RotationId={s.RotationId}, WeekStart={s.Week?.DateStart:yyyy-MM-dd}")));
                    }
                }

                // Group schedules by WeekId to handle multiple rotations per week
                var schedulesByWeekId = schedules
                    .GroupBy(s => s.WeekId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                _logger.LogDebug("Grouped {ScheduleCount} schedules into {WeekCount} weeks for clinician {MothraId}",
                    schedules.Count, schedulesByWeekId.Count, mothraId);

                // Pre-calculate semester names for all weeks to avoid repeated calls
                var weeksWithSemester = vWeeks.Select(w =>
                {
                    var semesterName = TermCodeService.GetTermCodeDescription(w.TermCode);
                    var normalizedSemester = semesterName.StartsWith("Unknown Term")
                        ? "Unknown Semester"
                        : semesterName;

                    // Check if this week has schedule assignments (could be multiple)
                    var hasSchedules = schedulesByWeekId.TryGetValue(w.WeekId, out var weekSchedules);

                    // Build rotations array for this week
                    var weekRotations = hasSchedules && weekSchedules != null && weekSchedules.Any()
                        ? weekSchedules.Select(schedule =>
                        {
                            rotations.TryGetValue(schedule.RotationId, out var rotation);
                            // Always return a rotation object even if lookup fails
                            // Determine rotation name with fallback logic
                            var rotationName = GetRotationDisplayName(rotation, schedule.RotationId);

                            return new
                            {
                                rotationId = schedule.RotationId,
                                name = rotationName,
                                abbreviation = rotation?.Abbreviation ?? "",
                                serviceId = rotation?.ServiceId ?? 0,
                                serviceName = rotation?.Service?.ServiceName ?? "",
                                scheduleId = schedule.InstructorScheduleId,
                                isPrimaryEvaluator = schedule.Evaluator
                            };
                        })
                          .OrderBy(r => r!.name) // Sort rotations alphabetically
                          .Cast<dynamic>()
                          .ToArray()
                        : Array.Empty<dynamic>();

                    return new
                    {
                        Week = new
                        {
                            weekId = w.WeekId,
                            weekNumber = w.WeekNum, // Use proper academic week number from vWeek
                            dateStart = w.DateStart,
                            dateEnd = w.DateEnd,
                            termCode = w.TermCode,
                            rotations = weekRotations // Array to support multiple rotations
                        },
                        Semester = normalizedSemester
                    };
                }).ToList();

                // Group all weeks by pre-calculated semester
                var groupedSchedules = weeksWithSemester
                    .GroupBy(item => item.Semester)
                    .Select(g => new
                    {
                        semester = g.Key,
                        weeks = g.Select(item => item.Week).OrderBy(w => w.dateStart).ToList()
                    })
                    .OrderBy(g =>
                    {
                        return g.weeks.Any() ? g.weeks.Min(w => w.dateStart) : DateTime.MaxValue;
                    })
                    .Cast<object>()
                    .ToList();

                var result = new
                {
                    clinician = clinicianInfo,
                    gradYear = targetYear, // Returns the grad year from database settings
                    schedulesBySemester = groupedSchedules
                };

                _logger.LogDebug("Found {ScheduleCount} schedule entries for clinician {MothraId} (grad year {Year})",
                    schedules.Count, mothraId, targetYear);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Store context for ApiExceptionFilter to use in logging
                SetExceptionContext("MothraId", mothraId);
                throw; // Let ApiExceptionFilter handle the response
            }
        }

        /// <summary>
        /// Get all rotations that a clinician has been scheduled for
        /// </summary>
        /// <param name="mothraId">The MothraId of the clinician</param>
        /// <returns>List of unique rotations for the clinician</returns>
        [HttpGet("{mothraId}/rotations")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClinicianRotations(string mothraId)
        {
            try
            {
                _logger.LogDebug("Fetching rotations for clinician {MothraId}", mothraId);

                // Get unique rotation IDs for this clinician
                var rotationIds = await _context.InstructorSchedules
                    .Where(i => i.MothraId == mothraId)
                    .Select(i => i.RotationId)
                    .Distinct()
                    .ToListAsync();

                // Load rotation data in batch
                var rotationData = await LoadRotationsByIdsAsync(rotationIds);
                var rotations = rotationData.Values
                    .Select(r => new
                    {
                        RotationId = r.RotId,
                        RotationName = r.Name,
                        Abbreviation = r.Abbreviation,
                        ServiceId = r.ServiceId,
                        ServiceName = r.Service!.ServiceName
                    })
                    .OrderBy(r => r.ServiceName)
                    .ThenBy(r => r.RotationName)
                    .ToList();

                _logger.LogDebug("Found {RotationCount} unique rotations for clinician {MothraId}", rotations.Count, mothraId);
                return Ok(rotations);
            }
            catch (Exception ex)
            {
                // Store context for ApiExceptionFilter to use in logging
                SetExceptionContext("MothraId", mothraId);
                throw; // Let ApiExceptionFilter handle the response
            }
        }

        /// <summary>
        /// Helper method to get all clinicians using PersonService with grad year filtering
        /// </summary>
        /// <param name="includeAllAffiliates">Whether to include all affiliates or just active (both use current + previous grad year)</param>
        /// <returns>List of clinicians from PersonService</returns>
        private async Task<List<ClinicianSummary>> GetAllCliniciansAsync(bool includeAllAffiliates = false)
        {
            var currentGradYear = await GetCurrentGradYearAsync();

            if (includeAllAffiliates)
            {
                // When includeAllAffiliates is true, fetch active employee affiliates from AAUD database through PersonService
                _logger.LogDebug("Fetching all active employee affiliates from AAUD database");

                var allAffiliates = await _personService.GetAllActiveEmployeeAffiliatesAsync(HttpContext.RequestAborted);

                _logger.LogDebug("Found {Count} active employee affiliates from AAUD", allAffiliates.Count);
                return allAffiliates;
            }
            else
            {
                // When includeAllAffiliates is false, get only scheduled clinicians
                // but filter out those without proper names
                var gradYearsBack = ACTIVE_CLINICIANS_GRAD_YEARS_BACK;

                var scheduledClinicians = await _personService.GetCliniciansByGradYearRangeAsync(
                    currentGradYear - gradYearsBack,
                    currentGradYear,
                    cancellationToken: HttpContext.RequestAborted);

                // Filter out clinicians without proper names (those showing as "Clinician {MothraId}")
                var cliniciansWithNames = scheduledClinicians
                    .Where(c => !string.IsNullOrEmpty(c.FullName) &&
                               !c.FullName.StartsWith("Clinician ", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                _logger.LogDebug("Filtered {Original} scheduled clinicians to {Filtered} with proper names",
                    scheduledClinicians.Count, cliniciansWithNames.Count);

                return cliniciansWithNames;
            }
        }

        /// <summary>
        /// Helper method to find a specific clinician by MothraId from PersonService
        /// </summary>
        /// <param name="mothraId">The MothraId to search for</param>
        /// <returns>Clinician info or null if not found</returns>
        private async Task<ClinicianSummary?> GetClinicianByMothraIdAsync(string mothraId)
        {
            // First try to get from scheduled clinicians (better data quality)
            var person = await _personService.GetPersonAsync(mothraId, HttpContext.RequestAborted);
            if (person != null)
            {
                return new ClinicianSummary
                {
                    MothraId = person.MothraId,
                    FullName = person.FullName,
                    FirstName = person.FirstName,
                    LastName = person.LastName
                };
            }

            // Fallback to AAUD lookup through PersonService
            return await _personService.GetClinicianFromAaudAsync(mothraId, HttpContext.RequestAborted);
        }

        /// <summary>
        /// Helper method to load rotations by rotation IDs with service information
        /// </summary>
        /// <param name="rotationIds">List of rotation IDs to load</param>
        /// <returns>Dictionary mapping rotation ID to rotation entity</returns>
        private async Task<Dictionary<int, Viper.Models.ClinicalScheduler.Rotation>> LoadRotationsByIdsAsync(List<int> rotationIds)
        {
            return await _context.Rotations
                .AsNoTracking()
                .Include(r => r.Service)
                .Where(r => rotationIds.Contains(r.RotId))
                .ToDictionaryAsync(r => r.RotId, r => r, HttpContext.RequestAborted);
        }

        /// <summary>
        /// Helper method to build clinician info object from PersonService data
        /// </summary>
        /// <param name="clinician">Clinician data from PersonService</param>
        /// <param name="mothraId">MothraId for fallback</param>
        /// <param name="role">Optional role to include</param>
        /// <returns>Standardized clinician info object</returns>
        private object BuildClinicianInfoFromPersonService(ClinicianSummary clinician, string mothraId, string? role = null)
        {
            return new
            {
                mothraId = mothraId,
                fullName = clinician.FullName,
                firstName = clinician.FirstName,
                lastName = clinician.LastName,
                role = role
            };
        }

        /// <summary>
        /// Helper method to build clinician info object, trying PersonService first with fallback
        /// </summary>
        /// <param name="mothraId">The MothraId to lookup</param>
        /// <param name="role">Optional role to include</param>
        /// <returns>Clinician info object</returns>
        private async Task<object> BuildClinicianInfoAsync(string mothraId, string? role = null)
        {
            // Try PersonService which includes AAUD fallback
            var clinicianFromPersonService = await GetClinicianByMothraIdAsync(mothraId);
            if (clinicianFromPersonService != null)
            {
                return BuildClinicianInfoFromPersonService(clinicianFromPersonService, mothraId, role);
            }

            // If all lookups fail, return default
            return CreateDefaultClinicianInfo(mothraId, role);
        }


        /// <summary>
        /// Helper method to create a default clinician info object when no data is found
        /// </summary>
        /// <param name="mothraId">The MothraId for the default clinician</param>
        /// <param name="role">Optional role to include</param>
        /// <returns>Default clinician info object</returns>
        private static object CreateDefaultClinicianInfo(string mothraId, string? role = null)
        {
            return new
            {
                mothraId = mothraId,
                fullName = $"Clinician {mothraId}",
                firstName = "",
                lastName = "",
                role = role
            };
        }

        /// <summary>
        /// Filter clinicians based on user permissions.
        /// Users with EditOwnSchedule permission should only see themselves.
        /// </summary>
        /// <param name="clinicians">List of all available clinicians</param>
        /// <returns>Filtered list of clinicians based on user permissions</returns>
        private IEnumerable<ClinicianSummary> FilterCliniciansByPermissions(IEnumerable<ClinicianSummary> clinicians, string? viewContext = null)
        {
            try
            {
                var currentUser = _userHelper.GetCurrentUser();
                if (currentUser == null)
                {
                    _logger.LogWarning("No current user found when filtering clinicians");
                    return clinicians; // Return all if no user context
                }

                // Get RAPS context to check permissions
                var rapsContext = HttpContext.RequestServices.GetRequiredService<RAPSContext>();

                // Check permission levels
                var hasAdminPermission = _userHelper.HasPermission(rapsContext, currentUser, ClinicalSchedulePermissions.Admin);
                var hasManagePermission = _userHelper.HasPermission(rapsContext, currentUser, ClinicalSchedulePermissions.Manage);
                var hasEditClnSchedulesPermission = _userHelper.HasPermission(rapsContext, currentUser, ClinicalSchedulePermissions.EditClnSchedules);
                var hasEditOwnSchedulePermission = _userHelper.HasPermission(rapsContext, currentUser, ClinicalSchedulePermissions.EditOwnSchedule);

                // Users with EditOwnSchedule permission should only see themselves in clinician view
                // In rotation view, they should see all clinicians to add to rotations
                if (hasEditOwnSchedulePermission &&
                    !hasAdminPermission &&
                    !hasManagePermission &&
                    !hasEditClnSchedulesPermission &&
                    viewContext == "clinician")
                {
                    _logger.LogDebug("Filtering clinicians for own-schedule user {MothraId} in clinician view", currentUser.MothraId);

                    // Filter to only the current user in clinician view
                    var filteredClinicians = clinicians.Where(c => c.MothraId == currentUser.MothraId).ToList();

                    _logger.LogDebug("Filtered {Original} clinicians to {Filtered} for own-schedule user in clinician view",
                        clinicians.Count(), filteredClinicians.Count);

                    return filteredClinicians;
                }

                // All other cases (Admin, Manage, EditClnSchedules, rotation view, or service-specific permissions) see all clinicians
                return clinicians;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering clinicians by permissions. Returning unfiltered list.");
                return clinicians; // Return unfiltered list on error to avoid breaking functionality
            }
        }

        /// <summary>
        /// Normalize and validate the viewContext parameter
        /// </summary>
        /// <param name="viewContext">The raw viewContext value from the request</param>
        /// <returns>Normalized viewContext value or null for invalid/unknown values</returns>
        private static string? NormalizeViewContext(string? viewContext)
        {
            if (string.IsNullOrWhiteSpace(viewContext))
                return null;

            var normalized = viewContext.Trim().ToLowerInvariant();

            // Whitelist of valid view contexts
            return normalized switch
            {
                "clinician" => "clinician",
                "rotation" => "rotation",
                _ => null // Treat unknown values as default (null)
            };
        }

        /// <summary>
        /// Check if the current user has permission to view a specific clinician's schedule.
        /// Users with EditOwnSchedule permission can only view their own schedule.
        /// </summary>
        /// <param name="targetMothraId">The MothraId of the clinician whose schedule is being requested</param>
        /// <returns>True if user has permission, false otherwise</returns>
        private bool CheckClinicianScheduleViewPermission(string targetMothraId)
        {
            try
            {
                var currentUser = _userHelper.GetCurrentUser();
                if (currentUser == null)
                {
                    _logger.LogWarning("No current user found when checking clinician schedule view permission");
                    return false;
                }

                // Get RAPS context to check permissions
                var rapsContext = HttpContext.RequestServices.GetRequiredService<RAPSContext>();

                // Check permission levels
                var hasAdminPermission = _userHelper.HasPermission(rapsContext, currentUser, ClinicalSchedulePermissions.Admin);
                var hasManagePermission = _userHelper.HasPermission(rapsContext, currentUser, ClinicalSchedulePermissions.Manage);
                var hasEditClnSchedulesPermission = _userHelper.HasPermission(rapsContext, currentUser, ClinicalSchedulePermissions.EditClnSchedules);
                var hasEditOwnSchedulePermission = _userHelper.HasPermission(rapsContext, currentUser, ClinicalSchedulePermissions.EditOwnSchedule);

                // Users with higher permissions can view any clinician's schedule
                if (hasAdminPermission || hasManagePermission || hasEditClnSchedulesPermission)
                {
                    _logger.LogDebug("User {MothraId} has elevated permissions, allowing access to view schedule for {TargetMothraId}",
                        currentUser.MothraId, targetMothraId);
                    return true;
                }

                // Users with EditOwnSchedule permission can only view their own schedule
                if (hasEditOwnSchedulePermission)
                {
                    var canViewOwnSchedule = currentUser.MothraId.Equals(targetMothraId, StringComparison.OrdinalIgnoreCase);
                    _logger.LogDebug("User {MothraId} has EditOwnSchedule permission. Can view schedule for {TargetMothraId}: {CanView}",
                        currentUser.MothraId, targetMothraId, canViewOwnSchedule);
                    return canViewOwnSchedule;
                }

                // Check if user has service-specific permissions that would allow them to view this clinician
                // This would be handled by existing service-level permission logic (if implemented)
                // For now, deny access if no specific permissions are found
                _logger.LogDebug("User {MothraId} has no applicable permissions to view schedule for {TargetMothraId}",
                    currentUser.MothraId, targetMothraId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking clinician schedule view permission for target {TargetMothraId}", targetMothraId);
                return false; // Deny access on error
            }
        }

        /// <summary>
        /// Gets the display name for a rotation with fallback logic
        /// </summary>
        /// <param name="rotation">The rotation entity (may be null)</param>
        /// <param name="rotationId">The rotation ID for fallback display</param>
        /// <returns>Display name for the rotation</returns>
        private static string GetRotationDisplayName(Rotation? rotation, int rotationId)
        {
            if (rotation != null && !string.IsNullOrEmpty(rotation.Name))
            {
                return rotation.Name;
            }

            if (rotation != null && !string.IsNullOrEmpty(rotation.Abbreviation))
            {
                return rotation.Abbreviation;
            }

            return $"Rotation {rotationId}";
        }

    }
}
