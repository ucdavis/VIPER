using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Areas.ClinicalScheduler.Models.DTOs.Responses;
using Viper.Areas.Curriculum.Services;
using Viper.Models.ClinicalScheduler;
using Web.Authorization;
using Person = Viper.Models.ClinicalScheduler.Person;

namespace Viper.Areas.ClinicalScheduler.Controllers
{

    [Route("api/clinicalscheduler/rotations")]
    [Permission(Allow = ClinicalSchedulePermissions.Base)]
    public class RotationsController : BaseClinicalSchedulerController
    {
        private readonly ClinicalSchedulerContext _context;
        private readonly IWeekService _weekService;
        private readonly IRotationService _rotationService;
        private readonly ISchedulePermissionService _permissionService;
        private readonly IEvaluationPolicyService _evaluationPolicyService;

        public RotationsController(ClinicalSchedulerContext context,
            IGradYearService gradYearService, IWeekService weekService, IRotationService rotationService,
            ISchedulePermissionService permissionService, IEvaluationPolicyService evaluationPolicyService,
            ILogger<RotationsController> logger)
            : base(gradYearService, logger)
        {
            _context = context;
            _weekService = weekService;
            _rotationService = rotationService;
            _permissionService = permissionService;
            _evaluationPolicyService = evaluationPolicyService;
        }




        /// <summary>
        /// Get all rotations with optional filtering
        /// </summary>
        /// <param name="serviceId">Optional service ID to filter by</param>
        /// <param name="includeService">Include service details (default: true)</param>
        /// <returns>List of rotations</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RotationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<RotationDto>>> GetRotations(int? serviceId = null, bool includeService = true)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Getting rotations. ServiceId: {ServiceId}, IncludeService: {IncludeService}", serviceId, includeService);

                // Get rotations through service layer
                List<RotationDto> rotations;

                if (serviceId.HasValue)
                {
                    rotations = await _rotationService.GetRotationsByServiceAsync(serviceId.Value, HttpContext.RequestAborted);
                }
                else
                {
                    rotations = await _rotationService.GetRotationsAsync(HttpContext.RequestAborted);
                }

                // Filter rotations based on user permissions
                var filteredRotations = new List<RotationDto>();
                foreach (var rotation in rotations)
                {
                    // Check if user can edit this rotation's service
                    if (await _permissionService.HasEditPermissionForServiceAsync(rotation.ServiceId, HttpContext.RequestAborted))
                    {
                        filteredRotations.Add(rotation);
                    }
                }

                // Rotations are already DTOs from the service
                var rotationDtos = filteredRotations;

