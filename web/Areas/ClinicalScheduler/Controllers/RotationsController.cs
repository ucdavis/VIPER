using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
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
    public class RotationsController : ApiController
    {
        private readonly ClinicalSchedulerContext _context;
        private readonly ILogger<RotationsController> _logger;

        public RotationsController(ClinicalSchedulerContext context, ILogger<RotationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all rotations with optional filtering
        /// </summary>
        /// <param name="serviceId">Optional service ID to filter by</param>
        /// <param name="includeService">Include service details (default: true)</param>
        /// <returns>List of rotations</returns>
        [HttpGet]
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
                        .OrderBy(r => r.Service.ServiceName)
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
                    error = "Failed to retrieve rotations",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Get a specific rotation by ID
        /// </summary>
        /// <param name="id">Rotation ID</param>
        /// <param name="includeService">Include service details (default: true)</param>
        /// <returns>Single rotation</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetRotation(int id, bool includeService = true)
        {
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
                    message = ex.Message,
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
        [HttpGet("{id}/schedule")]
        public async Task<ActionResult<object>> GetRotationSchedule(int id, int? year = null)
        {
            try
            {
                var targetYear = year ?? DateTime.Now.Year;
                _logger.LogInformation("Getting schedule for rotation {RotationId} for year {Year}", id, targetYear);

                // Get rotation info first
                var rotation = await _context.Rotations
                    .Include(r => r.Service)
                    .FirstOrDefaultAsync(r => r.RotId == id);

                if (rotation == null)
                {
                    return NotFound(new { error = "Rotation not found", rotationId = id });
                }

                // Get instructor schedules for this rotation
                var instructorSchedules = await _context.InstructorSchedules
                    .Include(i => i.Week)
                    .Where(i => i.RotationId == id && i.DateStart.Year == targetYear)
                    .OrderBy(i => i.Week.DateStart)
                    .ThenBy(i => i.LastName)
                    .ThenBy(i => i.FirstName)
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
                        Week = new
                        {
                            i.Week.WeekId,
                            i.Week.DateStart,
                            i.Week.DateEnd,
                            i.Week.TermCode
                        }
                    })
                    .ToListAsync();

                // Get weeks for this rotation with efficient week number calculation
                var weekIds = instructorSchedules.Select(i => i.Week.WeekId).Distinct().ToList();
                
                // NOTE: Two different reviewers suggested contradictory approaches:
                // 1. First reviewer: Avoid N+1 queries by calculating in memory
                // 2. Second reviewer: Avoid loading all year weeks, use database calculation
                // 
                // This implementation uses the in-memory approach because:
                // - Academic years typically have ~52 weeks (small dataset)
                // - Eliminates N+1 query problem completely
                // - Simple and maintainable
                // 
                // For very large datasets, consider using raw SQL with ROW_NUMBER() window function
                
                // Get all weeks for the target year to calculate week numbers efficiently
                var yearWeeks = await _context.Weeks
                    .Where(w => w.DateStart.Year == targetYear)
                    .OrderBy(w => w.DateStart)
                    .Select(w => new { w.WeekId, w.DateStart })
                    .ToListAsync();

                // Calculate week numbers in memory (avoids N+1 queries)
                var weekNumberMap = yearWeeks
                    .Select((w, index) => new { w.WeekId, WeekNumber = index + 1 })
                    .ToDictionary(w => w.WeekId, w => w.WeekNumber);

                // Get the specific weeks we need
                var weeksData = await _context.Weeks
                    .Where(w => weekIds.Contains(w.WeekId))
                    .OrderBy(w => w.DateStart)
                    .Select(w => new
                    {
                        w.WeekId,
                        w.DateStart,
                        w.DateEnd,
                        w.TermCode
                    })
                    .ToListAsync();

                // Combine with week numbers and business rules
                var weeks = weeksData.Select(w => new
                {
                    w.WeekId,
                    w.DateStart,
                    w.DateEnd,
                    w.TermCode,
                    WeekNumber = weekNumberMap.ContainsKey(w.WeekId) ? weekNumberMap[w.WeekId] : 0,
                    // Business rule: Even-numbered weeks require primary evaluators
                    RequiresPrimaryEvaluator = weekNumberMap.ContainsKey(w.WeekId) && weekNumberMap[w.WeekId] % 2 == 0
                }).ToList();

                _logger.LogInformation("Retrieved {InstructorCount} instructor assignments across {WeekCount} weeks for rotation {RotationName}",
                    instructorSchedules.Count, weeks.Count, rotation.Name);

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
                    Year = targetYear,
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
                    message = ex.Message,
                    rotationId = id
                });
            }
        }

        /// <summary>
        /// Get rotation counts by service
        /// </summary>
        /// <returns>Service rotation summary</returns>
        [HttpGet("summary")]
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
                    error = "Failed to retrieve rotation summary",
                    message = ex.Message
                });
            }
        }
    }
}