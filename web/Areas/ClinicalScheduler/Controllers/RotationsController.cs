using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Areas.Curriculum.Services;
using Web.Authorization;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    // DTOs for type safety
    public class ServiceDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
    }

    public class RotationDto
    {
        public int RotId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string CourseNumber { get; set; } = string.Empty;
        public int ServiceId { get; set; }
        public ServiceDto? Service { get; set; }
    }

    [Route("api/clinicalscheduler/rotations")]
    [Permission(Allow = "SVMSecure.ClnSched")]
    public class RotationsController : BaseClinicalSchedulerController
    {
        private readonly ClinicalSchedulerContext _context;
        private readonly IWeekService _weekService;
        private readonly IRotationService _rotationService;

        public RotationsController(ClinicalSchedulerContext context,
            IGradYearService gradYearService, IWeekService weekService, IRotationService rotationService,
            ILogger<RotationsController> logger)
            : base(gradYearService, logger)
        {
            _context = context;
            _weekService = weekService;
            _rotationService = rotationService;
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

                // Get rotations through service layer for consistent deduplication
                List<Viper.Models.ClinicalScheduler.Rotation> rotations;

                if (serviceId.HasValue)
                {
                    rotations = await _rotationService.GetRotationsByServiceAsync(serviceId.Value, HttpContext.RequestAborted);
                }
                else
                {
                    rotations = await _rotationService.GetRotationsAsync(activeOnly: true, HttpContext.RequestAborted);
                }

                // Convert to DTOs for response
                var rotationDtos = rotations.Select(r => new RotationDto
                {
                    RotId = r.RotId,
                    Name = r.Name,
                    Abbreviation = r.Abbreviation,
                    SubjectCode = r.SubjectCode,
                    CourseNumber = r.CourseNumber,
                    ServiceId = r.ServiceId,
                    Service = includeService && r.Service != null ? new ServiceDto
                    {
                        ServiceId = r.Service.ServiceId,
                        ServiceName = r.Service.ServiceName,
                        ShortName = r.Service.ShortName
                    } : null
                }).ToList();

                _logger.LogInformation("Retrieved {Count} rotations via RotationService", rotationDtos.Count);
                return Ok(rotationDtos);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Failed to retrieve rotations", "ServiceId", serviceId);
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
                return BadRequest(new { error = "Rotation ID must be a positive integer", rotationId = id });
            }

            try
            {
                _logger.LogInformation("Getting rotation with ID: {RotationId}, IncludeService: {IncludeService}", id, includeService);

                // Get rotations through service layer for consistent deduplication
                var rotation = await _rotationService.GetRotationAsync(id, HttpContext.RequestAborted);

                if (rotation == null)
                {
                    _logger.LogWarning("Rotation not found with ID: {RotationId}", id);
                    return NotFound(new { error = "Rotation not found", rotationId = id });
                }

                // Build response object
                var response = BuildRotationResponse(rotation, includeService);

                _logger.LogInformation("Retrieved rotation via RotationService: {RotationName}", rotation.Name);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Failed to retrieve rotation", "RotationId", id);
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
        public async Task<ActionResult<object>> GetRotationSchedule(int id, int? year = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id <= 0)
            {
                _logger.LogWarning("Invalid rotation ID requested for schedule: {RotationId}", id);
                return BadRequest(new { error = "Rotation ID must be a positive integer", rotationId = id });
            }

            try
            {
                // Use grad year logic instead of calendar year
                var targetYear = await GetTargetYearAsync(year);
                _logger.LogInformation("Getting schedule for rotation {RotationId} for grad year {Year}", id, targetYear);

                // Get rotation details through service layer
                var rotation = await _rotationService.GetRotationAsync(id, HttpContext.RequestAborted);

                if (rotation == null)
                {
                    return NotFound(new { error = "Rotation not found", rotationId = id });
                }

                // Get weeks for the grad year using vWeek view (contains correct week numbers)
                var vWeeks = await _weekService.GetWeeksAsync(targetYear, includeExtendedRotation: true);

                if (!vWeeks.Any())
                {
                    _logger.LogWarning("No weeks found for grad year {Year}", targetYear);
                    return Ok(new
                    {
                        Rotation = BuildSimpleRotationResponse(rotation),
                        GradYear = targetYear,
                        SchedulesBySemester = new List<object>()
                    });
                }

                // Get instructor schedules for this rotation using grad year filtering
                // Filter by weeks that belong to the target grad year
                var weekIds = vWeeks.Select(w => w.WeekId).ToList();

                // DistinctBy InstructorScheduleId approach like ColdFusion SELECT DISTINCT
                var allInstructorSchedules = await _context.InstructorSchedules
                    .AsNoTracking()
                    .Include(i => i.Week)
                    .Where(i => i.RotationId == id && weekIds.Contains(i.WeekId))
                    .ToListAsync();

                var baseInstructorSchedules = allInstructorSchedules
                    .DistinctBy(i => i.InstructorScheduleId)
                    .OrderBy(i => i.Week.DateStart)
                    .ThenBy(i => i.MothraId)
                    .ToList();

                _logger.LogInformation("Retrieved {AllCount} total, {DistinctCount} after DistinctBy(InstructorScheduleId)", allInstructorSchedules.Count, baseInstructorSchedules.Count);

                // Get unique MothraIds and fetch person data
                var uniqueMothraIds = baseInstructorSchedules.Select(i => i.MothraId).Distinct().ToList();
                var personData = await _context.Persons
                    .AsNoTracking()
                    .Where(p => uniqueMothraIds.Contains(p.IdsMothraId))
                    .ToDictionaryAsync(p => p.IdsMothraId, p => p);

                // Deduplicate weeks and pre-calculate semester names for all weeks
                var deduplicatedWeeks = vWeeks
                    .DistinctBy(w => w.WeekId)
                    .ToList();

                var weeksWithSemester = deduplicatedWeeks.Select(w =>
                {
                    var semesterName = TermCodeService.GetTermCodeDescription(w.TermCode);
                    var normalizedSemester = semesterName.StartsWith("Unknown Term")
                        ? "Unknown Semester"
                        : semesterName;

                    // Find schedules for this week
                    var weekSchedules = baseInstructorSchedules
                        .Where(s => s.WeekId == w.WeekId)
                        .Select(i => new
                        {
                            instructorScheduleId = i.InstructorScheduleId,
                            firstName = personData.ContainsKey(i.MothraId) ? personData[i.MothraId].PersonDisplayFirstName : "Unknown",
                            lastName = personData.ContainsKey(i.MothraId) ? personData[i.MothraId].PersonDisplayLastName : "Unknown",
                            fullName = personData.ContainsKey(i.MothraId) ? personData[i.MothraId].PersonDisplayFullName : $"Person {i.MothraId}",
                            mothraId = i.MothraId,
                            evaluator = i.Evaluator,
                            isPrimaryEvaluator = i.Evaluator
                        })
                        .ToList();

                    return new
                    {
                        Week = new
                        {
                            weekId = w.WeekId,
                            weekNumber = w.WeekNum,
                            dateStart = w.DateStart,
                            dateEnd = w.DateEnd,
                            termCode = w.TermCode,
                            extendedRotation = w.ExtendedRotation,
                            forcedVacation = w.ForcedVacation,
                            requiresPrimaryEvaluator = EvaluationPolicyService.RequiresPrimaryEvaluator(w.WeekNum, deduplicatedWeeks),
                            instructorSchedules = weekSchedules
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

                _logger.LogInformation("Retrieved schedule for rotation {RotationName} (grad year {Year}) grouped into {SemesterCount} semesters",
                    rotation.Name, targetYear, groupedSchedules.Count);

                return Ok(new
                {
                    Rotation = BuildSimpleRotationResponse(rotation),
                    AcademicYear = targetYear,
                    SchedulesBySemester = groupedSchedules
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Failed to retrieve rotation schedule", "RotationId", id);
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

                _logger.LogInformation("Retrieved {Count} rotations with scheduled weeks for year {Year}", rotationsWithSchedules.Count, targetYear);
                return Ok(rotationsWithSchedules);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Failed to retrieve rotations with scheduled weeks", "Year", year);
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
            try
            {
                _logger.LogInformation("Getting rotation summary");

                var summary = await _context.Rotations
                    .AsNoTracking()
                    .Include(r => r.Service)
                    .GroupBy(r => new { r.ServiceId, r.Service.ServiceName, r.Service.ShortName })
                    .Select(g => new
                    {
                        ServiceId = g.Key.ServiceId,
                        ServiceName = g.Key.ServiceName,
                        ShortName = g.Key.ShortName,
                        RotationCount = g.Count(),
                        Rotations = g.Select(r => new { r.RotId, r.Name, r.Abbreviation }).ToList()
                    })
                    .OrderBy(s => s.ServiceName)
                    .ToListAsync();

                var totalRotations = await _context.Rotations.CountAsync();

                _logger.LogInformation("Retrieved summary for {ServiceCount} services with {TotalRotations} total rotations",
                    summary.Count, totalRotations);

                return Ok(new
                {
                    TotalRotations = totalRotations,
                    ServiceCount = summary.Count,
                    Services = summary
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Failed to retrieve rotation summary");
            }
        }




        /// <summary>
        /// Builds a rotation response object with optional service details
        /// </summary>
        /// <param name="rotation">The rotation entity</param>
        /// <param name="includeService">Whether to include service details</param>
        /// <returns>Anonymous object representing the rotation</returns>
        private object BuildRotationResponse(Viper.Models.ClinicalScheduler.Rotation rotation, bool includeService = true)
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

        /// <summary>
        /// Builds a simplified rotation response object for schedule contexts
        /// </summary>
        /// <param name="rotation">The rotation entity</param>
        /// <returns>Anonymous object representing the rotation</returns>
        private object BuildSimpleRotationResponse(Viper.Models.ClinicalScheduler.Rotation rotation)
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
                    rotation.Service.ShortName
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

            try
            {
                _logger.LogInformation("Getting available years, publishedOnly: {PublishedOnly}", publishedOnly);

                var currentGradYear = await GetCurrentGradYearAsync();
                var availableGradYears = await _gradYearService.GetAvailableGradYearsAsync(publishedOnly);

                return Ok(new
                {
                    CurrentGradYear = currentGradYear,
                    AvailableGradYears = availableGradYears
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Failed to retrieve available years");
            }
        }
    }
}