                _logger.LogInformation("Retrieved {Count} rotations via RotationService", rotationDtos.Count);
                return Ok(rotationDtos);
            }
            catch (Exception)
            {
                // Store context for ApiExceptionFilter to use in logging
                SetExceptionContext(new Dictionary<string, object>
                {
                    ["ServiceId"] = serviceId,
                    ["Operation"] = "RetrieveRotations"
                });
                throw; // Let ApiExceptionFilter handle the response
            }
        }

        /// <summary>
        /// Get a specific rotation by ID
        /// </summary>
        /// <param name="id">Rotation ID</param>
        /// <param name="includeService">Include service details (default: true)</param>
        /// <returns>Single rotation</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> GetRotation(int id, bool includeService = true)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id <= 0)
            {
                _logger.LogWarning("Invalid rotation ID requested: {RotationId}", id);
                return BadRequest(new RotationErrorResponse("Rotation ID must be a positive integer", id));
            }

            try
            {
                _logger.LogInformation("Getting rotation with ID: {RotationId}, IncludeService: {IncludeService}", id, includeService);

                // Get rotations through service layer
                var rotation = await _rotationService.GetRotationAsync(id, HttpContext.RequestAborted);

                if (rotation == null)
                {
                    _logger.LogWarning("Rotation not found with ID: {RotationId}", id);
                    return NotFound(new RotationErrorResponse("Rotation not found", id));
                }

                // Build response object
                var response = BuildRotationResponse(rotation, includeService);

                _logger.LogInformation("Retrieved rotation via RotationService: {RotationName}", rotation.Name);
                return Ok(response);
            }
            catch (Exception)
            {
                // Store context for ApiExceptionFilter to use in logging
                SetExceptionContext("RotationId", id);
                throw; // Let ApiExceptionFilter handle the response
            }
        }

        /// <summary>
        /// Get instructor schedules for a specific rotation
        /// </summary>
        /// <param name="id">Rotation ID</param>
        /// <param name="year">Year to filter by (optional, defaults to current year)</param>
        /// <returns>Instructor schedules for the rotation</returns>
        [HttpGet("{id:int}/schedule")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> GetRotationSchedule(int id, [FromQuery] int? year = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id <= 0)
            {
                _logger.LogWarning("Invalid rotation ID requested for schedule: {RotationId}", id);
                return BadRequest(new RotationErrorResponse("Rotation ID must be a positive integer", id));
            }

            try
            {
                var targetYear = await GetTargetYearAsync(year);
                _logger.LogInformation("Getting schedule for rotation {RotationId} for grad year {Year}", id, targetYear);

                // Get rotation details through service layer
                var rotation = await _rotationService.GetRotationAsync(id, HttpContext.RequestAborted);

                if (rotation == null)
                {
                    return NotFound(new RotationErrorResponse("Rotation not found", id));
                }

                // Get weeks for the target year
                var vWeeks = await GetWeeksForRotationAsync(targetYear, includeExtendedRotation: true);

                if (!vWeeks.Any())
                {
                    _logger.LogWarning("No weeks found for grad year {Year}", targetYear);
                    return Ok(BuildEmptyScheduleResponse(rotation, targetYear));
                }

                // Get all necessary data
                var weekIds = vWeeks.Select(w => w.WeekId).ToList();
                var allInstructorSchedules = await GetInstructorSchedulesForWeeksAsync(id, weekIds);
                var recentCliniciansData = await GetRecentCliniciansAsync(id, targetYear, targetYear - 1);

                // Get rotation weekly preferences (closed status)
                var rotationWeeklyPrefs = await GetRotationWeeklyPrefsAsync(id, weekIds);

                // Process and deduplicate data
                var baseInstructorSchedules = allInstructorSchedules
                    .DistinctBy(i => i.InstructorScheduleId)
                    .OrderBy(i => i.Week.DateStart)
                    .ThenBy(i => i.MothraId)
                    .ToList();

                _logger.LogInformation("Retrieved {AllCount} total, {DistinctCount} after DistinctBy(InstructorScheduleId)",
                    allInstructorSchedules.Count, baseInstructorSchedules.Count);

                // Get person data for all involved clinicians
                var uniqueMothraIds = baseInstructorSchedules.Select(i => i.MothraId).Distinct();
                var recentClinicianMothraIds = recentCliniciansData.Select(c => c.MothraId).Distinct();
                var allMothraIds = uniqueMothraIds.Concat(recentClinicianMothraIds);
                var personData = await GetPersonDataBatchAsync(allMothraIds);

                // Build week schedules with semester information
                var deduplicatedWeeks = vWeeks.DistinctBy(w => w.WeekId).ToList();
                var weeksWithSemester = deduplicatedWeeks.Select(w => new
                {
                    Week = BuildWeekScheduleItem(w, baseInstructorSchedules, personData, deduplicatedWeeks, rotationWeeklyPrefs, rotation),
                    Semester = NormalizeSemesterName(w.TermCode)
                });

                // Group schedules by semester and build recent clinicians list
                var groupedSchedules = GroupSchedulesBySemester(weeksWithSemester);
                var recentCliniciansList = BuildRecentCliniciansList(recentClinicianMothraIds, personData);

                _logger.LogInformation("Retrieved schedule for rotation {RotationName} (grad year {Year}) grouped into {SemesterCount} semesters, {RecentClinicianCount} recent clinicians",
                    rotation.Name, targetYear, groupedSchedules.Count, recentCliniciansList.Count);

                return Ok(BuildRotationScheduleResponse(rotation, targetYear, groupedSchedules, recentCliniciansList));
            }
            catch (Exception)
            {
                // Store context for ApiExceptionFilter to use in logging
                SetExceptionContext("RotationId", id);
                throw; // Let ApiExceptionFilter handle the response
            }
        }

        /// <summary>
        /// Get rotations that have scheduled weeks for a specific year
        /// </summary>
        /// <param name="year">Year to filter by (optional, defaults to current year)</param>
        /// <param name="includeService">Include service details (default: true)</param>
        /// <returns>List of rotations with scheduled weeks</returns>
        [HttpGet("with-scheduled-weeks")]
        [ProducesResponseType(typeof(IEnumerable<RotationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<RotationDto>>> GetRotationsWithScheduledWeeks([FromQuery] int? year = null, [FromQuery] bool includeService = true)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var targetYear = await GetTargetYearAsync(year);
                _logger.LogInformation("Getting rotations with scheduled weeks for year {Year}, IncludeService: {IncludeService}", targetYear, includeService);

                // Get rotations that have instructor schedules for the specified year
                // Use join-based approach for better performance instead of nested Any() calls
                var rotationsWithSchedulesQuery = from r in _context.Rotations.AsNoTracking()
                                                  join i in _context.InstructorSchedules on r.RotId equals i.RotationId
                                                  join w in _context.Weeks on i.WeekId equals w.WeekId
                                                  where w.DateStart.Year == targetYear
                                                  select r.RotId;

                var rotationIdsWithSchedules = await rotationsWithSchedulesQuery.Distinct().ToListAsync();

                var query = _context.Rotations.AsNoTracking()
                    .Where(r => rotationIdsWithSchedules.Contains(r.RotId));

                if (includeService)
                {
                    query = query.Include(r => r.Service);
                }

                List<RotationDto> rotationsWithSchedules;

                if (includeService)
                {
                    rotationsWithSchedules = await query
                        .OrderBy(r => r.Service.ServiceName ?? r.Name)
                        .ThenBy(r => r.Name)
                        .Select(r => new RotationDto
                        {
                            RotId = r.RotId,
                            Name = r.Name,
                            Abbreviation = r.Abbreviation,
                            SubjectCode = r.SubjectCode,
                            CourseNumber = r.CourseNumber,
                            ServiceId = r.ServiceId,
                            Service = new ServiceDto
                            {
                                ServiceId = r.Service.ServiceId,
                                ServiceName = r.Service.ServiceName,
                                ShortName = r.Service.ShortName
                            }
                        })
                        .ToListAsync();
                }
                else
                {
                    rotationsWithSchedules = await query
                        .OrderBy(r => r.Name)
                        .Select(r => new RotationDto
                        {
                            RotId = r.RotId,
                            Name = r.Name,
                            Abbreviation = r.Abbreviation,
                            SubjectCode = r.SubjectCode,
                            CourseNumber = r.CourseNumber,
                            ServiceId = r.ServiceId,
                            Service = null
                        })
                        .ToListAsync();
                }

                // Filter results based on user permissions
                var filteredRotations = new List<RotationDto>();
                foreach (var rotation in rotationsWithSchedules)
                {
                    if (await _permissionService.HasEditPermissionForServiceAsync(rotation.ServiceId, HttpContext.RequestAborted))
                    {
                        filteredRotations.Add(rotation);
                    }
                }

                _logger.LogInformation("Retrieved {Count} rotations with scheduled weeks for year {Year} (filtered to {FilteredCount})", rotationsWithSchedules.Count, targetYear, filteredRotations.Count);
                return Ok(filteredRotations);
            }
            catch (Exception ex)
            {
                // Store context for ApiExceptionFilter to use in logging
                SetExceptionContext("Year", year);
                throw; // Let ApiExceptionFilter handle the response
            }
        }

        /// <summary>
        /// Get rotation counts by service
        /// </summary>
        /// <returns>Service rotation summary</returns>
        [HttpGet("summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> GetRotationSummary()
        {
            _logger.LogInformation("Getting rotation summary");

            var summary = await _context.Rotations
                .AsNoTracking()
                .Include(r => r.Service)
                .GroupBy(r => new { r.ServiceId, r.Service.ServiceName, r.Service.ShortName })
                .Select(g => new ServiceSummaryDto
                {
                    ServiceId = g.Key.ServiceId,
                    ServiceName = g.Key.ServiceName,
                    ShortName = g.Key.ShortName,
                    RotationCount = g.Count(),
                    Rotations = g.Select(r => new RotationSummaryDto
                    {
                        RotId = r.RotId,
                        Name = r.Name,
                        Abbreviation = r.Abbreviation
                    }).ToList()
                })
                .OrderBy(s => s.ServiceName)
                .ToListAsync();

            var totalRotations = await _context.Rotations.CountAsync();

            _logger.LogInformation("Retrieved summary for {ServiceCount} services with {TotalRotations} total rotations",
                summary.Count, totalRotations);

            return Ok(new RotationSummaryResponse
            {
                TotalRotations = totalRotations,
                ServiceCount = summary.Count,
                Services = summary
            });
        }




        /// <summary>
        /// Builds a rotation response object with optional service details
        /// </summary>
        /// <param name="rotation">The rotation entity</param>
        /// <param name="includeService">Whether to include service details</param>
        /// <returns>Anonymous object representing the rotation</returns>
        private object BuildRotationResponse(RotationDto rotation, bool includeService = true)
        {
            return new
            {
                rotation.RotId,
                rotation.Name,
                rotation.Abbreviation,
                rotation.SubjectCode,
                rotation.CourseNumber,
                rotation.ServiceId,
                Service = includeService && rotation.Service != null ? new
                {
                    rotation.Service.ServiceId,
                    rotation.Service.ServiceName,
                    rotation.Service.ShortName
                } : null
            };
        }

        #region Helper Methods for GetRotationSchedule

        /// <summary>
        /// Gets weeks for a rotation filtered by year
        /// </summary>
        private async Task<List<WeekDto>> GetWeeksForRotationAsync(int year, bool includeExtendedRotation = true)
        {
            return await _weekService.GetWeeksAsync(year, includeExtendedRotation);
        }

        /// <summary>
        /// Gets instructor schedules for specific weeks and rotation
        /// </summary>
        private async Task<List<InstructorSchedule>> GetInstructorSchedulesForWeeksAsync(int rotationId, IEnumerable<int> weekIds)
        {
            return await _context.InstructorSchedules
                .AsNoTracking()
                .Include(i => i.Week)
                .Where(i => i.RotationId == rotationId && weekIds.Contains(i.WeekId))
                .ToListAsync();
        }

        /// <summary>
        /// Gets recent clinicians data for a rotation from current and previous year
        /// </summary>
        private async Task<List<RecentClinicianData>> GetRecentCliniciansAsync(int rotationId, int currentYear, int previousYear)
        {
            var currentYearWeeks = await _weekService.GetWeeksAsync(currentYear, includeExtendedRotation: true);
            var previousYearWeeks = await _weekService.GetWeeksAsync(previousYear, includeExtendedRotation: true);
            var allYearWeekIds = currentYearWeeks.Select(w => w.WeekId)
                .Concat(previousYearWeeks.Select(w => w.WeekId))
                .ToList();

            var recentClinicians = await _context.InstructorSchedules
                .AsNoTracking()
                .Include(i => i.Week)
                .Where(i => i.RotationId == rotationId && allYearWeekIds.Contains(i.WeekId))
                .Select(i => new { i.MothraId, i.Week.DateStart })
                .Distinct()
                .ToListAsync();

            return recentClinicians.Select(c => new RecentClinicianData
            {
                MothraId = c.MothraId,
                DateStart = c.DateStart
            }).ToList();
        }

        /// <summary>
        /// Gets person data in batch for multiple MothraIds
        /// </summary>
        private async Task<Dictionary<string, Person>> GetPersonDataBatchAsync(IEnumerable<string> mothraIds)
        {
            var distinctMothraIds = mothraIds.Distinct().ToList();
            return await _context.Persons
                .AsNoTracking()
                .Where(p => distinctMothraIds.Contains(p.IdsMothraId))
                .ToDictionaryAsync(p => p.IdsMothraId, p => p);
        }

        /// <summary>
        /// Fetches rotation weekly preferences for specified weeks
        /// </summary>
        /// <param name="rotationId">The rotation ID</param>
        /// <param name="weekIds">List of week IDs to fetch preferences for</param>
        /// <returns>Dictionary mapping WeekId to Closed status (true if rotation is closed for that week)</returns>
        private async Task<Dictionary<int, bool>> GetRotationWeeklyPrefsAsync(int rotationId, List<int> weekIds)
        {
            var prefs = await _context.RotationWeeklyPrefs
                .AsNoTracking()
                .Where(p => p.RotId == rotationId && weekIds.Contains(p.WeekId))
                .ToDictionaryAsync(p => p.WeekId, p => p.Closed);
            return prefs;
        }

        /// <summary>
        /// Data structure for recent clinician information
        /// </summary>
        private sealed class RecentClinicianData
        {
            public string MothraId { get; set; } = string.Empty;
            public DateTime DateStart { get; set; }
        }


        /// <summary>
        /// Normalizes semester name from term code
        /// </summary>
        private static string NormalizeSemesterName(int termCode)
        {
            var semesterName = TermCodeService.GetTermCodeDescription(termCode);
            return semesterName.StartsWith("Unknown Term") ? "Unknown Semester" : semesterName;
        }

        /// <summary>
        /// Builds a week schedule item with instructor information
        /// </summary>
        private object BuildWeekScheduleItem(WeekDto week, IEnumerable<InstructorSchedule> allSchedules,
            Dictionary<string, Person> personData, List<WeekDto> allWeeks,
            Dictionary<int, bool> rotationWeeklyPrefs, RotationDto? rotation = null) // WeekId -> Closed status mapping
        {
            var weekSchedules = allSchedules
                .Where(s => s.WeekId == week.WeekId)
                .Select(i => new
                {
                    instructorScheduleId = i.InstructorScheduleId,
                    firstName = personData.ContainsKey(i.MothraId) ? personData[i.MothraId].PersonDisplayFirstName : "Unknown",
                    lastName = personData.ContainsKey(i.MothraId) ? personData[i.MothraId].PersonDisplayLastName : "Unknown",
                    fullName = personData.ContainsKey(i.MothraId)
                        ? personData[i.MothraId].PersonDisplayFullName
                        : $"Person {i.MothraId}",
                    mothraId = i.MothraId,
                    isPrimaryEvaluator = i.Evaluator
                })
                .ToList();

            // Pass all weeks to evaluation service for complete rotation block analysis
            // Don't filter by scheduled clinicians - the evaluation logic needs to see
            // adjacent weeks to properly determine primary evaluator requirements
            var rotationWeeks = allWeeks.OrderBy(w => w.WeekNum).ToList();

            // Check if this specific week is closed for this rotation
            var rotationClosed = rotationWeeklyPrefs.TryGetValue(week.WeekId, out var closed) && closed;

            // Use the simplified evaluation logic with actual closed status
            var requiresPrimary = _evaluationPolicyService.RequiresPrimaryEvaluator(
                week.WeekNum,
                rotationWeeks,
                rotation?.Service?.WeekSize,
                rotationClosed);

            return new
            {
                weekId = week.WeekId,
                weekNumber = week.WeekNum,
                dateStart = week.DateStart,
                dateEnd = week.DateEnd,
                termCode = week.TermCode,
                extendedRotation = week.ExtendedRotation,
                rotationClosed = rotationClosed,
                requiresPrimaryEvaluator = requiresPrimary,
                instructorSchedules = weekSchedules
            };
        }

        /// <summary>
        /// Groups week schedules by semester
        /// </summary>
        private List<object> GroupSchedulesBySemester(IEnumerable<dynamic> weekSchedules)
        {
            return weekSchedules
                .GroupBy(item => (string)item.Semester)
                .Select(g => new
                {
                    semester = g.Key,
                    weeks = g.Select(item => item.Week).OrderBy(w => w.dateStart).ToList()
                })
                .OrderBy(g => g.weeks.Any() ? g.weeks[0].dateStart : DateTime.MaxValue)
                .Cast<object>()
                .ToList();
        }

        /// <summary>
        /// Builds recent clinicians list with full names
        /// </summary>
        private List<object> BuildRecentCliniciansList(IEnumerable<string> mothraIds, Dictionary<string, Person> personData)
        {
            return mothraIds
                .Distinct()
                .Select(mothraId => new
                {
                    mothraId = mothraId,
                    fullName = personData.ContainsKey(mothraId)
                        ? personData[mothraId].PersonDisplayFullName
                        : $"Clinician {mothraId}"
                })
                .OrderBy(c => c.fullName)
                .Cast<object>()
                .ToList();
        }

        /// <summary>
        /// Builds the complete rotation schedule response
        /// </summary>
        private object BuildRotationScheduleResponse(RotationDto rotation, int gradYear,
            List<object> schedulesBySemester, List<object> recentClinicians)
        {
            return new
            {
                rotation = BuildSimpleRotationResponse(rotation),
                gradYear = gradYear,
                schedulesBySemester = schedulesBySemester,
                recentClinicians = recentClinicians
            };
        }

        /// <summary>
        /// Builds an empty rotation schedule response when no weeks are found
        /// </summary>
        private object BuildEmptyScheduleResponse(RotationDto rotation, int academicYear)
        {
            return new
            {
                Rotation = BuildSimpleRotationResponse(rotation),
                AcademicYear = academicYear,
                SchedulesBySemester = new List<object>()
            };
        }

        #endregion

        /// <summary>
        /// Builds a simplified rotation response object for schedule contexts
        /// </summary>
        /// <param name="rotation">The rotation entity</param>
        /// <returns>Anonymous object representing the rotation</returns>
        private object BuildSimpleRotationResponse(RotationDto rotation)
        {
            return new
            {
                rotation.RotId,
                rotation.Name,
                rotation.Abbreviation,
                Service = rotation.Service != null ? new
                {
                    rotation.Service.ServiceId,
                    rotation.Service.ServiceName,
                    rotation.Service.ShortName,
                    rotation.Service.WeekSize
                } : null
            };
        }

        /// <summary>
        /// Get available graduation years for the clinical scheduler
        /// </summary>
        /// <param name="publishedOnly">If true, only return years where PublishSchedule is true</param>
        /// <returns>Available graduation years with current year highlighted</returns>

        [HttpGet("years")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> GetAvailableYears([FromQuery] bool publishedOnly = false)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            _logger.LogInformation("Getting available years, publishedOnly: {PublishedOnly}", publishedOnly);

            var currentGradYear = await GetCurrentGradYearAsync();
            var availableGradYears = await _gradYearService.GetAvailableGradYearsAsync(publishedOnly);

            return Ok(new
            {
                CurrentGradYear = currentGradYear,
                AvailableGradYears = availableGradYears
            });
        }

        /// <summary>
        /// Get initial page data for Clinical Scheduler including years and permissions
        /// This eliminates multiple API calls on page load
        /// </summary>
        /// <param name="publishedOnly">If true, only return years where PublishSchedule is true</param>
        /// <returns>Initial data needed by Clinical Scheduler pages</returns>
        [HttpGet("page-data")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> GetPageData([FromQuery] bool publishedOnly = false)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            _logger.LogInformation("Getting initial page data, publishedOnly: {PublishedOnly}", publishedOnly);

            var currentGradYear = await GetCurrentGradYearAsync();
            var availableGradYears = await _gradYearService.GetAvailableGradYearsAsync(publishedOnly);

            return Ok(new
            {
                currentGradYear = currentGradYear,
                availableGradYears = availableGradYears,
            });
        }

    }
}
