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
        private readonly AAUDContext _aaudContext;
        private readonly WeekService _weekService;

        public CliniciansController(ClinicalSchedulerContext context, AAUDContext aaudContext, ILogger<CliniciansController> logger,
            AcademicYearService academicYearService, WeekService weekService, IMemoryCache cache)
            : base(academicYearService, cache, logger)
        {
            _context = context;
            _aaudContext = aaudContext;
            _weekService = weekService;
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
                return StatusCode(500, new { error = "An error occurred while fetching clinicians" });
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
                return StatusCode(500, new { error = "An error occurred while comparing counts" });
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
                // Filter by weeks that belong to the target academic year
                var weekIds = vWeeks.Select(w => w.WeekId).ToList();

                var schedules = await _context.InstructorSchedules
                    .Include(i => i.Rotation)
                    .Include(i => i.Service)
                    .Where(i => i.MothraId == mothraId && weekIds.Contains(i.WeekId))
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

                // Group schedules by semester (based on week term code) using vWeek data
                List<object> groupedSchedules;

                if (schedules.Any())
                {
                    // Pre-calculate semester names to avoid repeated calls during grouping
                    var semesterLookup = new Dictionary<int, string>();
                    foreach (var schedule in schedules)
                    {
                        // Get term code from vWeek data
                        var vWeek = vWeeks.FirstOrDefault(w => w.WeekId == schedule.WeekId);
                        if (vWeek != null && !semesterLookup.ContainsKey(vWeek.TermCode))
                        {
                            var semesterName = TermCodes.GetSemesterName(vWeek.TermCode);
                            var normalizedSemester = semesterName.StartsWith("Invalid Term Code") || semesterName.StartsWith("Unknown Term")
                                ? "Unknown Semester"
                                : semesterName;
                            semesterLookup[vWeek.TermCode] = normalizedSemester;
                        }
                    }

                    // Group schedules by semester using vWeek data for proper week numbers
                    groupedSchedules = schedules
                        .Select(s =>
                        {
                            var vWeek = vWeeks.FirstOrDefault(w => w.WeekId == s.WeekId);
                            return new
                            {
                                Schedule = s,
                                VWeek = vWeek,
                                Semester = vWeek != null && semesterLookup.ContainsKey(vWeek.TermCode)
                                    ? semesterLookup[vWeek.TermCode]
                                    : "Unknown Semester"
                            };
                        })
                        .Where(item => item.VWeek != null)
                        .GroupBy(item => item.Semester)
                        .Select(g => new
                        {
                            semester = g.Key,
                            weeks = g.Select(item => new
                            {
                                weekId = item.Schedule.WeekId,
                                weekNumber = item.VWeek!.WeekNum, // Use proper academic week number from vWeek
                                dateStart = item.Schedule.DateStart,
                                dateEnd = item.Schedule.DateEnd,
                                rotation = new
                                {
                                    rotationId = item.Schedule.RotationId,
                                    rotationName = item.Schedule.RotationName,
                                    abbreviation = item.Schedule.Abbreviation,
                                    serviceId = item.Schedule.ServiceId,
                                    serviceName = item.Schedule.ServiceName
                                },
                                isPrimaryEvaluator = item.Schedule.Evaluator
                            }).OrderBy(w => w.dateStart).ToList()
                        })
                        .OrderBy(g => TermCodes.GetSemesterOrder(g.semester))
                        .Cast<object>()
                        .ToList();
                }
                else
                {
                    // No schedules - create empty schedule with all weeks for the academic year
                    // Pre-calculate semester names for all weeks to avoid repeated calls
                    var weeksWithSemester = vWeeks.Select(w =>
                    {
                        var semesterName = TermCodes.GetSemesterName(w.TermCode);
                        var normalizedSemester = semesterName.StartsWith("Invalid Term Code") || semesterName.StartsWith("Unknown Term")
                            ? "Unknown Semester"
                            : semesterName;
                        return new
                        {
                            Week = new
                            {
                                weekId = w.WeekId,
                                weekNumber = w.WeekNum, // Use proper academic week number from vWeek
                                dateStart = w.DateStart,
                                dateEnd = w.DateEnd,
                                termCode = w.TermCode,
                                rotation = (object?)null, // No rotation assigned
                                isPrimaryEvaluator = false
                            },
                            Semester = normalizedSemester
                        };
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
                    academicYear = targetYear, // Returns the academic year from database settings
                    schedulesBySemester = groupedSchedules
                };

                _logger.LogInformation("Found {ScheduleCount} schedule entries for clinician {MothraId} (academic year {Year})",
                    schedules.Count, mothraId, targetYear);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching schedule for clinician {mothraId}");
                return StatusCode(500, new { error = "An error occurred while fetching the clinician schedule" });
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
                return StatusCode(500, new { error = "An error occurred while fetching clinician rotations" });
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
                return StatusCode(500, new { error = "An error occurred during data analysis" });
            }
        }


    }
}