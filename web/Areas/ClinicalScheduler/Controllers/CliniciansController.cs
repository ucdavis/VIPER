using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Viper.Models.AAUD;
using Web.Authorization;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("ClinicalScheduler")]
    [Permission(Allow = "SVMSecure.ClnSched")]
    public class CliniciansController : ApiController
    {
        private readonly ClinicalSchedulerContext _context;
        private readonly AAUDContext _aaudContext;
        private readonly ILogger<CliniciansController> _logger;

        public CliniciansController(ClinicalSchedulerContext context, AAUDContext aaudContext, ILogger<CliniciansController> logger)
        {
            _context = context;
            _aaudContext = aaudContext;
            _logger = logger;
        }

        /// <summary>
        /// Get a list of all unique clinicians, combining active clinicians with historically scheduled ones
        /// </summary>
        /// <param name="year">Year to filter clinicians by (optional, defaults to current year behavior)</param>
        /// <param name="includeAllAffiliates">If true, includes all affiliates instead of just active clinicians</param>
        /// <returns>List of clinicians with their basic info</returns>
        [HttpGet]
        public async Task<IActionResult> GetClinicians([FromQuery] int? year = null, [FromQuery] bool includeAllAffiliates = false)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var targetYear = year ?? currentYear;

                _logger.LogInformation("GetClinicians endpoint called with year: {Year}, includeAllAffiliates: {IncludeAllAffiliates}", targetYear, includeAllAffiliates);

                if (targetYear == currentYear)
                {
                    List<dynamic> sourceClinicians;

                    if (includeAllAffiliates)
                    {
                        _logger.LogInformation("Fetching all affiliates");

                        // Get all affiliates from VwCurrentAffiliates
                        sourceClinicians = await _aaudContext.VwCurrentAffiliates
                            .Where(a => !string.IsNullOrEmpty(a.IdsMothraid))
                            .Select(a => new
                            {
                                MothraId = a.IdsMothraid!,
                                FullName = !string.IsNullOrWhiteSpace(a.FirstName) || !string.IsNullOrWhiteSpace(a.LastName)
                                    ? $"{a.FirstName ?? ""} {a.LastName ?? ""}".Trim()
                                    : $"Affiliate {a.IdsMothraid}",
                                FirstName = a.FirstName ?? "",
                                LastName = a.LastName ?? "",
                                Source = "Affiliate"
                            })
                            .ToListAsync<dynamic>();
                    }
                    else
                    {
                        _logger.LogInformation("Fetching active clinicians plus those scheduled in past 2 years");

                        // Get active clinicians from VwVmthClinician (using "Y" for active status)
                        sourceClinicians = await _aaudContext.VwVmthClinicians
                            .Where(c => c.UserActive == "Y" && !string.IsNullOrEmpty(c.IdsMothraid))
                            .Select(c => new
                            {
                                MothraId = c.IdsMothraid!,
                                FullName = !string.IsNullOrWhiteSpace(c.FullName)
                                    ? c.FullName
                                    : !string.IsNullOrWhiteSpace(c.PersonDisplayFirstName) || !string.IsNullOrWhiteSpace(c.PersonDisplayLastName)
                                        ? $"{c.PersonDisplayFirstName ?? ""} {c.PersonDisplayLastName ?? ""}".Trim()
                                        : $"Clinician {c.IdsMothraid}",
                                FirstName = c.PersonDisplayFirstName ?? "",
                                LastName = c.PersonDisplayLastName ?? "",
                                Source = "Active"
                            })
                            .ToListAsync<dynamic>();
                    }

                    _logger.LogInformation("Found {SourceClinicianCount} source clinicians/affiliates", sourceClinicians.Count);

                    // Get historically scheduled clinicians from past 2 years
                    var twoYearsAgo = DateTime.Now.AddYears(-2);
                    var historicalClinicians = await _context.InstructorSchedules
                        .Where(i => i.DateStart >= twoYearsAgo)
                        .GroupBy(i => i.MothraId)
                        .Select(g => new
                        {
                            MothraId = g.Key,
                            FullName = g.First().FullName,
                            FirstName = g.First().FirstName,
                            LastName = g.First().LastName,
                            Source = "Historical"
                        })
                        .ToListAsync();

                    _logger.LogInformation("Found {HistoricalClinicianCount} historically scheduled clinicians", historicalClinicians.Count);

                    // Combine and deduplicate by MothraId
                    var allClinicians = sourceClinicians
                        .Concat(historicalClinicians.Cast<dynamic>())
                        .GroupBy(c => c.MothraId)
                        .Select(g => g.First()) // Take first occurrence (source clinicians come first)
                        .OrderBy(c => c.LastName)
                        .ThenBy(c => c.FirstName)
                        .Select(c => new
                        {
                            c.MothraId,
                            c.FullName,
                            c.FirstName,
                            c.LastName
                        })
                        .ToList();

                    _logger.LogInformation("Combined total: {TotalClinicianCount} unique clinicians", allClinicians.Count);
                    return Ok(allClinicians);
                }
                else
                {
                    // Past year: show clinicians only from that specific year
                    _logger.LogInformation("Fetching clinicians scheduled in {Year}", targetYear);

                    var clinicians = await _context.InstructorSchedules
                        .Where(i => i.DateStart.Year == targetYear)
                        .GroupBy(i => i.MothraId)
                        .Select(g => new
                        {
                            MothraId = g.Key,
                            FullName = g.First().FullName,
                            FirstName = g.First().FirstName,
                            LastName = g.First().LastName
                        })
                        .OrderBy(c => c.LastName)
                        .ThenBy(c => c.FirstName)
                        .ToListAsync();

                    _logger.LogInformation("Found {ClinicianCount} clinicians for year {Year}", clinicians.Count, targetYear);
                    return Ok(clinicians);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching clinicians");
                return StatusCode(500, new { error = "An error occurred while fetching clinicians", details = ex.Message });
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
                if (includeAllAffiliates)
                {
                    var count = await _aaudContext.VwCurrentAffiliates
                        .Where(a => !string.IsNullOrEmpty(a.IdsMothraid))
                        .CountAsync();
                    return Ok(new { count, source = "All Affiliates" });
                }
                else
                {
                    // Count active clinicians
                    var activeCount = await _aaudContext.VwVmthClinicians
                        .Where(c => c.UserActive == "Y" && !string.IsNullOrEmpty(c.IdsMothraid))
                        .CountAsync();

                    // Count historical
                    var twoYearsAgo = DateTime.Now.AddYears(-2);
                    var historicalCount = await _context.InstructorSchedules
                        .Where(i => i.DateStart >= twoYearsAgo)
                        .Select(i => i.MothraId)
                        .Distinct()
                        .CountAsync();

                    return Ok(new
                    {
                        activeCount,
                        historicalCount,
                        estimatedTotal = activeCount + historicalCount,
                        source = "Active Clinicians + Historical"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting clinicians");
                return StatusCode(500, new { error = "An error occurred while counting clinicians" });
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
                    .Where(i => i.DateStart >= twoYearsAgo)
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
                _logger.LogError(ex, "Error comparing counts");
                return StatusCode(500, new { error = "An error occurred while comparing counts", details = ex.Message });
            }
        }

        /// <summary>
        /// Get the schedule for a specific clinician
        /// </summary>
        /// <param name="mothraId">The MothraId of the clinician</param>
        /// <param name="year">The grad year to filter by (optional)</param>
        /// <returns>Schedule information for the clinician grouped by semester</returns>
        [HttpGet("{mothraId}/schedule")]
        public async Task<IActionResult> GetClinicianSchedule(string mothraId, [FromQuery] int? year = null)
        {
            try
            {
                _logger.LogInformation("Fetching schedule for clinician {MothraId}, year: {Year}", mothraId, year);

                // Build base query
                var query = _context.InstructorSchedules
                    .Include(i => i.Week)
                    .Include(i => i.Rotation)
                    .Include(i => i.Service)
                    .Where(i => i.MothraId == mothraId);

                // Apply year filter if provided
                if (year.HasValue)
                {
                    query = query.Where(i => i.Week.WeekGradYears.Any(wgy => wgy.GradYear == year.Value));
                }

                // Get the instructor schedules
                var schedules = await query
                    .OrderBy(i => i.DateStart)
                    .ToListAsync();

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

                // Group schedules by semester (based on week term code)
                List<object> groupedSchedules;

                if (schedules.Any())
                {
                    // Get all weeks for the actual years present in the schedule data
                    var scheduleYears = schedules.Select(s => s.DateStart.Year).Distinct().ToList();
                    var yearWeeks = await _context.Weeks
                        .Where(w => scheduleYears.Contains(w.DateStart.Year))
                        .OrderBy(w => w.DateStart)
                        .Select(w => new { w.WeekId, w.DateStart })
                        .ToListAsync();

                    // Create week number mapping by year
                    var weekNumberMap = new Dictionary<int, int>();
                    foreach (var yearGroup in yearWeeks.GroupBy(w => w.DateStart.Year))
                    {
                        var weeksInYear = yearGroup.OrderBy(w => w.DateStart).ToList();
                        for (int i = 0; i < weeksInYear.Count; i++)
                        {
                            weekNumberMap[weeksInYear[i].WeekId] = i + 1;
                        }
                    }

                    // Pre-calculate semester names to avoid repeated calls during grouping
                    var semesterLookup = new Dictionary<int, string>();
                    foreach (var schedule in schedules)
                    {
                        if (!semesterLookup.ContainsKey(schedule.Week.TermCode))
                        {
                            var semesterName = TermCodes.GetSemesterName(schedule.Week.TermCode);
                            var normalizedSemester = semesterName.StartsWith("Invalid Term Code") || semesterName.StartsWith("Unknown Term")
                                ? "Unknown Semester"
                                : semesterName;
                            semesterLookup[schedule.Week.TermCode] = normalizedSemester;
                        }
                    }

                    // Clinician has schedules - group them by pre-calculated semester
                    groupedSchedules = schedules
                        .GroupBy(s => semesterLookup[s.Week.TermCode])
                        .Select(g => new
                        {
                            semester = g.Key,
                            weeks = g.Select(s => new
                            {
                                weekId = s.WeekId,
                                weekNumber = weekNumberMap.ContainsKey(s.WeekId) ? weekNumberMap[s.WeekId] : 0,
                                dateStart = s.DateStart,
                                dateEnd = s.DateEnd,
                                rotation = new
                                {
                                    rotationId = s.RotationId,
                                    rotationName = s.RotationName,
                                    abbreviation = s.Abbreviation,
                                    serviceId = s.ServiceId,
                                    serviceName = s.ServiceName
                                },
                                isPrimaryEvaluator = s.Evaluator
                            }).OrderBy(w => w.dateStart).ToList()
                        })
                        .OrderBy(g => TermCodes.GetSemesterOrder(g.semester))
                        .Cast<object>()
                        .ToList();
                }
                else
                {
                    // No schedules - create empty schedule with all weeks for the year
                    var targetYear = year ?? DateTime.Now.Year;

                    // Get all weeks for the year and calculate week numbers like RotationsController
                    var yearWeeks = await _context.Weeks
                        .Where(w => w.DateStart.Year == targetYear)
                        .OrderBy(w => w.DateStart)
                        .Select(w => new { w.WeekId, w.DateStart, w.DateEnd, w.TermCode })
                        .ToListAsync();

                    // Calculate week numbers in memory (avoids N+1 queries and WeekGradYear dependency)
                    var allWeeks = yearWeeks
                        .Select((w, index) => new
                        {
                            weekId = w.WeekId,
                            weekNumber = index + 1, // Week numbers start from 1
                            dateStart = w.DateStart,
                            dateEnd = w.DateEnd,
                            termCode = w.TermCode,
                            rotation = (object?)null, // No rotation assigned
                            isPrimaryEvaluator = false
                        })
                        .ToList();

                    // Pre-calculate semester names for all weeks to avoid repeated calls
                    var weeksWithSemester = allWeeks.Select(w =>
                    {
                        var semesterName = TermCodes.GetSemesterName(w.termCode);
                        var normalizedSemester = semesterName.StartsWith("Invalid Term Code") || semesterName.StartsWith("Unknown Term")
                            ? "Unknown Semester"
                            : semesterName;
                        return new { Week = w, Semester = normalizedSemester };
                    }).ToList();

                    // Group weeks by pre-calculated semester
                    groupedSchedules = weeksWithSemester
                        .GroupBy(item => item.Semester)
                        .Select(g => new
                        {
                            semester = g.Key,
                            weeks = g.Select(item => item.Week).OrderBy(w => w.dateStart).ToList()
                        })
                        .OrderBy(g => TermCodes.GetSemesterOrder(g.semester))
                        .Cast<object>()
                        .ToList();
                }

                var result = new
                {
                    clinician = clinicianInfo,
                    schedulesBySemester = groupedSchedules
                };

                _logger.LogInformation("Found {ScheduleCount} schedule entries for clinician {MothraId}", schedules.Count, mothraId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching schedule for clinician {mothraId}");
                return StatusCode(500, new { error = "An error occurred while fetching the clinician schedule", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all rotations that a clinician has been scheduled for
        /// </summary>
        /// <param name="mothraId">The MothraId of the clinician</param>
        /// <returns>List of unique rotations for the clinician</returns>
        [HttpGet("{mothraId}/rotations")]
        public async Task<IActionResult> GetClinicianRotations(string mothraId)
        {
            try
            {
                _logger.LogInformation("Fetching rotations for clinician {MothraId}", mothraId);

                var rotations = await _context.InstructorSchedules
                    .Where(i => i.MothraId == mothraId)
                    .Select(i => new
                    {
                        i.RotationId,
                        i.RotationName,
                        i.Abbreviation,
                        i.ServiceId,
                        i.ServiceName
                    })
                    .Distinct()
                    .OrderBy(r => r.ServiceName)
                    .ThenBy(r => r.RotationName)
                    .ToListAsync();

                _logger.LogInformation("Found {RotationCount} unique rotations for clinician {MothraId}", rotations.Count, mothraId);
                return Ok(rotations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching rotations for clinician {mothraId}");
                return StatusCode(500, new { error = "An error occurred while fetching clinician rotations", details = ex.Message });
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
                    .Where(i => i.DateStart >= twoYearsAgo)
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
                _logger.LogError(ex, "Error analyzing clinician data");
                return StatusCode(500, new { error = "An error occurred during data analysis", details = ex.Message });
            }
        }
    }
}