using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Viper.Classes.SQLContext;
using Viper.Areas.ClinicalScheduler.Services;
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
        private readonly WeekService _weekService;

        public RotationsController(ClinicalSchedulerContext context,
            AcademicYearService academicYearService, WeekService weekService,
            IMemoryCache cache, ILogger<RotationsController> logger)
            : base(academicYearService, cache, logger)
        {
            _context = context;
            _weekService = weekService;
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
            try
            {
                _logger.LogInformation("Getting rotations. ServiceId: {ServiceId}, IncludeService: {IncludeService}", serviceId, includeService);

                var query = _context.Rotations.AsQueryable();

                if (serviceId.HasValue)
                {
                    query = query.Where(r => r.ServiceId == serviceId.Value);
                }

                if (includeService)
                {
                    query = query.Include(r => r.Service);
                }

                // Get rotations with optional service data using DTOs for type safety
                List<RotationDto> rotations;

                if (includeService)
                {
                    rotations = await query
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
                    rotations = await query
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

                _logger.LogInformation("Retrieved {Count} rotations", rotations.Count);
                return Ok(rotations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rotations. ServiceId: {ServiceId}", serviceId);
                return StatusCode(500, new
                {
                    error = "Failed to retrieve rotations"
                });
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
            if (id <= 0)
            {
                _logger.LogWarning("Invalid rotation ID requested: {RotationId}", id);
                return BadRequest(new { error = "Rotation ID must be a positive integer", rotationId = id });
            }

            try
            {
                _logger.LogInformation("Getting rotation with ID: {RotationId}, IncludeService: {IncludeService}", id, includeService);

                var query = _context.Rotations.AsQueryable();

                if (includeService)
                {
                    query = query.Include(r => r.Service);
                }

                var rotation = await query
                    .Where(r => r.RotId == id)
                    .Select(r => new
                    {
                        r.RotId,
                        r.Name,
                        r.Abbreviation,
                        r.SubjectCode,
                        r.CourseNumber,
                        r.ServiceId,
                        Service = includeService ? new
                        {
                            r.Service.ServiceId,
                            r.Service.ServiceName,
                            r.Service.ShortName
                        } : null
                    })
                    .FirstOrDefaultAsync();

                if (rotation == null)
                {
                    _logger.LogWarning("Rotation not found with ID: {RotationId}", id);
                    return NotFound(new { error = "Rotation not found", rotationId = id });
                }

                _logger.LogInformation("Retrieved rotation: {RotationName}", rotation.Name);
                return Ok(rotation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rotation with ID: {RotationId}", id);
                return StatusCode(500, new
                {
                    error = "Failed to retrieve rotation",
                    rotationId = id
                });
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
            if (id <= 0)
            {
                _logger.LogWarning("Invalid rotation ID requested for schedule: {RotationId}", id);
                return BadRequest(new { error = "Rotation ID must be a positive integer", rotationId = id });
            }

            try
            {
                // Use academic year logic instead of calendar year
                var targetYear = await GetTargetYearAsync(year);
                _logger.LogInformation("Getting schedule for rotation {RotationId} for academic year {Year}", id, targetYear);

                // Get rotation info first
                var rotation = await _context.Rotations
                    .Include(r => r.Service)
                    .FirstOrDefaultAsync(r => r.RotId == id);

                if (rotation == null)
                {
                    return NotFound(new { error = "Rotation not found", rotationId = id });
                }

                // Get weeks for the academic year using vWeek view (contains correct week numbers)
                var vWeeks = await _weekService.GetWeeksAsync(targetYear, includeExtendedRotation: true);

                if (!vWeeks.Any())
                {
                    _logger.LogWarning("No weeks found for academic year {Year}", targetYear);
                    return Ok(new
                    {
                        Rotation = new
                        {
                            rotation.RotId,
                            rotation.Name,
                            rotation.Abbreviation,
                            Service = new
                            {
                                rotation.Service.ServiceId,
                                rotation.Service.ServiceName,
                                rotation.Service.ShortName
                            }
                        },
                        AcademicYear = targetYear,
                        Weeks = new List<object>(),
                        InstructorSchedules = new List<object>()
                    });
                }

                // Get instructor schedules for this rotation using academic year filtering
                // Filter by weeks that belong to the target academic year
                var weekIds = vWeeks.Select(w => w.WeekId).ToList();

                // First get the base instructor schedule data from Entity Framework
                var baseInstructorSchedules = await _context.InstructorSchedules
                    .Where(i => i.RotationId == id && weekIds.Contains(i.WeekId))
                    .OrderBy(i => i.DateStart)
                    .ThenBy(i => i.LastName)
                    .ThenBy(i => i.FirstName)
                    .ToListAsync();

                // Create a dictionary for fast vWeek lookups
                var vWeekDict = vWeeks.ToDictionary(w => w.WeekId, w => w);

                // Now combine the data in memory
                var instructorSchedules = baseInstructorSchedules
                    .Select(i => new
                    {
                        i.InstructorScheduleId,
                        i.FirstName,
                        i.LastName,
                        i.FullName,
                        i.MothraId,
                        i.Evaluator,
                        i.DateStart,
                        i.DateEnd,
                        i.WeekId,
                        // Get week number from vWeek data
                        WeekNumber = vWeekDict.ContainsKey(i.WeekId) ? vWeekDict[i.WeekId].WeekNum : 0,
                        Week = new
                        {
                            WeekId = i.WeekId,
                            DateStart = i.DateStart,
                            DateEnd = i.DateEnd,
                            TermCode = vWeekDict.ContainsKey(i.WeekId) ? vWeekDict[i.WeekId].TermCode : 0
                        }
                    })
                    .ToList();

                // Build weeks data using the vWeek view data (contains correct week numbers)
                var weeks = vWeeks
                    .Where(w => weekIds.Contains(w.WeekId))
                    .Select(w => new
                    {
                        w.WeekId,
                        w.DateStart,
                        w.DateEnd,
                        w.TermCode,
                        WeekNumber = w.WeekNum, // Use the pre-calculated academic week number from database view
                        w.ExtendedRotation,
                        w.ForcedVacation,
                        RequiresPrimaryEvaluator = EvaluationPolicyService.RequiresPrimaryEvaluator(w.WeekNum)
                    })
                    .OrderBy(w => w.WeekNumber)
                    .ToList();

                _logger.LogInformation("Retrieved {InstructorCount} instructor assignments across {WeekCount} weeks for rotation {RotationName} (academic year {Year})",
                    instructorSchedules.Count, weeks.Count, rotation.Name, targetYear);

                return Ok(new
                {
                    Rotation = new
                    {
                        rotation.RotId,
                        rotation.Name,
                        rotation.Abbreviation,
                        Service = new
                        {
                            rotation.Service.ServiceId,
                            rotation.Service.ServiceName,
                            rotation.Service.ShortName
                        }
                    },
                    AcademicYear = targetYear, // Returns the academic year from database settings
                    Weeks = weeks,
                    InstructorSchedules = instructorSchedules
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schedule for rotation {RotationId}", id);
                return StatusCode(500, new
                {
                    error = "Failed to retrieve rotation schedule",
                    rotationId = id
                });
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
            try
            {
                var targetYear = year ?? DateTime.Now.Year;
                _logger.LogInformation("Getting rotations with scheduled weeks for year {Year}, IncludeService: {IncludeService}", targetYear, includeService);

                // Get rotations that have instructor schedules for the specified year
                var query = _context.Rotations.AsQueryable();

                if (includeService)
                {
                    query = query.Include(r => r.Service);
                }

                // Filter to only rotations that have scheduled weeks in the target year
                List<RotationDto> rotationsWithSchedules;

                if (includeService)
                {
                    rotationsWithSchedules = await query
                        .Where(r => _context.InstructorSchedules.Any(i =>
                            i.RotationId == r.RotId &&
                            i.DateStart.Year == targetYear))
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
                        .Where(r => _context.InstructorSchedules.Any(i =>
                            i.RotationId == r.RotId &&
                            i.DateStart.Year == targetYear))
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
                _logger.LogError(ex, "Error retrieving rotations with scheduled weeks for year {Year}", year);
                return StatusCode(500, new
                {
                    error = "Failed to retrieve rotations with scheduled weeks",
                    year = year
                });
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
                _logger.LogError(ex, "Error retrieving rotation summary");
                return StatusCode(500, new
                {
                    error = "Failed to retrieve rotation summary"
                });
            }
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
            try
            {
                _logger.LogInformation("Getting available years, publishedOnly: {PublishedOnly}", publishedOnly);

                var currentGradYear = await GetCurrentGradYearAsync();
                var availableGradYears = await _academicYearService.GetAvailableGradYearsAsync(publishedOnly);

                return Ok(new
                {
                    CurrentGradYear = currentGradYear,
                    AvailableGradYears = availableGradYears
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available years");
                return StatusCode(500, new
                {
                    error = "Failed to retrieve available years"
                });
            }
        }
    }
}