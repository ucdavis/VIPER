using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for dashboard statistics and data hygiene operations.
/// Supports department-level filtering for users with ViewDept permission.
/// Alert states are persisted to the database for durability across server restarts.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly EffortDbContext _context;
    private readonly ITermService _termService;

    public DashboardService(EffortDbContext context, ITermService termService)
    {
        _context = context;
        _termService = termService;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(int termCode, List<string>? departmentCodes = null, CancellationToken ct = default)
    {
        // Empty list (not null) means ViewDept user with no authorized departments - return empty data
        if (departmentCodes != null && departmentCodes.Count == 0)
        {
            return new DashboardStatsDto
            {
                CurrentTerm = await _termService.GetTermAsync(termCode, ct)
            };
        }

        var term = await _termService.GetTermAsync(termCode, ct);
        var hasDeptFilter = departmentCodes != null && departmentCodes.Count > 0;

        var personsQuery = _context.Persons.Where(p => p.TermCode == termCode);
        if (hasDeptFilter)
        {
            personsQuery = personsQuery.Where(p => departmentCodes!.Contains(p.EffortDept));
        }

        // Get basic instructor counts
        var totalInstructors = await personsQuery.CountAsync(ct);
        var verifiedInstructors = await personsQuery.CountAsync(p => p.EffortVerified != null, ct);

        // Get person IDs who have records (separate query to avoid nested aggregates)
        var personIdsWithRecords = await _context.Records
            .Where(r => r.TermCode == termCode)
            .Select(r => r.PersonId)
            .Distinct()
            .ToListAsync(ct);

        var instructorsWithNoRecords = await personsQuery
            .CountAsync(p => !personIdsWithRecords.Contains(p.PersonId), ct);

        var coursesQuery = _context.Courses.Where(c => c.TermCode == termCode);
        if (hasDeptFilter)
        {
            coursesQuery = coursesQuery.Where(c => departmentCodes!.Contains(c.CustDept));
        }

        // Get basic course counts
        var totalCourses = await coursesQuery.CountAsync(ct);

        // Get course IDs that have records (separate query to avoid nested aggregates)
        var courseIdsWithRecords = await _context.Records
            .Where(r => r.TermCode == termCode)
            .Select(r => r.CourseId)
            .Distinct()
            .ToListAsync(ct);

        var coursesWithInstructors = await coursesQuery
            .CountAsync(c => courseIdsWithRecords.Contains(c.Id), ct);

        var recordsQuery = _context.Records.Where(r => r.TermCode == termCode);
        if (hasDeptFilter)
        {
            // Filter records by person's department
            var personIds = await personsQuery.Select(p => p.PersonId).ToListAsync(ct);
            recordsQuery = recordsQuery.Where(r => personIds.Contains(r.PersonId));
        }
        var totalRecords = await recordsQuery.CountAsync(ct);

        // Get alerts for summary (with department filter)
        var alerts = await GetDataHygieneAlertsAsync(termCode, departmentCodes, ct);

        return new DashboardStatsDto
        {
            CurrentTerm = term,
            TotalInstructors = totalInstructors,
            VerifiedInstructors = verifiedInstructors,
            TotalCourses = totalCourses,
            CoursesWithInstructors = coursesWithInstructors,
            TotalRecords = totalRecords,
            InstructorsWithNoRecords = instructorsWithNoRecords,
            HygieneSummary = new DataHygieneSummaryDto
            {
                ActiveAlerts = alerts.Count(a => a.Status == "Active"),
                ResolvedAlerts = alerts.Count(a => a.Status == "Resolved"),
                IgnoredAlerts = alerts.Count(a => a.Status == "Ignored")
            }
        };
    }

    public async Task<List<DepartmentVerificationDto>> GetDepartmentVerificationAsync(int termCode, List<string>? departmentCodes = null, int threshold = 80, CancellationToken ct = default)
    {
        // Empty list (not null) means ViewDept user with no authorized departments - return empty data
        if (departmentCodes != null && departmentCodes.Count == 0)
        {
            return [];
        }

        var hasDeptFilter = departmentCodes != null && departmentCodes.Count > 0;

        var personsQuery = _context.Persons.Where(p => p.TermCode == termCode);
        if (hasDeptFilter)
        {
            personsQuery = personsQuery.Where(p => departmentCodes!.Contains(p.EffortDept));
        }

        // Get department breakdown
        var deptStats = await personsQuery
            .GroupBy(p => p.EffortDept)
            .Select(g => new
            {
                DepartmentCode = g.Key,
                TotalInstructors = g.Count(),
                VerifiedInstructors = g.Count(p => p.EffortVerified != null)
            })
            .ToListAsync(ct);

        // Get department names from ReportUnits
        var deptCodesToLookup = deptStats.Select(d => d.DepartmentCode).ToList();
        var deptNames = await _context.ReportUnits
            .Where(ru => deptCodesToLookup.Contains(ru.UnitCode))
            .ToDictionaryAsync(ru => ru.UnitCode, ru => ru.UnitName, ct);

        return deptStats
            .Select(d =>
            {
                var code = string.IsNullOrWhiteSpace(d.DepartmentCode) ? "" : d.DepartmentCode;
                var displayName = string.IsNullOrWhiteSpace(code)
                    ? "No Department"
                    : deptNames.GetValueOrDefault(code) ?? code;

                return new DepartmentVerificationDto
                {
                    DepartmentCode = code,
                    DepartmentName = displayName,
                    TotalInstructors = d.TotalInstructors,
                    VerifiedInstructors = d.VerifiedInstructors,
                    MeetsThreshold = d.TotalInstructors > 0 && (double)d.VerifiedInstructors / d.TotalInstructors * 100 >= threshold
                };
            })
            .OrderBy(d => d.MeetsThreshold)
            .ThenByDescending(d => d.TotalInstructors)
            .ToList();
    }

    public async Task<List<EffortChangeAlertDto>> GetDataHygieneAlertsAsync(int termCode, List<string>? departmentCodes = null, CancellationToken ct = default)
    {
        // Empty list (not null) means ViewDept user with no authorized departments - return empty data
        if (departmentCodes != null && departmentCodes.Count == 0)
        {
            return [];
        }

        var alerts = new List<EffortChangeAlertDto>();
        var hasDeptFilter = departmentCodes != null && departmentCodes.Count > 0;

        // Get instructors with no effort records
        var instructorsQuery = _context.Persons.Where(p => p.TermCode == termCode);
        if (hasDeptFilter)
        {
            instructorsQuery = instructorsQuery.Where(p => departmentCodes!.Contains(p.EffortDept));
        }

        var instructorsNoRecords = await instructorsQuery
            .Where(p => !_context.Records.Any(r => r.PersonId == p.PersonId && r.TermCode == termCode))
            .Where(p => p.FirstName != "GUEST") // Exclude guest accounts
            .Select(p => new { p.PersonId, p.FirstName, p.LastName, p.EffortDept })
            .ToListAsync(ct);

        foreach (var instructor in instructorsNoRecords)
        {
            var fullName = $"{instructor.FirstName} {instructor.LastName}".Trim();
            alerts.Add(new EffortChangeAlertDto
            {
                AlertType = "NoRecords",
                Title = "No Effort Records",
                Description = "Instructor imported but has no course assignments",
                EntityType = "Instructor",
                EntityId = instructor.PersonId.ToString(),
                EntityName = fullName,
                DepartmentCode = instructor.EffortDept,
                Severity = "Medium"
            });
        }

        // Get courses with no instructors
        var coursesQuery = _context.Courses.Where(c => c.TermCode == termCode);
        if (hasDeptFilter)
        {
            coursesQuery = coursesQuery.Where(c => departmentCodes!.Contains(c.CustDept));
        }

        // S6610: EF Core LINQ-to-SQL doesn't support EndsWith(char), only EndsWith(string)
#pragma warning disable S6610
        var coursesNoInstructors = await coursesQuery
            .Where(c => !_context.Records.Any(r => r.CourseId == c.Id))
            .Where(c => !c.CrseNumb.Trim().EndsWith("R")) // TODO(VPR-41): EF Core 10 supports char overload, remove pragma
            .Select(c => new { c.Id, c.SubjCode, c.CrseNumb, c.SeqNumb, c.CustDept })
            .ToListAsync(ct);
#pragma warning restore S6610

        foreach (var course in coursesNoInstructors)
        {
            var courseName = $"{course.SubjCode.Trim()} {course.CrseNumb.Trim()}-{course.SeqNumb.Trim()}";
            alerts.Add(new EffortChangeAlertDto
            {
                AlertType = "NoInstructors",
                Title = "No Instructors Assigned",
                Description = "Course has no instructor assignments",
                EntityType = "Course",
                EntityId = course.Id.ToString(),
                EntityName = courseName,
                DepartmentCode = course.CustDept,
                Severity = "Medium"
            });
        }

        // Get instructors with no department assigned (data quality issue)
        // Only show to admins - ViewDept users can't meaningfully act on these
        if (!hasDeptFilter)
        {
            var instructorsNoDept = await _context.Persons
                .Where(p => p.TermCode == termCode && (p.EffortDept == null || p.EffortDept == ""))
                .Select(p => new { p.PersonId, p.FirstName, p.LastName, p.EffortDept })
                .ToListAsync(ct);

            foreach (var instructor in instructorsNoDept)
            {
                var fullName = $"{instructor.FirstName} {instructor.LastName}".Trim();
                alerts.Add(new EffortChangeAlertDto
                {
                    AlertType = "NoDepartment",
                    Title = "No Department Assigned",
                    Description = "",
                    EntityType = "Instructor",
                    EntityId = instructor.PersonId.ToString(),
                    EntityName = fullName,
                    DepartmentCode = instructor.EffortDept ?? "UNK",
                    Severity = "High"
                });
            }
        }

        // Get effort records with 0 hours assigned (data quality issue)
        var recordsQuery = _context.Records.Where(r => r.TermCode == termCode && r.Hours == 0);
        if (hasDeptFilter)
        {
            var personIdsInDepts = await instructorsQuery.Select(p => p.PersonId).ToListAsync(ct);
            recordsQuery = recordsQuery.Where(r => personIdsInDepts.Contains(r.PersonId));
        }

        var zeroHourRecords = await recordsQuery
            .Join(_context.Persons.Where(p => p.TermCode == termCode), r => r.PersonId, p => p.PersonId, (r, p) => new { r.Id, r.PersonId, p.FirstName, p.LastName, p.EffortDept, r.CourseId })
            .Join(_context.Courses, x => x.CourseId, c => c.Id, (x, c) => new { x.Id, x.PersonId, x.FirstName, x.LastName, x.EffortDept, c.SubjCode, c.CrseNumb, c.SeqNumb })
            .ToListAsync(ct);

        foreach (var record in zeroHourRecords)
        {
            var fullName = $"{record.FirstName} {record.LastName}".Trim();
            var courseName = $"{record.SubjCode.Trim()} {record.CrseNumb.Trim()}-{record.SeqNumb.Trim()}";
            alerts.Add(new EffortChangeAlertDto
            {
                AlertType = "ZeroHours",
                Title = "Zero Hours Assigned",
                Description = $"0 hours on {courseName}",
                EntityType = "Instructor",
                EntityId = record.PersonId.ToString(),
                EntityName = fullName,
                DepartmentCode = record.EffortDept,
                Severity = "Medium"
            });
        }

        // Get instructors not verified past deadline (if term is open and has an expected completion)
        var instructorsNotVerified = await instructorsQuery
            .Where(p => p.EffortVerified == null)
            .Select(p => new { p.PersonId, p.FirstName, p.LastName, p.EffortDept })
            .ToListAsync(ct);

        // Only add as alerts if term has been open for a while (30+ days)
        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        // S6561: This is a business date comparison (not benchmarking) - clock drift is acceptable
#pragma warning disable S6561
        var isOverdue = term?.OpenedDate != null && (DateTime.Today - term.OpenedDate.Value).TotalDays > 30;
#pragma warning restore S6561

        if (isOverdue)
        {
            foreach (var instructor in instructorsNotVerified)
            {
                var fullName = $"{instructor.FirstName} {instructor.LastName}".Trim();
                alerts.Add(new EffortChangeAlertDto
                {
                    AlertType = "NotVerified",
                    Title = "Verification Overdue",
                    Description = "Effort not verified after 30+ days",
                    EntityType = "Instructor",
                    EntityId = instructor.PersonId.ToString(),
                    EntityName = fullName,
                    DepartmentCode = instructor.EffortDept,
                    Severity = "Low"
                });
            }
        }

        // Apply persisted states from database
        await ApplyAlertStatesAsync(alerts, termCode, departmentCodes, ct);

        return alerts.OrderByDescending(a => a.Severity == "High")
            .ThenByDescending(a => a.Severity == "Medium")
            .ThenBy(a => a.Status == "Ignored")
            .ToList();
    }

    public async Task<bool> IgnoreAlertAsync(int termCode, string alertType, string entityId, int ignoredBy, List<string>? departmentCodes = null, CancellationToken ct = default)
    {
        // Verify the alert exists and is within the user's authorized scope
        var alerts = await GetDataHygieneAlertsAsync(termCode, departmentCodes, ct);
        if (!alerts.Any(a => a.AlertType == alertType && a.EntityId == entityId))
        {
            return false;
        }

        var existingState = await _context.AlertStates
            .FirstOrDefaultAsync(a => a.TermCode == termCode && a.AlertType == alertType && a.EntityId == entityId, ct);

        if (existingState != null)
        {
            existingState.Status = "Ignored";
            existingState.IgnoredBy = ignoredBy;
            existingState.IgnoredDate = DateTime.Now;
            existingState.ModifiedDate = DateTime.Now;
            existingState.ModifiedBy = ignoredBy;
        }
        else
        {
            _context.AlertStates.Add(new AlertState
            {
                TermCode = termCode,
                AlertType = alertType,
                EntityType = GetEntityTypeForAlertType(alertType),
                EntityId = entityId,
                Status = "Ignored",
                IgnoredBy = ignoredBy,
                IgnoredDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                ModifiedBy = ignoredBy
            });
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }

    private static string GetEntityTypeForAlertType(string alertType)
    {
        return alertType switch
        {
            "NoInstructors" => "Course",
            _ => "Instructor"
        };
    }

    private async Task ApplyAlertStatesAsync(List<EffortChangeAlertDto> alerts, int termCode, List<string>? departmentCodes, CancellationToken ct)
    {
        if (alerts.Count == 0) return;

        // Batch load all states for this term
        var states = await _context.AlertStates
            .Where(a => a.TermCode == termCode)
            .ToListAsync(ct);

        var stateDict = states.ToDictionary(
            a => (a.AlertType, a.EntityId),
            a => a);

        // Mark alerts that no longer exist as Resolved
        // Only perform this cleanup when viewing the full picture (no dept filter)
        // Otherwise we would incorrectly resolve alerts from other departments
        if (departmentCodes == null)
        {
            var currentAlertKeys = alerts.Select(a => (a.AlertType, EntityId: a.EntityId)).ToHashSet();
            foreach (var state in states.Where(s => s.Status == "Active" && !currentAlertKeys.Contains((s.AlertType, s.EntityId))))
            {
                state.Status = "Resolved";
                state.ResolvedDate = DateTime.Now;
                state.ModifiedDate = DateTime.Now;
            }
        }

        // Apply states to alerts
        foreach (var alert in alerts.Where(a => stateDict.ContainsKey((a.AlertType, a.EntityId))))
        {
            var state = stateDict[(alert.AlertType, alert.EntityId)];
            alert.Status = state.Status;
            if (state.IgnoredDate.HasValue)
            {
                alert.ReviewedDate = state.IgnoredDate;
            }
        }

        await _context.SaveChangesAsync(ct);
    }
}
