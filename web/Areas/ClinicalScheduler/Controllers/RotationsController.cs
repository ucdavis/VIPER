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
        private readonly RotationService _rotationService;

        public RotationsController(ClinicalSchedulerContext context,
            AcademicYearService academicYearService, WeekService weekService, RotationService rotationService,
            IMemoryCache cache, ILogger<RotationsController> logger)
            : base(academicYearService, cache, logger)
        {
            _context = context;
            _weekService = weekService;
            _rotationService = rotationService;
        }

        /// <summary>
        /// Check for duplicate instructors in a specific rotation
        /// </summary>
        [HttpGet("{id:int}/check-duplicates")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> CheckDuplicates(int id, int? weekId = null)
        {
            try
            {
                // Get academic year and weeks data for proper week numbers
                var targetYear = await _academicYearService.GetCurrentSelectionYearAsync();
                var vWeeks = await _weekService.GetWeeksAsync(targetYear, includeExtendedRotation: true);

                // Create a dictionary for fast vWeek lookups
                var vWeekDict = vWeeks.ToDictionary(w => w.WeekId, w => w);

                var query = _context.InstructorSchedules
                    .Include(i => i.Week)
                    .Where(i => i.RotationId == id);

                if (weekId.HasValue)
                {
                    query = query.Where(i => i.WeekId == weekId.Value);
                }

                var rawData = await query
                    .OrderBy(i => i.WeekId)
                    .ThenBy(i => i.MothraId)
                    .Select(i => new
                    {
                        i.InstructorScheduleId,
                        i.MothraId,
                        i.WeekId,
                        i.RotationId,
                        i.Evaluator,
                        WeekNumber = vWeekDict.ContainsKey(i.WeekId) ? vWeekDict[i.WeekId].WeekNum : 0 // Get week number from vWeek data
                    })
                    .ToListAsync();

                // Group by MothraId and WeekId to find duplicates
                var duplicates = rawData
                    .GroupBy(r => new { r.MothraId, r.WeekId })
                    .Where(g => g.Count() > 1)
                    .Select(g => new
                    {
                        g.Key.MothraId,
                        g.Key.WeekId,
                        Count = g.Count(),
                        Records = g.ToList()
                    })
                    .ToList();

                return Ok(new
                {
                    RotationId = id,
                    WeekId = weekId,
                    TotalRecords = rawData.Count,
                    UniquePersonWeekCombinations = rawData.GroupBy(r => new { r.MothraId, r.WeekId }).Count(),
                    DuplicatesFound = duplicates.Count,
                    Duplicates = duplicates,
                    FirstFewRecords = rawData.Take(10).ToList()
                });
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error in CheckDuplicates. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, new { error = ex.Message, correlationId });
            }
        }

        /// <summary>
        /// Database diagnostic endpoint to test ClinicalScheduler database table queries
        /// </summary>
        /// <returns>Database query test results</returns>
        [HttpGet("test-tables")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> TestClinicalSchedulerTables()
        {
            try
            {
                _logger.LogInformation("Testing ClinicalScheduler database table queries");

                var connectionString = _context.Database.GetConnectionString();
                var results = new Dictionary<string, object>();

                try
                {
                    using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                    await connection.OpenAsync();

                    // Test 1: Check what database we're connected to
                    using var dbCommand = connection.CreateCommand();
                    dbCommand.CommandText = "SELECT DB_NAME() as DatabaseName";
                    var databaseName = await dbCommand.ExecuteScalarAsync();
                    results["connectedDatabase"] = databaseName;

                    // Test 2: Get Rotation table column structure
                    try
                    {
                        using var rotColsCommand = connection.CreateCommand();
                        rotColsCommand.CommandText = @"
                            SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
                            FROM INFORMATION_SCHEMA.COLUMNS 
                            WHERE TABLE_NAME = 'Rotation' AND TABLE_SCHEMA = 'dbo'
                            ORDER BY ORDINAL_POSITION";
                        using var reader = await rotColsCommand.ExecuteReaderAsync();
                        var columns = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            columns.Add(new
                            {
                                columnName = reader.GetString(0),
                                dataType = reader.GetString(1),
                                isNullable = reader.GetString(2)
                            });
                        }
                        results["rotationColumns"] = new { success = true, columns = columns };
                    }
                    catch (Exception ex)
                    {
                        results["rotationColumns"] = new { success = false, error = ex.Message };
                    }

                    // Test 3: Try to query Service table
                    try
                    {
                        using var serviceCommand = connection.CreateCommand();
                        serviceCommand.CommandText = "SELECT TOP 5 * FROM Service";
                        using var reader = await serviceCommand.ExecuteReaderAsync();
                        var services = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            var service = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                service[reader.GetName(i)] = reader.GetValue(i);
                            }
                            services.Add(service);
                        }
                        results["serviceTable"] = new { success = true, count = services.Count, data = services };
                    }
                    catch (Exception ex)
                    {
                        results["serviceTable"] = new { success = false, error = ex.Message };
                    }

                    // Test 4: Try to query InstructorSchedule table
                    try
                    {
                        using var instrCommand = connection.CreateCommand();
                        instrCommand.CommandText = "SELECT TOP 5 * FROM InstructorSchedule";
                        using var reader = await instrCommand.ExecuteReaderAsync();
                        var schedules = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            var schedule = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                schedule[reader.GetName(i)] = reader.GetValue(i);
                            }
                            schedules.Add(schedule);
                        }
                        results["instructorScheduleTable"] = new { success = true, count = schedules.Count, data = schedules };
                    }
                    catch (Exception ex)
                    {
                        results["instructorScheduleTable"] = new { success = false, error = ex.Message };
                    }

                    // Test 5: Try to query vWeek view
                    try
                    {
                        using var weekCommand = connection.CreateCommand();
                        weekCommand.CommandText = "SELECT TOP 5 * FROM vWeek";
                        using var reader = await weekCommand.ExecuteReaderAsync();
                        var weeks = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            var week = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                week[reader.GetName(i)] = reader.GetValue(i);
                            }
                            weeks.Add(week);
                        }
                        results["vWeekView"] = new { success = true, count = weeks.Count, data = weeks };
                    }
                    catch (Exception ex)
                    {
                        results["vWeekView"] = new { success = false, error = ex.Message };
                    }

                    // Test 6: List all available tables and views
                    try
                    {
                        using var schemaCommand = connection.CreateCommand();
                        schemaCommand.CommandText = @"
                            SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE 
                            FROM INFORMATION_SCHEMA.TABLES 
                            WHERE TABLE_NAME LIKE '%rotation%' OR TABLE_NAME LIKE '%service%' OR 
                                  TABLE_NAME LIKE '%week%' OR TABLE_NAME LIKE '%instructor%' OR 
                                  TABLE_NAME LIKE '%schedule%'
                            ORDER BY TABLE_TYPE, TABLE_NAME";
                        using var reader = await schemaCommand.ExecuteReaderAsync();
                        var schema = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            schema.Add(new
                            {
                                schema = reader.GetString(0),
                                name = reader.GetString(1),
                                type = reader.GetString(2)
                            });
                        }
                        results["availableSchema"] = schema;
                    }
                    catch (Exception ex)
                    {
                        results["availableSchema"] = new { error = ex.Message };
                    }
                }
                catch (Exception ex)
                {
                    var correlationId = Guid.NewGuid().ToString();
                    _logger.LogError(ex, "Database connection failed. CorrelationId: {CorrelationId}", correlationId);
                    return StatusCode(500, new
                    {
                        error = "Database connection failed",
                        message = "A database connection error occurred. Please contact the administrator.",
                        correlationId
                    });
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "ClinicalScheduler table test failed. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, new
                {
                    error = "Table test failed",
                    message = ex.Message,
                    correlationId
                });
            }
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

                // Get rotations through service layer for consistent deduplication
                List<Viper.Models.CTS.Rotation> rotations;

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
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error retrieving rotations. ServiceId: {ServiceId}. CorrelationId: {CorrelationId}", serviceId, correlationId);
                return StatusCode(500, new
                {
                    error = "Failed to retrieve rotations",
                    correlationId
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

                // Get rotations through service layer for consistent deduplication
                var rotation = await _rotationService.GetRotationAsync(id, HttpContext.RequestAborted);

                if (rotation == null)
                {
                    _logger.LogWarning("Rotation not found with ID: {RotationId}", id);
                    return NotFound(new { error = "Rotation not found", rotationId = id });
                }

                // Build response object
                var response = new
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

                _logger.LogInformation("Retrieved rotation via RotationService: {RotationName}", rotation.Name);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error retrieving rotation with ID: {RotationId}. CorrelationId: {CorrelationId}", id, correlationId);
                return StatusCode(500, new
                {
                    error = "Failed to retrieve rotation",
                    rotationId = id,
                    correlationId
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

                // Get rotation details through service layer
                var rotation = await _rotationService.GetRotationAsync(id, HttpContext.RequestAborted);

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

                // DistinctBy InstructorScheduleId approach like ColdFusion SELECT DISTINCT
                var allInstructorSchedules = await _context.InstructorSchedules
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
                    .Where(p => uniqueMothraIds.Contains(p.IdsMothraId))
                    .ToDictionaryAsync(p => p.IdsMothraId, p => p);

                // Create a dictionary for fast vWeek lookups (handle potential duplicates)
                var vWeekDict = new Dictionary<int, dynamic>();
                foreach (var week in vWeeks)
                {
                    if (!vWeekDict.ContainsKey(week.WeekId))
                    {
                        vWeekDict[week.WeekId] = week;
                    }
                }

                // Now combine the data in memory with person data from the dictionary lookup
                var instructorSchedules = baseInstructorSchedules
                    .Select(i => new
                    {
                        i.InstructorScheduleId,
                        FirstName = personData.ContainsKey(i.MothraId) ? personData[i.MothraId].PersonDisplayFirstName : "Unknown",
                        LastName = personData.ContainsKey(i.MothraId) ? personData[i.MothraId].PersonDisplayLastName : "Unknown",
                        FullName = personData.ContainsKey(i.MothraId) ? personData[i.MothraId].PersonDisplayFullName : $"Person {i.MothraId}",
                        i.MothraId,
                        i.Evaluator,
                        DateStart = i.Week.DateStart,   // Get dates from Week navigation property
                        DateEnd = i.Week.DateEnd,       // Get dates from Week navigation property
                        i.WeekId,
                        // Get week number from vWeek data
                        WeekNumber = vWeekDict.ContainsKey(i.WeekId) ? vWeekDict[i.WeekId].WeekNum : 0,
                        Week = new
                        {
                            WeekId = i.WeekId,
                            DateStart = i.Week.DateStart,  // Get dates from Week navigation property
                            DateEnd = i.Week.DateEnd,      // Get dates from Week navigation property
                            TermCode = vWeekDict.ContainsKey(i.WeekId) ? vWeekDict[i.WeekId].TermCode : 0
                        }
                    })
                    .ToList();

                // Build weeks data using only the week IDs that are actually referenced in deduplicated instructor schedules
                var actualWeekIds = baseInstructorSchedules.Select(i => i.WeekId).Distinct().ToList();
                var weeks = vWeeks
                    .Where(w => actualWeekIds.Contains(w.WeekId))
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
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error retrieving schedule for rotation {RotationId}. CorrelationId: {CorrelationId}", id, correlationId);
                return StatusCode(500, new
                {
                    error = "Failed to retrieve rotation schedule",
                    rotationId = id,
                    correlationId
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
                            _context.Weeks.Any(w => w.WeekId == i.WeekId && w.DateStart.Year == targetYear)))
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
                            _context.Weeks.Any(w => w.WeekId == i.WeekId && w.DateStart.Year == targetYear)))
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
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error retrieving rotations with scheduled weeks for year {Year}. CorrelationId: {CorrelationId}", year, correlationId);
                return StatusCode(500, new
                {
                    error = "Failed to retrieve rotations with scheduled weeks",
                    year = year,
                    correlationId
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
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error retrieving rotation summary. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, new
                {
                    error = "Failed to retrieve rotation summary",
                    correlationId
                });
            }
        }

        /// <summary>
        /// Database diagnostic endpoint to check column names in actual tables
        /// </summary>
        /// <returns>Column information for Rotation and Service tables</returns>
        [HttpGet("test-columns")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> TestDatabaseColumns()
        {
            try
            {
                _logger.LogInformation("Testing ClinicalScheduler database column structures");

                var connectionString = _context.Database.GetConnectionString();
                var results = new Dictionary<string, object>();

                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync();

                // Get column information for Rotation table
                using var rotationColsCommand = connection.CreateCommand();
                rotationColsCommand.CommandText = @"
                    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'Rotation' AND TABLE_SCHEMA = 'dbo'
                    ORDER BY ORDINAL_POSITION";
                using var reader1 = await rotationColsCommand.ExecuteReaderAsync();
                var rotationColumns = new List<object>();
                while (await reader1.ReadAsync())
                {
                    rotationColumns.Add(new
                    {
                        columnName = reader1.GetString(0),
                        dataType = reader1.GetString(1),
                        isNullable = reader1.GetString(2),
                        columnDefault = reader1.IsDBNull(3) ? null : reader1.GetString(3)
                    });
                }
                results["rotationColumns"] = rotationColumns;

                await reader1.CloseAsync();

                // Get column information for Service table
                using var serviceColsCommand = connection.CreateCommand();
                serviceColsCommand.CommandText = @"
                    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'Service' AND TABLE_SCHEMA = 'dbo'
                    ORDER BY ORDINAL_POSITION";
                using var reader2 = await serviceColsCommand.ExecuteReaderAsync();
                var serviceColumns = new List<object>();
                while (await reader2.ReadAsync())
                {
                    serviceColumns.Add(new
                    {
                        columnName = reader2.GetString(0),
                        dataType = reader2.GetString(1),
                        isNullable = reader2.GetString(2),
                        columnDefault = reader2.IsDBNull(3) ? null : reader2.GetString(3)
                    });
                }
                results["serviceColumns"] = serviceColumns;

                await reader2.CloseAsync();

                // Test sample data from Rotation table
                try
                {
                    using var rotDataCommand = connection.CreateCommand();
                    rotDataCommand.CommandText = "SELECT TOP 2 * FROM Rotation";
                    using var reader3 = await rotDataCommand.ExecuteReaderAsync();
                    var rotationData = new List<object>();
                    while (await reader3.ReadAsync())
                    {
                        var rotation = new Dictionary<string, object>();
                        for (int i = 0; i < reader3.FieldCount; i++)
                        {
                            rotation[reader3.GetName(i)] = reader3.GetValue(i);
                        }
                        rotationData.Add(rotation);
                    }
                    results["rotationSampleData"] = rotationData;
                }
                catch (Exception ex)
                {
                    results["rotationSampleData"] = new { error = ex.Message };
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "ClinicalScheduler column test failed. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, new
                {
                    error = "Column test failed",
                    message = ex.Message,
                    correlationId
                });
            }
        }

        /// <summary>
        /// Database diagnostic endpoint to check available tables and views
        /// </summary>
        /// <returns>Database schema information</returns>
        [HttpGet("test")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> TestDatabaseConnection()
        {
            try
            {
                _logger.LogInformation("Testing ClinicalSchedulerContext database connection and available schema");

                // Test basic database connection (bypass EF)
                var connectionString = _context.Database.GetConnectionString();
                var availableTables = new List<string>();
                var availableViews = new List<string>();

                try
                {
                    // Use raw SQL connection to bypass Entity Framework entirely
                    using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                    await connection.OpenAsync();

                    using var command = connection.CreateCommand();

                    // Get all tables and views with clinical scheduler related names from both dbo and cts schemas
                    command.CommandText = @"
                        SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE (TABLE_SCHEMA = 'dbo' OR TABLE_SCHEMA = 'cts') AND (
                            TABLE_NAME LIKE '%rotation%' OR 
                            TABLE_NAME LIKE '%service%' OR 
                            TABLE_NAME LIKE '%week%' OR 
                            TABLE_NAME LIKE '%instructor%' OR 
                            TABLE_NAME LIKE '%student%' OR
                            TABLE_NAME LIKE '%schedule%' OR
                            TABLE_NAME LIKE 'vw%'
                        )
                        ORDER BY TABLE_SCHEMA, TABLE_TYPE, TABLE_NAME";

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var tableSchema = reader.GetString(0);
                        var tableName = reader.GetString(1);
                        var tableType = reader.GetString(2);
                        var fullName = $"{tableSchema}.{tableName}";

                        if (tableType == "BASE TABLE")
                            availableTables.Add(fullName);
                        else if (tableType == "VIEW")
                            availableViews.Add(fullName);
                    }
                }
                catch (Exception ex)
                {
                    var correlationId = Guid.NewGuid().ToString();
                    _logger.LogError(ex, "Failed to query schema information. CorrelationId: {CorrelationId}", correlationId);
                    return StatusCode(500, new
                    {
                        error = "Schema query failed",
                        message = "A schema query error occurred. Please contact the administrator.",
                        correlationId
                    });
                }

                return Ok(new
                {
                    canConnect = true,
                    databaseReachable = true,
                    totalTables = availableTables.Count,
                    totalViews = availableViews.Count,
                    availableTables = availableTables.OrderBy(t => t).ToList(),
                    availableViews = availableViews.OrderBy(v => v).ToList(),
                    lookingFor = new[] { "cts.vwRotation", "cts.vwService", "cts.vwWeeks", "cts.vwInstructorSchedule", "cts.vwStudentSchedule" },
                    missingFromEF = new[] { "cts.vwRotation", "cts.vwService", "cts.vwWeeks", "cts.vwInstructorSchedule", "cts.vwStudentSchedule" }
                        .Where(vw => !availableViews.Contains(vw))
                        .ToList(),
                    foundInCTS = availableViews.Where(v => v.StartsWith("cts.")).ToList()
                });
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Database diagnostic test failed. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, new
                {
                    error = "Database diagnostic test failed",
                    exceptionType = ex.GetType().Name,
                    message = ex.Message,
                    correlationId
                });
            }
        }

        /// <summary>
        /// Check actual column names in database tables
        /// </summary>
        [HttpGet("check-columns")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> CheckDatabaseColumns()
        {
            try
            {
                _logger.LogInformation("Checking actual column names in ClinicalScheduler database");

                var connectionString = _context.Database.GetConnectionString();
                var results = new Dictionary<string, object>();

                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync();

                // Check Rotation table columns
                using var rotCommand = connection.CreateCommand();
                rotCommand.CommandText = "SELECT TOP 1 * FROM Rotation";
                using var reader = await rotCommand.ExecuteReaderAsync();
                var rotationColumns = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    rotationColumns.Add(reader.GetName(i));
                }
                results["rotationActualColumns"] = rotationColumns;
                await reader.CloseAsync();

                // Check Service table columns  
                using var serviceCommand = connection.CreateCommand();
                serviceCommand.CommandText = "SELECT TOP 1 * FROM Service";
                using var reader2 = await serviceCommand.ExecuteReaderAsync();
                var serviceColumns = new List<string>();
                for (int i = 0; i < reader2.FieldCount; i++)
                {
                    serviceColumns.Add(reader2.GetName(i));
                }
                results["serviceActualColumns"] = serviceColumns;
                await reader2.CloseAsync();

                // Check InstructorSchedule table columns
                using var instructorCommand = connection.CreateCommand();
                instructorCommand.CommandText = "SELECT TOP 1 * FROM InstructorSchedule";
                using var reader3 = await instructorCommand.ExecuteReaderAsync();
                var instructorColumns = new List<string>();
                for (int i = 0; i < reader3.FieldCount; i++)
                {
                    instructorColumns.Add(reader3.GetName(i));
                }
                results["instructorScheduleActualColumns"] = instructorColumns;
                await reader3.CloseAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error checking database columns. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, new
                {
                    error = "Failed to check database columns",
                    message = ex.Message,
                    correlationId
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
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error retrieving available years. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, new
                {
                    error = "Failed to retrieve available years",
                    correlationId
                });
            }
        }
    }
}