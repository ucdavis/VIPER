using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Areas.Curriculum.Services;
using Web.Authorization;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("ClinicalScheduler")]
    [Permission(Allow = ClinicalSchedulePermissions.Manage)]
    public class CliniciansController : BaseClinicalSchedulerController
    {
        #region Constants

        /// <summary>
        /// Number of days to look back for active clinicians (2 years)
        /// </summary>
        private const int ACTIVE_CLINICIANS_LOOKBACK_DAYS = 730;

        /// <summary>
        /// Number of days to look back for all affiliates including historical (3 years)
        /// </summary>
        private const int ALL_AFFILIATES_LOOKBACK_DAYS = 1095;

        #endregion

        private readonly ClinicalSchedulerContext _context;
        private readonly AAUDContext _aaudContext; // Legacy context for fallback clinician lookup
        private readonly IWeekService _weekService;
        private readonly IPersonService _personService;

        public CliniciansController(ClinicalSchedulerContext context, AAUDContext aaudContext, ILogger<CliniciansController> logger,
            IGradYearService gradYearService, IWeekService weekService, IPersonService personService)
            : base(gradYearService, logger)
        {
            _context = context;
            _aaudContext = aaudContext;
            _weekService = weekService;
            _personService = personService;
        }

        /// <summary>
        /// Get a list of all unique clinicians, combining active clinicians with historically scheduled ones
        /// </summary>
        /// <param name="year">Year to filter clinicians by (optional, defaults to current year behavior)</param>
        /// <param name="includeAllAffiliates">If true, includes all affiliates instead of just active clinicians</param>
        /// <returns>List of clinicians with their basic info</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClinicians([FromQuery] int? year = null, [FromQuery] bool includeAllAffiliates = false)
        {
            try
            {
                var currentGradYear = await GetCurrentGradYearAsync();
                var targetYear = year ?? currentGradYear;

                _logger.LogInformation("GetClinicians endpoint called with year: {Year}, includeAllAffiliates: {IncludeAllAffiliates}", targetYear, includeAllAffiliates);

                if (targetYear >= currentGradYear)
                {
                    // Current or future year: use PersonService to get clinicians from Clinical Scheduler data
                    _logger.LogInformation("Using PersonService to get clinicians for current/future year {Year} (includeAllAffiliates: {IncludeAllAffiliates})", targetYear, includeAllAffiliates);

                    // Note: includeAllAffiliates parameter is kept for API compatibility
                    // but PersonService only has access to clinicians who have been scheduled
                    // This is actually better data quality since it's clinicians actually involved in scheduling
                    var clinicians = await GetAllCliniciansAsync(includeAllAffiliates);

                    // Convert to the expected response format
                    var result = clinicians.Select(c => new
                    {
                        MothraId = c.MothraId,
                        FullName = c.FullName,
                        FirstName = c.FirstName,
                        LastName = c.LastName
                    }).ToList();

                    _logger.LogInformation("Retrieved {TotalClinicianCount} unique clinicians from PersonService", result.Count);
                    return Ok(result);
                }
                else
                {
                    // Past year: use PersonService to get clinicians for specific year
                    _logger.LogInformation("Using PersonService to get clinicians for past year {Year}", targetYear);

                    var clinicians = await _personService.GetCliniciansByYearAsync(targetYear, HttpContext.RequestAborted);

                    // Convert to the expected response format
                    var result = clinicians.Select(c => new
                    {
                        MothraId = c.MothraId,
                        FullName = c.FullName,
                        FirstName = c.FirstName,
                        LastName = c.LastName
                    }).ToList();

                    _logger.LogInformation("Found {ClinicianCount} clinicians for year {Year}", result.Count, targetYear);
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex, "An error occurred while fetching clinicians");
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
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClinicianSchedule(string mothraId, [FromQuery] int? year = null)
        {
            var correlationId = HttpContext.TraceIdentifier ?? Guid.NewGuid().ToString();

            try
            {
                // Use grad year logic instead of calendar year
                var targetYear = await GetTargetYearAsync(year);
                _logger.LogInformation("Fetching schedule for clinician {MothraId}, grad year: {Year}", mothraId, targetYear);

                // Get weeks for the grad year using vWeek view (contains correct week numbers)
                var vWeeks = await _weekService.GetWeeksAsync(targetYear, includeExtendedRotation: true);

                _logger.LogInformation("Retrieved {WeekCount} weeks for year {Year}, unique WeekIds: {UniqueCount}",
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
                    _logger.LogInformation("No schedules found for clinician {MothraId} - returning empty schedule", mothraId);

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
                _logger.LogInformation("Retrieved {ScheduleCount} schedules for clinician {MothraId}", schedules.Count, mothraId);

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

                Dictionary<int, Models.ClinicalScheduler.InstructorSchedule> schedulesByWeekId;
                try
                {
                    schedulesByWeekId = schedules.ToDictionary(s => s.WeekId, s => s);
                }
                catch (ArgumentException ex) when (ex.Message.Contains("An item with the same key has already been added"))
                {
                    _logger.LogError(ex, "Duplicate key error when creating schedules dictionary for clinician {MothraId}. " +
                        "Total schedules: {ScheduleCount}, Unique WeekIds in vWeeks: {UniqueWeekCount}, " +
                        "WeekIds from schedules: {ScheduleWeekIds}",
                        mothraId, schedules.Count, vWeeks.Select(w => w.WeekId).Distinct().Count(),
                        string.Join(", ", schedules.Select(s => s.WeekId).OrderBy(id => id)));

                    // Create dictionary using GroupBy to handle duplicates - take the first schedule for each WeekId
                    schedulesByWeekId = schedules
                        .GroupBy(s => s.WeekId)
                        .ToDictionary(g => g.Key, g => g.First());

                    _logger.LogInformation("Created schedules dictionary with GroupBy fallback. Dictionary size: {DictionarySize}",
                        schedulesByWeekId.Count);
                }

                // Deduplicate weeks and pre-calculate semester names for all weeks to avoid repeated calls
                var deduplicatedWeeks = vWeeks
                    .DistinctBy(w => w.WeekId)
                    .ToList();

                var weeksWithSemester = deduplicatedWeeks.Select(w =>
                {
                    var semesterName = TermCodeService.GetTermCodeDescription(w.TermCode);
                    var normalizedSemester = semesterName.StartsWith("Unknown Term")
                        ? "Unknown Semester"
                        : semesterName;

                    // Check if this week has a schedule assignment
                    var hasSchedule = schedulesByWeekId.TryGetValue(w.WeekId, out var schedule);

                    return new
                    {
                        Week = new
                        {
                            weekId = w.WeekId,
                            weekNumber = w.WeekNum, // Use proper academic week number from vWeek
                            dateStart = w.DateStart,
                            dateEnd = w.DateEnd,
                            termCode = w.TermCode,
                            rotation = hasSchedule && rotations.TryGetValue(schedule.RotationId, out var rotation) ? new
                            {
                                rotationId = schedule.RotationId,
                                rotationName = rotation.Name, // Use loaded rotation data
                                abbreviation = rotation.Abbreviation, // Use loaded rotation data
                                serviceId = rotation.ServiceId, // Use loaded rotation data
                                serviceName = rotation.Service?.ServiceName // Use loaded rotation data
                            } : null, // No rotation assigned or rotation not found
                            isPrimaryEvaluator = hasSchedule && schedule.Evaluator
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
                    .OrderBy(g => g.weeks.Any() ? g.weeks.Min(w => w.dateStart) : DateTime.MaxValue)
                    .Cast<object>()
                    .ToList();

                var result = new
                {
                    clinician = clinicianInfo,
                    gradYear = targetYear, // Returns the grad year from database settings
                    schedulesBySemester = groupedSchedules
                };

                _logger.LogInformation("Found {ScheduleCount} schedule entries for clinician {MothraId} (grad year {Year})",
                    schedules.Count, mothraId, targetYear);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "An error occurred while fetching the clinician schedule", "MothraId", mothraId);
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
                _logger.LogInformation("Fetching rotations for clinician {MothraId}", mothraId);

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

                _logger.LogInformation("Found {RotationCount} unique rotations for clinician {MothraId}", rotations.Count, mothraId);
                return Ok(rotations);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "An error occurred while fetching clinician rotations", "MothraId", mothraId);
            }
        }

        /// <summary>
        /// Helper method to get all clinicians using PersonService with standard parameters
        /// </summary>
        /// <param name="includeAllAffiliates">Whether to include all affiliates (3 year lookback) or just active (2 year lookback)</param>
        /// <returns>List of clinicians from PersonService</returns>
        private async Task<List<ClinicianSummary>> GetAllCliniciansAsync(bool includeAllAffiliates = false)
        {
            return await _personService.GetCliniciansAsync(
                includeHistorical: true,
                sinceDays: includeAllAffiliates ? ALL_AFFILIATES_LOOKBACK_DAYS : ACTIVE_CLINICIANS_LOOKBACK_DAYS,
                cancellationToken: HttpContext.RequestAborted);
        }

        /// <summary>
        /// Helper method to find a specific clinician by MothraId from PersonService
        /// </summary>
        /// <param name="mothraId">The MothraId to search for</param>
        /// <returns>Clinician info or null if not found</returns>
        private async Task<ClinicianSummary?> GetClinicianByMothraIdAsync(string mothraId)
        {
            var clinicians = await GetAllCliniciansAsync(includeAllAffiliates: true);
            return clinicians.FirstOrDefault(c => c.MothraId == mothraId);
        }

        /// <summary>
        /// Helper method to load rotations by rotation IDs with service information
        /// </summary>
        /// <param name="rotationIds">List of rotation IDs to load</param>
        /// <returns>Dictionary mapping rotation ID to rotation entity</returns>
        private async Task<Dictionary<int, Models.ClinicalScheduler.Rotation>> LoadRotationsByIdsAsync(List<int> rotationIds)
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
        /// Helper method to build clinician info object, trying PersonService first, then AAUD fallback
        /// </summary>
        /// <param name="mothraId">The MothraId to lookup</param>
        /// <param name="role">Optional role to include</param>
        /// <returns>Clinician info object</returns>
        private async Task<object> BuildClinicianInfoAsync(string mothraId, string? role = null)
        {
            // Try PersonService first
            var clinicianFromPersonService = await GetClinicianByMothraIdAsync(mothraId);
            if (clinicianFromPersonService != null)
            {
                return BuildClinicianInfoFromPersonService(clinicianFromPersonService, mothraId, role);
            }

            // Fallback to AAUD context
            var clinicianFromAaud = await GetClinicianFromAaudAsync(mothraId, role);
            return clinicianFromAaud ?? CreateDefaultClinicianInfo(mothraId, role);
        }

        /// <summary>
        /// Helper method to get clinician info from AAUD context as fallback
        /// </summary>
        /// <param name="mothraId">The MothraId to look up</param>
        /// <param name="role">Optional role to include in the result</param>
        /// <returns>Clinician info object or null if not found</returns>
        private async Task<object?> GetClinicianFromAaudAsync(string mothraId, string? role = null)
        {
            var clinicianFromAaud = await _aaudContext.VwVmthClinicians
                .Where(c => c.IdsMothraid == mothraId)
                .Select(c => new
                {
                    mothraId = mothraId,
                    fullName = ResolveClinicianName(c.FullName, c.PersonDisplayFirstName, c.PersonDisplayLastName, mothraId),
                    firstName = c.PersonDisplayFirstName ?? "",
                    lastName = c.PersonDisplayLastName ?? "",
                    role = role
                })
                .FirstOrDefaultAsync();

            return clinicianFromAaud;
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
        /// Helper method to resolve clinician full name with fallbacks
        /// </summary>
        /// <param name="fullName">Primary full name from data source</param>
        /// <param name="firstName">First name to use as fallback</param>
        /// <param name="lastName">Last name to use as fallback</param>
        /// <param name="mothraId">MothraId to use as final fallback</param>
        /// <returns>Resolved full name</returns>
        private static string ResolveClinicianName(string? fullName, string? firstName, string? lastName, string mothraId)
        {
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return fullName;
            }

            if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
            {
                return $"{firstName ?? ""} {lastName ?? ""}".Trim();
            }

            return $"Clinician {mothraId}";
        }
    }
}