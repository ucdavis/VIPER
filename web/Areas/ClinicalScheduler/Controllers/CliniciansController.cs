using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Areas.ClinicalScheduler.Services;
using Web.Authorization;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("ClinicalScheduler")]
    [Permission(Allow = "SVMSecure.ClnSched")]
    public class CliniciansController : BaseClinicalSchedulerController
    {
        private readonly ClinicalSchedulerContext _context;
        private readonly AAUDContext _aaudContext; // Legacy context for fallback clinician lookup
        private readonly WeekService _weekService;
        private readonly PersonService _personService;
        private readonly RotationService _rotationService;

        public CliniciansController(ClinicalSchedulerContext context, AAUDContext aaudContext, ILogger<CliniciansController> logger,
            AcademicYearService academicYearService, WeekService weekService, PersonService personService,
            RotationService rotationService, IMemoryCache cache)
            : base(academicYearService, cache, logger)
        {
            _context = context;
            _aaudContext = aaudContext;
            _weekService = weekService;
            _personService = personService;
            _rotationService = rotationService; // Added for proper rotation data loading
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
                var currentYear = DateTime.Now.Year;
                var targetYear = year ?? currentYear;

                _logger.LogInformation("GetClinicians endpoint called with year: {Year}, includeAllAffiliates: {IncludeAllAffiliates}", targetYear, includeAllAffiliates);

                if (targetYear == currentYear)
                {
                    // Use PersonService to get clinicians from Clinical Scheduler data
                    _logger.LogInformation("Using PersonService to get clinicians (includeAllAffiliates: {IncludeAllAffiliates})", includeAllAffiliates);

                    // Note: includeAllAffiliates parameter is kept for API compatibility
                    // but PersonService only has access to clinicians who have been scheduled
                    // This is actually better data quality since it's clinicians actually involved in scheduling
                    var clinicians = await _personService.GetCliniciansAsync(
                        includeHistorical: true,
                        sinceDays: includeAllAffiliates ? 1095 : 730, // 3 years vs 2 years for affiliates
                        cancellationToken: HttpContext.RequestAborted);

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
                    _logger.LogInformation("Using PersonService to get clinicians for year {Year}", targetYear);

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
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error fetching clinicians. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, new { error = "An error occurred while fetching clinicians", correlationId });
            }
        }

        /// <summary>
        /// Get a count of clinicians with different filters
        /// </summary>
        /// <returns>Count of clinicians</returns>
        [HttpGet("count")]
        public async Task<IActionResult> GetCliniciansCount([FromQuery] bool includeAllAffiliates = false)
        {
            try
            {
                // Get clinician count from PersonService
                var clinicians = await _personService.GetCliniciansAsync(
                    includeHistorical: true,
                    sinceDays: includeAllAffiliates ? 1095 : 730, // 3 years vs 2 years for affiliates
                    cancellationToken: HttpContext.RequestAborted);

                var count = clinicians.Count;
                var source = includeAllAffiliates ? "Clinical Scheduler Data (3 years)" : "Clinical Scheduler Data (2 years)";

                return Ok(new { count, source });
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error counting clinicians. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, new { error = "An error occurred while counting clinicians", correlationId });
            }
        }

        /// <summary>
        /// Compare counts between current clinicians and all affiliates
        /// </summary>
        /// <returns>Comparison of clinician vs affiliate counts</returns>
        [HttpGet("compare-counts")]
        public async Task<IActionResult> CompareCounts()
        {
            try
            {
                // First check total count without any filters
                var totalVwVmthClinicians = await _aaudContext.VwVmthClinicians.CountAsync();

                // Count with just MothraId filter
                var withMothraIdCount = await _aaudContext.VwVmthClinicians
                    .Where(c => !string.IsNullOrEmpty(c.IdsMothraid))
                    .CountAsync();

                // Check what UserActive values exist
                var userActiveValues = await _aaudContext.VwVmthClinicians
                    .Select(c => c.UserActive)
                    .Distinct()
                    .ToListAsync();

                // Count active clinicians from VwVmthClinician
                var activeClinicianCount = await _aaudContext.VwVmthClinicians
                    .Where(c => c.UserActive == "Y" && !string.IsNullOrEmpty(c.IdsMothraid))
                    .CountAsync();

                // Get unique MothraIds from active clinicians
                var activeClinicians = await _aaudContext.VwVmthClinicians
                    .Where(c => c.UserActive == "Y" && !string.IsNullOrEmpty(c.IdsMothraid))
                    .Select(c => c.IdsMothraid)
                    .Distinct()
                    .ToListAsync();

                // Count all affiliates from VwCurrentAffiliates
                var allAffiliatesCount = await _aaudContext.VwCurrentAffiliates
                    .Where(a => !string.IsNullOrEmpty(a.IdsMothraid))
                    .CountAsync();

                // Get unique MothraIds from all affiliates
                var allAffiliates = await _aaudContext.VwCurrentAffiliates
                    .Where(a => !string.IsNullOrEmpty(a.IdsMothraid))
                    .Select(a => a.IdsMothraid)
                    .Distinct()
                    .ToListAsync();

                // Count historically scheduled clinicians from past 2 years
                var twoYearsAgo = DateTime.Now.AddYears(-2);
                var historicalClinicians = await _context.InstructorSchedules
                    .Include(i => i.Week)
                    .Where(i => i.Week.DateStart >= twoYearsAgo)
                    .Select(i => i.MothraId)
                    .Distinct()
                    .ToListAsync();

                // Calculate the difference
                var cliniciansNotInAffiliates = activeClinicians.Except(allAffiliates).ToList();
                var affiliatesNotInClinicians = allAffiliates.Except(activeClinicians).ToList();

                // Combined current approach (active + historical)
                var currentApproachTotal = activeClinicians
                    .Union(historicalClinicians)
                    .Distinct()
                    .Count();

                return Ok(new
                {
                    TotalVwVmthClinicians = totalVwVmthClinicians,
                    VwVmthCliniciansWithMothraId = withMothraIdCount,
                    UserActiveValues = userActiveValues,
                    ActiveCliniciansCount = activeClinicianCount,
                    UniqueActiveCliniciansCount = activeClinicians.Count,
                    AllAffiliatesCount = allAffiliatesCount,
                    UniqueAffiliatesCount = allAffiliates.Count,
                    HistoricalCliniciansCount = historicalClinicians.Count,
                    CurrentApproachTotal = currentApproachTotal,
                    AdditionalPeopleIfUsingAffiliates = allAffiliates.Count - currentApproachTotal,
                    CliniciansNotInAffiliates = cliniciansNotInAffiliates.Count,
                    AffiliatesNotInClinicians = affiliatesNotInClinicians.Count,
                    SampleCliniciansNotInAffiliates = cliniciansNotInAffiliates.Take(5).ToList(),
                    SampleAffiliatesNotInClinicians = affiliatesNotInClinicians.Take(5).ToList()
                });
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error comparing counts. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, new { error = "An error occurred while comparing counts", correlationId });
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
            try
            {
                // Use academic year logic instead of calendar year
                var targetYear = await GetTargetYearAsync(year);
                _logger.LogInformation("Fetching schedule for clinician {MothraId}, academic year: {Year}", mothraId, targetYear);

                // Get weeks for the academic year using vWeek view (contains correct week numbers)
                var vWeeks = await _weekService.GetWeeksAsync(targetYear, includeExtendedRotation: true);

                _logger.LogInformation("Retrieved {WeekCount} weeks for year {Year}, unique WeekIds: {UniqueCount}",
                    vWeeks.Count, targetYear, vWeeks.Select(w => w.WeekId).Distinct().Count());

                if (!vWeeks.Any())
                {
                    _logger.LogWarning("No weeks found for academic year {Year}", targetYear);
                    // Still try to get clinician info from AAUD context
                    var clinicianFromAaud = await _aaudContext.VwVmthClinicians
                        .Where(c => c.IdsMothraid == mothraId)
                        .Select(c => new
                        {
                            mothraId = mothraId,
                            fullName = !string.IsNullOrWhiteSpace(c.FullName)
                                ? c.FullName
                                : !string.IsNullOrWhiteSpace(c.PersonDisplayFirstName) || !string.IsNullOrWhiteSpace(c.PersonDisplayLastName)
                                    ? $"{c.PersonDisplayFirstName ?? ""} {c.PersonDisplayLastName ?? ""}".Trim()
                                    : $"Clinician {mothraId}",
                            firstName = c.PersonDisplayFirstName ?? "",
                            lastName = c.PersonDisplayLastName ?? "",
                            role = (string?)null
                        })
                        .FirstOrDefaultAsync();

                    return Ok(new
                    {
                        clinician = clinicianFromAaud ?? new
                        {
                            mothraId = mothraId,
                            fullName = $"Clinician {mothraId}",
                            firstName = "",
                            lastName = "",
                            role = (string?)null
                        },
                        academicYear = targetYear,
                        schedulesBySemester = new List<object>()
                    });
                }

                // Get instructor schedules for this clinician using academic year filtering
                // Filter by weeks that belong to the target academic year (ensure unique week IDs)
                var weekIds = vWeeks.Select(w => w.WeekId).Distinct().ToList();

                // Load schedules filtered by both mothraId and the specific weeks for this grad year
                var schedules = await _context.InstructorSchedules
                    .Include(i => i.Week) // Week navigation works fine
                    .Where(i => i.MothraId == mothraId && weekIds.Contains(i.WeekId))
                    .OrderBy(i => i.Week.DateStart)
                    .ToListAsync();

                // Get unique rotation IDs from schedules and load rotation data in batch
                var rotationIds = schedules.Select(s => s.RotationId).Distinct().ToList();
                var rotations = await _context.Rotations
                    .Include(r => r.Service)
                    .Where(r => rotationIds.Contains(r.RotId))
                    .ToDictionaryAsync(r => r.RotId, r => r, HttpContext.RequestAborted);

                // Get clinician info - if no schedules, we still need to provide clinician details
                object clinicianInfo;

                if (!schedules.Any())
                {
                    _logger.LogInformation("No schedules found for clinician {MothraId} - returning empty schedule", mothraId);

                    // Try to get clinician info from AAUD context for cases where they have no schedules
                    var clinicianFromAaud = await _aaudContext.VwVmthClinicians
                        .Where(c => c.IdsMothraid == mothraId)
                        .Select(c => new
                        {
                            mothraId = mothraId,
                            fullName = !string.IsNullOrWhiteSpace(c.FullName)
                                ? c.FullName
                                : !string.IsNullOrWhiteSpace(c.PersonDisplayFirstName) || !string.IsNullOrWhiteSpace(c.PersonDisplayLastName)
                                    ? $"{c.PersonDisplayFirstName ?? ""} {c.PersonDisplayLastName ?? ""}".Trim()
                                    : $"Clinician {mothraId}",
                            firstName = c.PersonDisplayFirstName ?? "",
                            lastName = c.PersonDisplayLastName ?? "",
                            role = (string?)null
                        })
                        .FirstOrDefaultAsync();

                    if (clinicianFromAaud != null)
                    {
                        clinicianInfo = clinicianFromAaud;
                    }
                    else
                    {
                        // Fallback if clinician not found in AAUD either
                        clinicianInfo = new
                        {
                            mothraId = mothraId,
                            fullName = $"Clinician {mothraId}",
                            firstName = "",
                            lastName = "",
                            role = (string?)null
                        };
                    }
                }
                else
                {
                    // Get clinician info from first schedule record
                    clinicianInfo = new
                    {
                        mothraId = mothraId,
                        fullName = schedules.First().FullName,
                        firstName = schedules.First().FirstName,
                        lastName = schedules.First().LastName,
                        role = schedules.First().Role
                    };
                }

                // Always show ALL weeks for the academic year with assignments where they exist
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

                Dictionary<int, Models.CTS.InstructorSchedule> schedulesByWeekId;
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

                // Pre-calculate semester names for all weeks to avoid repeated calls
                var weeksWithSemester = vWeeks.Select(w =>
                {
                    var semesterName = TermCodes.GetSemesterName(w.TermCode);
                    var normalizedSemester = semesterName.StartsWith("Invalid Term Code") || semesterName.StartsWith("Unknown Term")
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
                    .OrderBy(g => TermCodes.GetSemesterOrder(g.semester))
                    .Cast<object>()
                    .ToList();

                var result = new
                {
                    clinician = clinicianInfo,
                    academicYear = targetYear, // Returns the academic year from database settings
                    schedulesBySemester = groupedSchedules
                };

                _logger.LogInformation("Found {ScheduleCount} schedule entries for clinician {MothraId} (academic year {Year})",
                    schedules.Count, mothraId, targetYear);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error fetching schedule for clinician {MothraId}. CorrelationId: {CorrelationId}", mothraId, correlationId);
                return StatusCode(500, new { error = "An error occurred while fetching the clinician schedule", correlationId });
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
                var rotations = await _context.Rotations
                    .Include(r => r.Service)
                    .Where(r => rotationIds.Contains(r.RotId))
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
                    .ToListAsync();

                _logger.LogInformation("Found {RotationCount} unique rotations for clinician {MothraId}", rotations.Count, mothraId);
                return Ok(rotations);
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error fetching rotations for clinician {MothraId}. CorrelationId: {CorrelationId}", mothraId, correlationId);
                return StatusCode(500, new { error = "An error occurred while fetching clinician rotations", correlationId });
            }
        }

        /// <summary>
        /// Sanity check endpoint to analyze overlap between scheduled clinicians and VwVmthClinician
        /// </summary>
        [HttpGet("data-analysis")]
        public async Task<IActionResult> AnalyzeClinicianData()
        {
            try
            {
                _logger.LogInformation("Starting clinician data analysis");

                // Get historically scheduled clinicians from past 2 years
                var twoYearsAgo = DateTime.Now.AddYears(-2);
                var historicalClinicians = await _context.InstructorSchedules
                    .Include(i => i.Week)
                    .Where(i => i.Week.DateStart >= twoYearsAgo)
                    .GroupBy(i => i.MothraId)
                    .Select(g => new
                    {
                        MothraId = g.Key,
                        FullName = g.First().FullName,
                        FirstName = g.First().FirstName,
                        LastName = g.First().LastName,
                        ScheduleCount = g.Count(),
                        FirstScheduled = g.Min(x => x.DateStart),
                        LastScheduled = g.Max(x => x.DateStart)
                    })
                    .ToListAsync();

                // Get all clinicians from VwVmthClinician (both active and inactive)
                var vmthClinicians = await _aaudContext.VwVmthClinicians
                    .Where(c => !string.IsNullOrEmpty(c.IdsMothraid))
                    .Select(c => new
                    {
                        MothraId = c.IdsMothraid!,
                        FullName = c.FullName ?? $"{c.PersonDisplayFirstName} {c.PersonDisplayLastName}".Trim(),
                        FirstName = c.PersonDisplayFirstName ?? "",
                        LastName = c.PersonDisplayLastName ?? "",
                        UserActive = c.UserActive,
                        UserStatus = c.UserStatus,
                        UserDeactivateDate = c.UserDeactivateDate
                    })
                    .ToListAsync();

                // Get active clinicians only
                var activeClinicians = vmthClinicians.Where(c => c.UserActive == "Y").ToList();

                // Analysis
                var historicalMothraIds = historicalClinicians.Select(c => c.MothraId).ToHashSet();
                var vmthMothraIds = vmthClinicians.Select(c => c.MothraId).ToHashSet();
                var activeMothraIds = activeClinicians.Select(c => c.MothraId).ToHashSet();

                // Find overlaps and gaps
                var scheduledButNotInVmth = historicalClinicians
                    .Where(h => !vmthMothraIds.Contains(h.MothraId))
                    .ToList();

                var scheduledButNotActive = historicalClinicians
                    .Where(h => vmthMothraIds.Contains(h.MothraId) && !activeMothraIds.Contains(h.MothraId))
                    .Select(h => new
                    {
                        h.MothraId,
                        h.FullName,
                        h.ScheduleCount,
                        h.LastScheduled,
                        VmthStatus = vmthClinicians.First(v => v.MothraId == h.MothraId)
                    })
                    .ToList();

                var activeButNeverScheduled = activeClinicians
                    .Where(a => !historicalMothraIds.Contains(a.MothraId))
                    .ToList();

                var perfectOverlap = historicalClinicians
                    .Where(h => activeMothraIds.Contains(h.MothraId))
                    .ToList();

                var result = new
                {
                    Summary = new
                    {
                        HistoricallyScheduledCount = historicalClinicians.Count,
                        TotalVmthCliniciansCount = vmthClinicians.Count,
                        ActiveVmthCliniciansCount = activeClinicians.Count,
                        PerfectOverlapCount = perfectOverlap.Count,
                        ScheduledButNotInVmthCount = scheduledButNotInVmth.Count,
                        ScheduledButNotActiveCount = scheduledButNotActive.Count,
                        ActiveButNeverScheduledCount = activeButNeverScheduled.Count
                    },
                    Details = new
                    {
                        ScheduledButNotInVmth = scheduledButNotInVmth.Take(10), // Sample of problematic cases
                        ScheduledButNotActive = scheduledButNotActive.Take(10),
                        ActiveButNeverScheduled = activeButNeverScheduled.Take(10),
                        PerfectOverlap = perfectOverlap.Take(5) // Sample of good cases
                    },
                    DateRange = new
                    {
                        AnalysisDate = DateTime.Now,
                        HistoricalDataFrom = twoYearsAgo,
                        HistoricalDataTo = DateTime.Now
                    }
                };

                _logger.LogInformation("Analysis complete - Historical: {HistoricalCount}, Active VMTH: {ActiveCount}, Perfect overlap: {OverlapCount}", historicalClinicians.Count, activeClinicians.Count, perfectOverlap.Count);

                return Ok(result);
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error analyzing clinician data. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, new { error = "An error occurred during data analysis", correlationId });
            }
        }


    }
}