using System.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

public class MeritMultiYearService : BaseReportService, IMeritMultiYearService
{
    private readonly ILogger<MeritMultiYearService> _logger;

    public MeritMultiYearService(
        EffortDbContext context,
        ILogger<MeritMultiYearService> logger)
        : base(context)
    {
        _logger = logger;
    }

    // ============================================
    // Multi-Year Report
    // ============================================

    public async Task<MultiYearReport> GetMultiYearReportAsync(
        int personId,
        int startYear,
        int endYear,
        string? excludeClinicalTerms = null,
        string? excludeDidacticTerms = null,
        bool useAcademicYear = false,
        CancellationToken ct = default)
    {
        // Legacy convention: endYear is the review year; data covers through endYear - 1
        // (legacy ColdFusion dropdown submits currentYear-1 as the value)
        var originalEndYear = endYear;
        endYear -= 1;

        // Auto-load sabbatical exclusions when none explicitly provided
        if (string.IsNullOrWhiteSpace(excludeClinicalTerms) && string.IsNullOrWhiteSpace(excludeDidacticTerms))
        {
            var sabbatical = await _context.Sabbaticals
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.PersonId == personId, ct);

            if (sabbatical != null)
            {
                excludeClinicalTerms = sabbatical.ExcludeClinicalTerms;
                excludeDidacticTerms = sabbatical.ExcludeDidacticTerms;
            }
        }

        // Resolve PersonId to MothraId via ViperPerson
        var viperPerson = await _context.ViperPersons
            .AsNoTracking()
            .FirstOrDefaultAsync(vp => vp.PersonId == personId, ct);

        if (viperPerson == null)
        {
            return new MultiYearReport
            {
                StartYear = startYear,
                EndYear = endYear,
                UseAcademicYear = useAcademicYear
            };
        }

        var mothraId = viperPerson.MothraId;

        // Calculate term range for sp_merit_multiyear
        int startTermCode, endTermCode;
        if (useAcademicYear)
        {
            startTermCode = startYear * 100 + 4;    // Summer term
            endTermCode = endYear * 100 + 3;         // Spring term
        }
        else
        {
            startTermCode = startYear * 100;
            endTermCode = (endYear + 1) * 100 - 1;
        }

        // Execute merit and eval SPs in sequence (DbContext not thread-safe)
        var meritRows = await ExecuteMeritMultiyearSpAsync(
            personId, startTermCode, endTermCode,
            excludeClinicalTerms, excludeDidacticTerms, ct);

        var evalRows = await ExecuteEvalsMultiyearSpAsync(
            startYear, originalEndYear, mothraId,
            excludeDidacticTerms, useAcademicYear, ct);

        // Resolve instructor name and department from merit rows or DB
        var instructor = meritRows.Count > 0
            ? meritRows[0].Instructor
            : "";
        var rawDepartment = meritRows.Count > 0
            ? meritRows[0].Department
            : "";

        if (string.IsNullOrWhiteSpace(instructor) || string.IsNullOrWhiteSpace(rawDepartment))
        {
            // Fall back to most recent EffortPerson record
            var effortPerson = await _context.Persons
                .AsNoTracking()
                .Where(p => p.PersonId == personId)
                .OrderByDescending(p => p.TermCode)
                .FirstOrDefaultAsync(ct);

            if (effortPerson != null)
            {
                if (string.IsNullOrWhiteSpace(instructor))
                {
                    instructor = $"{effortPerson.LastName?.Trim()}, {effortPerson.FirstName?.Trim()}";
                }
                if (string.IsNullOrWhiteSpace(rawDepartment))
                {
                    rawDepartment = effortPerson.EffortDept ?? "";
                }
            }
        }

        // Resolve named department + job group description (matching legacy getDeptAndJobGroup)
        var (department, jobGroupDesc) = await ResolveNamedDepartmentAsync(
            personId, startTermCode, endTermCode, rawDepartment, ct);

        // Build effort types list matching legacy: always-show types + any extras with data
        var alwaysShowTypes = new List<string> { "CLI", "VAR", "LEC", "LAB", "DIS", "PBL", "CBL", "TBL", "PRS", "JLC", "EXM" };
        var dataTypes = meritRows
            .Where(r => !string.IsNullOrWhiteSpace(r.EffortTypeId))
            .Select(r => r.EffortTypeId.Trim())
            .Distinct()
            .ToHashSet();
        var extraTypes = dataTypes
            .Where(t => !alwaysShowTypes.Contains(t))
            .OrderBy(t => t)
            .ToList();
        var effortTypes = alwaysShowTypes.Concat(extraTypes).ToList();

        var report = new MultiYearReport
        {
            MothraId = mothraId,
            Instructor = instructor.Trim(),
            Department = department.Trim(),
            StartYear = startYear,
            EndYear = endYear,
            UseAcademicYear = useAcademicYear,
            ExcludedClinicalTerms = FilterTermsInRange(ParseTermList(excludeClinicalTerms), startTermCode, endTermCode),
            ExcludedDidacticTerms = FilterTermsInRange(ParseTermList(excludeDidacticTerms), startTermCode, endTermCode),
            EffortTypes = effortTypes,
            MeritSection = BuildMeritSection(meritRows, effortTypes, useAcademicYear),
            EvalSection = BuildEvalSection(evalRows, useAcademicYear)
        };

        // Compute yearly averages (matching legacy: denom=1 for CLI, didacticYears for others)
        ComputeYearlyAverages(report, startYear, endYear, excludeDidacticTerms, useAcademicYear);

        // Compute department averages using activity exclude SP
        await PopulateDepartmentAveragesAsync(
            report, mothraId, department, jobGroupDesc, startYear, endYear,
            useAcademicYear, ct);

        // Compute eval department average using raw dept (the eval SP filters by EffortDept, not named dept)
        var evalDept = evalRows.Count > 0 ? evalRows[0].Dept?.Trim() ?? "" : rawDepartment;
        await PopulateEvalDepartmentAverageAsync(
            report, evalDept, startYear, originalEndYear, excludeDidacticTerms,
            useAcademicYear, ct);

        _logger.LogDebug(
            "Multi-year report for person {PersonId}: {MeritYearCount} merit years, {EvalYearCount} eval years, {EffortTypeCount} effort types",
            personId,
            report.MeritSection.Years.Count,
            report.EvalSection.Years.Count,
            effortTypes.Count);

        return report;
    }

    // ── Department Resolution ────────────────────────────────────────

    /// <summary>
    /// Map raw EffortDept to named department and job group description using job group mapping.
    /// Matches legacy getDeptAndJobGroup + fn_getEffortDept + fn_getEffortTitle.
    /// </summary>
    private async Task<(string NamedDept, string JobGroupDesc)> ResolveNamedDepartmentAsync(
        int personId, int startTermCode, int endTermCode, string rawDepartment,
        CancellationToken ct)
    {
        var person = await _context.Persons
            .AsNoTracking()
            .Where(p => p.PersonId == personId
                && p.TermCode >= startTermCode
                && p.TermCode <= endTermCode)
            .OrderByDescending(p => p.TermCode)
            .FirstOrDefaultAsync(ct);

        // Fallback to any record if none in range
        person ??= await _context.Persons
            .AsNoTracking()
            .Where(p => p.PersonId == personId)
            .OrderByDescending(p => p.TermCode)
            .FirstOrDefaultAsync(ct);

        if (person == null)
        {
            return (rawDepartment, "PROFESSOR/IR");
        }

        var jobGroupId = person.JobGroupId?.Trim() ?? "";
        var reportUnit = person.ReportUnit?.Trim() ?? "";

        // Named department (matching legacy fn_getEffortDept)
        string namedDept;
        if (jobGroupId is "010" or "011" or "114" or "311" or "124" or "S56")
        {
            namedDept = person.EffortDept?.Trim() ?? rawDepartment;
        }
        else if (reportUnit.Contains("CAHFS", StringComparison.OrdinalIgnoreCase))
        {
            namedDept = "CAHFS";
        }
        else if (reportUnit.Contains("WHC", StringComparison.OrdinalIgnoreCase))
        {
            namedDept = "WHC";
        }
        else if (jobGroupId == "317")
        {
            namedDept = "VMTH";
        }
        else
        {
            namedDept = "All";
        }

        // Job group description (must match sp_dept_count_by_job_group_exclude filter values)
        var jobGroupDesc = jobGroupId switch
        {
            "335" => "ADJUNCT PROFESSOR",
            "341" => "CLINICAL PROFESSOR",
            "317" => "PROF OF CLIN ______",
            _ => "PROFESSOR/IR"
        };

        return (namedDept, jobGroupDesc);
    }

    // ── Yearly Average Calculation ───────────────────────────────────

    /// <summary>
    /// Compute yearly averages matching legacy behavior:
    /// - CLI/CLIH/CLIW: denom = 1 (legacy hardcodes this)
    /// - All other types: denom = didacticYears (endYear - startYear + 1 minus excluded term credit)
    /// </summary>
    private static void ComputeYearlyAverages(
        MultiYearReport report, int startYear, int endYear,
        string? excludeDidacticTerms, bool useAcademicYear)
    {
        var grandTotals = report.MeritSection.GrandTotals;
        var yearlyAverages = new Dictionary<string, decimal>();

        // Calculate didactic years (matching legacy numYears.cfc getDidacticYears)
        var adj = useAcademicYear ? 0 : 1;
        decimal excludeCredit = 0;
        if (!string.IsNullOrWhiteSpace(excludeDidacticTerms))
        {
            foreach (var termStr in excludeDidacticTerms.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(termStr.Trim(), out var term))
                {
                    excludeCredit += GetExcludeTermValue(startYear, endYear, term, useAcademicYear);
                }
            }
        }
        var didacticYears = endYear - startYear + adj - excludeCredit;
        if (didacticYears <= 0) didacticYears = 1;

        foreach (var effortType in report.EffortTypes)
        {
            var total = grandTotals.GetValueOrDefault(effortType);
            if (total <= 0) continue;

            // Legacy hardcodes denom=1 for clinical effort types
            var isClinical = effortType is "CLI" or "CLIH" or "CLIW";
            var denom = isClinical ? 1m : didacticYears;

            yearlyAverages[effortType] = Math.Round(total / denom, 1);
        }

        report.MeritSection.YearlyAverages = yearlyAverages;
    }

    /// <summary>
    /// Calculate excluded term credit (matching legacy numYears.cfc excludeTermValue).
    /// Quarter terms (01,03,08,10) count as 0.25 of a year.
    /// </summary>
    private static decimal GetExcludeTermValue(int startYear, int endYear, int term, bool useAcademicYear)
    {
        var termYear = term / 100;
        var termPart = term % 100;
        bool inRange;

        if (useAcademicYear)
        {
            var start = startYear * 100 + 4;
            var end = endYear * 100 + 3;
            inRange = term >= start && term <= end;
        }
        else
        {
            inRange = termYear >= startYear && termYear <= endYear;
        }

        if (inRange && termPart is 1 or 3 or 8 or 10)
        {
            return 0.25m;
        }

        return 0m;
    }

    // ── Merit Section Building ────────────────────────────────────────

    private static MultiYearMeritSection BuildMeritSection(
        List<MeritMultiyearRow> rows,
        List<string> effortTypes,
        bool useAcademicYear)
    {
        if (rows.Count == 0)
        {
            return new MultiYearMeritSection();
        }

        // Group rows by year (academic or calendar)
        var yearGroups = rows
            .GroupBy(r => useAcademicYear
                ? GetAcademicYearFromTerm(r.TermCode)
                : r.TermCode / 100)
            .OrderBy(g => g.Key)
            .ToList();

        var years = yearGroups.Select(yg =>
        {
            var yearCourses = yg
                .GroupBy(r => new { r.TermCode, r.CourseId, r.Course, r.RoleId })
                .OrderBy(g => g.Key.TermCode)
                .ThenBy(g => g.Key.Course)
                .Select(cg =>
                {
                    var efforts = new Dictionary<string, decimal>();
                    foreach (var row in cg.Where(r => !string.IsNullOrWhiteSpace(r.EffortTypeId) && r.Effort > 0))
                    {
                        var key = row.EffortTypeId.Trim();
                        efforts[key] = efforts.GetValueOrDefault(key) + row.Effort;
                    }

                    var first = cg.First();
                    return new MultiYearCourseRow
                    {
                        Course = first.Course.Trim(),
                        TermCode = first.TermCode,
                        Units = first.Units,
                        Enrollment = first.Enrollment,
                        Role = first.RoleId.Trim(),
                        Efforts = efforts
                    };
                })
                .ToList();

            // Year totals
            var yearTotals = new Dictionary<string, decimal>();
            foreach (var effortType in effortTypes)
            {
                var total = yearCourses.Sum(c => c.Efforts.GetValueOrDefault(effortType));
                if (total > 0)
                {
                    yearTotals[effortType] = total;
                }
            }

            return new MultiYearMeritYear
            {
                Year = yg.Key,
                YearLabel = yg.Key.ToString(),
                Courses = yearCourses,
                YearTotals = yearTotals
            };
        }).ToList();

        // Grand totals across all years
        var grandTotals = new Dictionary<string, decimal>();
        foreach (var effortType in effortTypes)
        {
            var total = years.Sum(y => y.YearTotals.GetValueOrDefault(effortType));
            if (total > 0)
            {
                grandTotals[effortType] = total;
            }
        }

        // Yearly averages are computed by the caller (needs excluded terms context)

        return new MultiYearMeritSection
        {
            Years = years,
            GrandTotals = grandTotals
        };
    }

    // ── Eval Section Building ─────────────────────────────────────────

    private static MultiYearEvalSection BuildEvalSection(
        List<EvalsMultiyearRow> rows,
        bool useAcademicYear)
    {
        if (rows.Count == 0)
        {
            return new MultiYearEvalSection();
        }

        // Filter to instructor evals only (EvalId == 0); facilitator evals are separate
        var instructorRows = rows.Where(r => r.EvalId == 0).ToList();

        // Group by year
        var yearGroups = instructorRows
            .GroupBy(r => useAcademicYear ? r.AcademicYear : r.CalendarYear)
            .OrderBy(g => g.Key)
            .ToList();

        var years = yearGroups.Select(yg =>
        {
            var courses = yg
                .OrderBy(r => r.TermCode)
                .ThenBy(r => r.Course)
                .Select(r => new MultiYearEvalCourse
                {
                    Course = r.Course.Trim(),
                    Crn = r.Crn.Trim(),
                    TermCode = r.TermCode,
                    Role = r.Role.ToString(),
                    Average = r.Average,
                    Median = CalculateMedian(r.N1, r.N2, r.N3, r.N4, r.N5),
                    NumResponses = r.NumResponses,
                    NumEnrolled = r.NumEnrolled
                })
                .ToList();

            // Year average (weighted by responses)
            var totalResponses = courses.Sum(c => c.NumResponses);
            var yearAverage = totalResponses > 0
                ? Math.Round(courses.Sum(c => c.Average * c.NumResponses) / totalResponses, 2)
                : 0;

            // Year median from all course medians
            var validMedians = courses
                .Where(c => c.Median.HasValue)
                .Select(c => c.Median!.Value)
                .OrderBy(m => m)
                .ToList();
            decimal? yearMedian = validMedians.Count > 0
                ? CalculateListMedian(validMedians)
                : null;

            return new MultiYearEvalYear
            {
                Year = yg.Key,
                YearLabel = yg.Key.ToString(),
                Courses = courses,
                YearAverage = yearAverage,
                YearMedian = yearMedian
            };
        }).ToList();

        // Overall average (weighted)
        var allResponses = years.Sum(y => y.Courses.Sum(c => c.NumResponses));
        var overallAverage = allResponses > 0
            ? Math.Round(years.Sum(y => y.Courses.Sum(c => c.Average * c.NumResponses)) / allResponses, 2)
            : 0;

        // Overall median
        var allMedians = years
            .SelectMany(y => y.Courses)
            .Where(c => c.Median.HasValue)
            .Select(c => c.Median!.Value)
            .OrderBy(m => m)
            .ToList();
        decimal? overallMedian = allMedians.Count > 0
            ? CalculateListMedian(allMedians)
            : null;

        return new MultiYearEvalSection
        {
            Years = years,
            OverallAverage = overallAverage,
            OverallMedian = overallMedian
        };
    }

    // ── Department Averages ───────────────────────────────────────────

    private async Task PopulateDepartmentAveragesAsync(
        MultiYearReport report,
        string mothraId,
        string department,
        string jobGroupDesc,
        int startYear, int endYear,
        bool useAcademicYear,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(department) || report.EffortTypes.Count == 0)
        {
            return;
        }

        // Sum faculty count across years (matching legacy getDeptCountMultiYear)
        var deptFacultyCount = await GetDeptFacultyCountMultiYearAsync(
            department, jobGroupDesc, startYear, endYear, useAcademicYear, ct);

        if (deptFacultyCount <= 0)
        {
            return;
        }

        var deptAverages = new Dictionary<string, decimal>();

        // For each effort type, get department total via sp_dept_activity_total_exclude
        // Legacy passes empty excludedTerms for dept averages (not the instructor's sabbatical exclusions)
        // Legacy excludes CLI from department averages (shows blank in report_multiyear.cfm)
        foreach (var effortType in report.EffortTypes)
        {
            if (effortType is "CLI" or "CLIH" or "CLIW")
            {
                continue;
            }

            // Legacy getDeptTotalForActivity defaults useAcademicYear=false and CF never passes it,
            // so dept averages always use calendar year term ranges regardless of the report setting
            var total = await ExecuteActivityExcludeForDeptAsync(
                mothraId, startYear, endYear, effortType, null, useAcademicYear: false, ct);

            if (total > 0)
            {
                // Legacy divides total by summed faculty count (not per-year)
                deptAverages[effortType] = Math.Round(total / deptFacultyCount, 1);
            }
        }

        report.MeritSection.DepartmentAverages = deptAverages;
        report.MeritSection.DepartmentFacultyCount = deptFacultyCount;
    }

    private async Task PopulateEvalDepartmentAverageAsync(
        MultiYearReport report,
        string evalDept,
        int startYear, int endYear,
        string? excludeDidacticTerms,
        bool useAcademicYear,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(evalDept))
        {
            return;
        }

        // Use sp_instructor_evals_average_exclude to compute dept eval average in a single query
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        int startTerm, endTerm;
        if (useAcademicYear)
        {
            startTerm = startYear * 100 + 4;
            endTerm = endYear * 100 + 3;
        }
        else
        {
            startTerm = startYear * 100 + 1;
            endTerm = endYear * 100 + 10;
        }

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new Microsoft.Data.SqlClient.SqlCommand("[effort].[sp_instructor_evals_average_exclude]", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = 30;
        command.Parameters.AddWithValue("@StartTerm", startTerm);
        command.Parameters.AddWithValue("@EndTerm", endTerm);
        command.Parameters.AddWithValue("@Dept", evalDept);
        command.Parameters.AddWithValue("@MothraId", DBNull.Value);
        command.Parameters.AddWithValue("@ExcludedTerms", (object?)excludeDidacticTerms ?? DBNull.Value);
        command.Parameters.AddWithValue("@FacilitatorEvals", false);

        await using var reader = await command.ExecuteReaderAsync(ct);
        // SP returns: avgEval(0), minEval(1), maxEval(2)
        if (await reader.ReadAsync(ct) && !await reader.IsDBNullAsync(0, ct))
        {
            report.EvalSection.DepartmentAverage = Math.Round(Convert.ToDecimal(reader.GetValue(0)), 2);
        }
    }

    // ── SP Execution: sp_merit_multiyear ──────────────────────────────

    private sealed class MeritMultiyearRow
    {
        public int TermCode { get; set; }
        public string MothraId { get; set; } = string.Empty;
        public string Instructor { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string Course { get; set; } = string.Empty;
        public decimal Units { get; set; }
        public int Enrollment { get; set; }
        public string RoleId { get; set; } = string.Empty;
        public string EffortTypeId { get; set; } = string.Empty;
        public decimal Effort { get; set; }
    }

    private async Task<List<MeritMultiyearRow>> ExecuteMeritMultiyearSpAsync(
        int personId, int startTermCode, int endTermCode,
        string? excludeClinicalTerms, string? excludeDidacticTerms,
        CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        var results = new List<MeritMultiyearRow>();

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new Microsoft.Data.SqlClient.SqlCommand("[effort].[sp_merit_multiyear]", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@PersonId", personId);
        command.Parameters.AddWithValue("@StartTermCode", startTermCode);
        command.Parameters.AddWithValue("@EndTermCode", endTermCode);
        command.Parameters.AddWithValue("@ExcludeClinicalTerms", (object?)excludeClinicalTerms ?? DBNull.Value);
        command.Parameters.AddWithValue("@ExcludeDidacticTerms", (object?)excludeDidacticTerms ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            // SP columns: TermCode, MothraId, Instructor, Department, CourseId, Course, Units, Enrollment, RoleId, EffortTypeId, Effort
            results.Add(new MeritMultiyearRow
            {
                TermCode = reader.GetInt32(0),
                MothraId = reader.GetString(1),
                Instructor = reader.GetString(2),
                Department = await reader.IsDBNullAsync(3, ct) ? "" : reader.GetString(3),
                CourseId = reader.GetInt32(4),
                Course = reader.GetString(5),
                Units = await reader.IsDBNullAsync(6, ct) ? 0m : Convert.ToDecimal(reader.GetValue(6)),
                Enrollment = await reader.IsDBNullAsync(7, ct) ? 0 : reader.GetInt32(7),
                RoleId = await reader.IsDBNullAsync(8, ct) ? "" : reader.GetInt32(8).ToString(),
                EffortTypeId = await reader.IsDBNullAsync(9, ct) ? "" : reader.GetString(9),
                Effort = await reader.IsDBNullAsync(10, ct) ? 0m : Convert.ToDecimal(reader.GetValue(10))
            });
        }

        return results;
    }

    // ── SP Execution: sp_instructor_evals_multiyear ───────────────────

    private sealed class EvalsMultiyearRow
    {
        public string TermName { get; set; } = string.Empty;
        public int TermCode { get; set; }
        public int AcademicYear { get; set; }
        public int CalendarYear { get; set; }
        public string MothraId { get; set; } = string.Empty;
        public string? Dept { get; set; }
        public string Instructor { get; set; } = string.Empty;
        public string Course { get; set; } = string.Empty;
        public int Role { get; set; }
        public decimal Average { get; set; }
        public int N5 { get; set; }
        public int N4 { get; set; }
        public int N3 { get; set; }
        public int N2 { get; set; }
        public int N1 { get; set; }
        public string Crn { get; set; } = string.Empty;
        public int NumEnrolled { get; set; }
        public int NumResponses { get; set; }
        public int EvalId { get; set; }
    }

    private async Task<List<EvalsMultiyearRow>> ExecuteEvalsMultiyearSpAsync(
        int startYear, int endYear, string? mothraId,
        string? excludedTerms, bool useAcademicYear,
        CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        var results = new List<EvalsMultiyearRow>();

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new Microsoft.Data.SqlClient.SqlCommand("[effort].[sp_instructor_evals_multiyear]", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@StartYear", startYear);
        command.Parameters.AddWithValue("@EndYear", endYear);
        command.Parameters.AddWithValue("@MothraId", (object?)mothraId ?? DBNull.Value);
        command.Parameters.AddWithValue("@ExcludedTerms", (object?)excludedTerms ?? DBNull.Value);
        command.Parameters.AddWithValue("@UseAcademicYear", useAcademicYear);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            // SP columns: TermName(0), TermCode(1), AcademicYear(2), CalendarYear(3), MothraID(4),
            //   Dept(5), Instructor(6), Course(7), Role(8), Average(9), n5(10), n4(11), n3(12), n2(13), n1(14),
            //   CRN(15), SubjCode(16), CourseNum(17), CourseKey(18), ReportUnit(19),
            //   NumEnrolled(20), OriginalNumEnrolled(21), NumResponses(22), evalid(23)
            // SP returns RIGHT(term_academic_year, 4) — the end year, e.g. "2022"
            var academicYearStr = await reader.IsDBNullAsync(2, ct) ? "" : reader.GetString(2);
            int.TryParse(academicYearStr, out var academicYearInt);

            results.Add(new EvalsMultiyearRow
            {
                TermName = await reader.IsDBNullAsync(0, ct) ? "" : reader.GetString(0),
                TermCode = reader.GetInt32(1),
                AcademicYear = academicYearInt,
                CalendarYear = reader.GetInt32(3),
                MothraId = await reader.IsDBNullAsync(4, ct) ? "" : reader.GetString(4),
                Dept = await reader.IsDBNullAsync(5, ct) ? null : reader.GetString(5),
                Instructor = await reader.IsDBNullAsync(6, ct) ? "" : reader.GetString(6),
                Course = await reader.IsDBNullAsync(7, ct) ? "" : reader.GetString(7),
                Role = await reader.IsDBNullAsync(8, ct) ? 2 : reader.GetInt32(8),
                Average = await reader.IsDBNullAsync(9, ct) ? 0m : Convert.ToDecimal(reader.GetValue(9)),
                N5 = await reader.IsDBNullAsync(10, ct) ? 0 : reader.GetInt32(10),
                N4 = await reader.IsDBNullAsync(11, ct) ? 0 : reader.GetInt32(11),
                N3 = await reader.IsDBNullAsync(12, ct) ? 0 : reader.GetInt32(12),
                N2 = await reader.IsDBNullAsync(13, ct) ? 0 : reader.GetInt32(13),
                N1 = await reader.IsDBNullAsync(14, ct) ? 0 : reader.GetInt32(14),
                Crn = await reader.IsDBNullAsync(15, ct) ? "" : reader.GetInt32(15).ToString(),
                NumEnrolled = await reader.IsDBNullAsync(20, ct) ? 0 : reader.GetInt32(20),
                NumResponses = await reader.IsDBNullAsync(22, ct) ? 0 : reader.GetInt32(22),
                EvalId = await reader.IsDBNullAsync(23, ct) ? 0 : reader.GetInt32(23)
            });
        }

        return results;
    }

    /// <summary>
    /// Get department-wide activity total for an effort type using sp_dept_activity_total_exclude.
    /// Single SP call per effort type (matching legacy performance).
    /// </summary>
    private async Task<decimal> ExecuteActivityExcludeForDeptAsync(
        string mothraId, int startYear, int endYear, string effortType,
        string? excludedTerms, bool useAcademicYear,
        CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new Microsoft.Data.SqlClient.SqlCommand("[effort].[sp_dept_activity_total_exclude]", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = 30;
        command.Parameters.AddWithValue("@MothraId", mothraId);
        command.Parameters.AddWithValue("@YearStart", startYear);
        command.Parameters.AddWithValue("@YearEnd", endYear);
        command.Parameters.AddWithValue("@Activity", effortType);
        command.Parameters.AddWithValue("@ExcludedTerms", (object?)excludedTerms ?? DBNull.Value);
        command.Parameters.AddWithValue("@AllDepts", false);
        command.Parameters.AddWithValue("@UseAcademicYear", useAcademicYear);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            // SP returns: Hours(0), Dept(1), JGDDesc(2)
            return await reader.IsDBNullAsync(0, ct) ? 0m : Convert.ToDecimal(reader.GetValue(0));
        }

        return 0m;
    }

    // ── SP Execution: sp_dept_job_group_count ─────────────────────────

    /// <summary>
    /// Sum faculty count across years using sp_dept_count_by_job_group_exclude.
    /// Matches legacy getDeptCountMultiYear which loops year by year.
    /// </summary>
    private async Task<int> GetDeptFacultyCountMultiYearAsync(
        string namedDept, string jobGroupDesc,
        int startYear, int endYear,
        bool useAcademicYear,
        CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        // endYear is already decremented by the caller (GenerateMultiYearReportAsync),
        // matching legacy's loop bound of `startYear to endYear - 1`
        var totalCount = 0;

        for (var year = startYear; year <= endYear; year++)
        {
            var yearParam = useAcademicYear ? $"{year}-{year + 1}" : year.ToString();

            await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new Microsoft.Data.SqlClient.SqlCommand(
                "[effort].[sp_dept_count_by_job_group_exclude]", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 30;
            command.Parameters.AddWithValue("@Year", yearParam);
            command.Parameters.AddWithValue("@Dept", namedDept);
            command.Parameters.AddWithValue("@JobGroupDesc", jobGroupDesc);
            command.Parameters.AddWithValue("@ExcludedTerms", DBNull.Value);
            command.Parameters.AddWithValue("@FilterOutNoCLIAssigned", false);
            command.Parameters.AddWithValue("@UseAcademicYear", useAcademicYear);

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                totalCount += await reader.IsDBNullAsync(0, ct) ? 0 : reader.GetInt32(0);
            }
        }

        return totalCount;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static int GetAcademicYearFromTerm(int termCode)
    {
        int year = termCode / 100;
        int term = termCode % 100;
        return (term >= 1 && term <= 3) ? year - 1 : year;
    }

    private static List<string> ParseTermList(string? terms)
    {
        if (string.IsNullOrWhiteSpace(terms))
        {
            return [];
        }

        return terms.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    /// <summary>
    /// Filter term code strings to only those within the report's term range.
    /// </summary>
    private static List<string> FilterTermsInRange(List<string> terms, int startTermCode, int endTermCode)
    {
        return terms
            .Where(t => int.TryParse(t, out var code) && code >= startTermCode && code <= endTermCode)
            .ToList();
    }

    /// <summary>
    /// Calculate median from n1-n5 response counts (1=worst, 5=best).
    /// Returns null if no responses.
    /// </summary>
    private static decimal? CalculateMedian(int n1, int n2, int n3, int n4, int n5)
    {
        var total = n1 + n2 + n3 + n4 + n5;
        if (total == 0) return null;

        // Build sorted response list
        var responses = new List<decimal>();
        for (var i = 0; i < n1; i++) responses.Add(1);
        for (var i = 0; i < n2; i++) responses.Add(2);
        for (var i = 0; i < n3; i++) responses.Add(3);
        for (var i = 0; i < n4; i++) responses.Add(4);
        for (var i = 0; i < n5; i++) responses.Add(5);

        return CalculateListMedian(responses);
    }

    private static decimal CalculateListMedian(List<decimal> sorted)
    {
        var count = sorted.Count;
        if (count == 0) return 0;

        if (count % 2 == 0)
        {
            return Math.Round((sorted[count / 2 - 1] + sorted[count / 2]) / 2, 2);
        }
        return sorted[count / 2];
    }

    // ============================================
    // PDF Generation
    // ============================================

    public Task<byte[]> GenerateReportPdfAsync(MultiYearReport report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);
        var compact = orderedTypes.Count > 10;
        var fontSize = compact ? 8f : 9f;
        var headerFontSize = compact ? 7f : 8f;
        var cellPadV = compact ? 1.5f : 2f;
        var effortWidth = compact ? 28f : 36f;

        var yearLabel = report.UseAcademicYear
            ? $"{report.StartYear}-{report.StartYear + 1} to {report.EndYear}-{report.EndYear + 1}"
            : $"{report.StartYear} to {report.EndYear}";

        var document = Document.Create(container =>
        {
            // Page 1: Merit Activity
            container.Page(page =>
            {
                page.Size(PageSizes.Letter.Landscape());
                page.MarginHorizontal(0.5f, Unit.Inch);
                page.MarginVertical(0.3f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontSize(fontSize));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("UCD School of Veterinary Medicine").Bold().FontSize(11);
                        row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("d MMMM yyyy")).Bold().FontSize(11);
                    });
                    col.Item().PaddingVertical(4).Text("Multi-Year Merit Activity Report").SemiBold().FontSize(12);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"{report.Instructor} ({report.Department})").SemiBold().FontSize(10);
                        row.RelativeItem().AlignRight().Text(yearLabel).SemiBold().FontSize(10);
                    });
                });

                page.Content().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(50);    // Qtr
                        columns.ConstantColumn(30);    // Role
                        columns.ConstantColumn(50);    // Enrolled
                        columns.ConstantColumn(40);    // Units
                        columns.ConstantColumn(160);   // Course
                        foreach (var _ in orderedTypes)
                        {
                            columns.ConstantColumn(effortWidth);
                        }
                    });

                    var hdrStyle = TextStyle.Default.FontSize(headerFontSize).Bold().Underline();

                    foreach (var year in report.MeritSection.Years)
                    {
                        // Year header (above column headers, matching legacy)
                        var yearColSpan = (uint)(5 + orderedTypes.Count);
                        table.Cell().ColumnSpan(yearColSpan).Background("#E8E8E8")
                            .PaddingVertical(cellPadV).PaddingLeft(4)
                            .Text($"Year {year.YearLabel}").Bold();

                        // Column headers (repeated per year, matching legacy)
                        table.Cell().PaddingVertical(cellPadV).Text("Qtr").Style(hdrStyle);
                        table.Cell().PaddingVertical(cellPadV).Text("Role").Style(hdrStyle);
                        table.Cell().PaddingVertical(cellPadV).Text("Enrolled").Style(hdrStyle);
                        table.Cell().PaddingVertical(cellPadV).Text("Units").Style(hdrStyle);
                        table.Cell().PaddingVertical(cellPadV).Text("Course").Style(hdrStyle);
                        foreach (var type in orderedTypes)
                        {
                            table.Cell().PaddingVertical(cellPadV).Text(type).Style(hdrStyle);
                        }

                        foreach (var course in year.Courses)
                        {
                            table.Cell().PaddingVertical(cellPadV).Text(course.TermCode.ToString());
                            table.Cell().PaddingVertical(cellPadV).Text(course.Role);
                            table.Cell().PaddingVertical(cellPadV).Text(course.Enrollment.ToString());
                            table.Cell().PaddingVertical(cellPadV).Text(course.Units.ToString("F0"));
                            table.Cell().PaddingVertical(cellPadV).Text(course.Course);
                            foreach (var type in orderedTypes)
                            {
                                var val = course.Efforts.GetValueOrDefault(type);
                                table.Cell().PaddingVertical(cellPadV)
                                    .Text(val > 0 ? val.ToString() : "0");
                            }
                        }

                        // Year totals
                        table.Cell().ColumnSpan(5).PaddingVertical(cellPadV).AlignRight().PaddingRight(8)
                            .Text($"Total for {year.YearLabel}").Bold().Italic();
                        foreach (var type in orderedTypes)
                        {
                            var val = year.YearTotals.GetValueOrDefault(type);
                            table.Cell().PaddingVertical(cellPadV)
                                .Text(val > 0 ? val.ToString() : "0").Bold();
                        }
                    }

                    // Instructor total
                    table.Cell().ColumnSpan(5).BorderTop(1.5f).BorderColor("#666666")
                        .Background("#E0E0E0").PaddingVertical(cellPadV).AlignRight().PaddingRight(8)
                        .Text($"{report.Instructor} Total").Bold();
                    foreach (var type in orderedTypes)
                    {
                        var val = report.MeritSection.GrandTotals.GetValueOrDefault(type);
                        table.Cell().BorderTop(1.5f).BorderColor("#666666")
                            .Background("#E0E0E0").PaddingVertical(cellPadV)
                            .Text(val > 0 ? val.ToString() : "0").Bold();
                    }

                    // Yearly averages
                    table.Cell().ColumnSpan(5).PaddingVertical(cellPadV).AlignRight().PaddingRight(8)
                        .Text($"* Yearly didactic average (actual clinical) over {report.StartYear} - {report.EndYear}").Italic();
                    foreach (var type in orderedTypes)
                    {
                        var val = report.MeritSection.YearlyAverages.GetValueOrDefault(type);
                        table.Cell().PaddingVertical(cellPadV)
                            .Text(val > 0 ? val.ToString("F1") : "0.0");
                    }

                    // Department averages
                    if (report.MeritSection.DepartmentAverages.Count > 0)
                    {
                        table.Cell().ColumnSpan(5).PaddingVertical(cellPadV).AlignRight().PaddingRight(8)
                            .Text($"{report.Department} Department Average over {report.StartYear} - {report.EndYear}"
                                + (report.MeritSection.DepartmentFacultyCount > 0
                                    ? $" with {report.MeritSection.DepartmentFacultyCount} Faculty"
                                    : "")).Italic();
                        foreach (var type in orderedTypes)
                        {
                            var val = report.MeritSection.DepartmentAverages.GetValueOrDefault(type);
                            table.Cell().PaddingVertical(cellPadV)
                                .Text(val > 0 ? val.ToString("F1") : "0.0");
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });

            // Page 2: Evaluations
            if (report.EvalSection.Years.Count > 0)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter.Landscape());
                    page.MarginHorizontal(0.5f, Unit.Inch);
                    page.MarginVertical(0.3f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(fontSize));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("UCD School of Veterinary Medicine").Bold().FontSize(11);
                            row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("d MMMM yyyy")).Bold().FontSize(11);
                        });
                        col.Item().PaddingVertical(4).Text("Multi-Year Evaluation Report").SemiBold().FontSize(12);
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"{report.Instructor} ({report.Department})").SemiBold().FontSize(10);
                            row.RelativeItem().AlignRight().Text(yearLabel).SemiBold().FontSize(10);
                        });
                    });

                    page.Content().PaddingTop(8).Column(contentCol =>
                    {
                        contentCol.Item().Text("Instructor").Bold();
                        contentCol.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(50);      // Term
                                columns.ConstantColumn(30);      // Role
                                columns.ConstantColumn(55);      // # Enrolled
                                columns.ConstantColumn(60);      // % Response
                                columns.RelativeColumn();        // Course
                                columns.ConstantColumn(50);      // Average
                                columns.ConstantColumn(50);      // Median
                            });

                            foreach (var year in report.EvalSection.Years)
                            {
                                // Year header
                                var yearHeaderText = report.UseAcademicYear
                                    ? $"Academic Year {year.YearLabel}"
                                    : year.YearLabel;
                                table.Cell().ColumnSpan(7).Background("#E8E8E8")
                                    .PaddingVertical(cellPadV).PaddingLeft(4)
                                    .Text(yearHeaderText).Bold();

                                // Column headers per year
                                var hdrStyle = TextStyle.Default.FontSize(headerFontSize).Bold().Underline();
                                table.Cell().PaddingVertical(cellPadV).Text("Term").Style(hdrStyle);
                                table.Cell().PaddingVertical(cellPadV).Text("Role").Style(hdrStyle);
                                table.Cell().PaddingVertical(cellPadV).Text("# Enrolled").Style(hdrStyle);
                                table.Cell().PaddingVertical(cellPadV).Text("% Response").Style(hdrStyle);
                                table.Cell().PaddingVertical(cellPadV).Text("Course").Style(hdrStyle);
                                table.Cell().PaddingVertical(cellPadV).Text("Average").Style(hdrStyle);
                                table.Cell().PaddingVertical(cellPadV).Text("Median").Style(hdrStyle);

                                foreach (var course in year.Courses)
                                {
                                    table.Cell().PaddingVertical(cellPadV).Text(course.TermCode.ToString());
                                    table.Cell().PaddingVertical(cellPadV).Text(course.Role);
                                    table.Cell().PaddingVertical(cellPadV).Text(course.NumEnrolled.ToString());
                                    var pctResponse = course.NumEnrolled > 0
                                        ? ((double)course.NumResponses / course.NumEnrolled * 100).ToString("F1") + "%"
                                        : "0.0%";
                                    table.Cell().PaddingVertical(cellPadV).Text(pctResponse);
                                    table.Cell().PaddingVertical(cellPadV).Text(course.Course);
                                    table.Cell().PaddingVertical(cellPadV).Text(course.Average.ToString("F2"));
                                    table.Cell().PaddingVertical(cellPadV)
                                        .Text(course.Median.HasValue ? Math.Round(course.Median.Value).ToString() : "");
                                }

                                // Year average (value only in Average column)
                                table.Cell().ColumnSpan(5).PaddingVertical(cellPadV).AlignRight().PaddingRight(4)
                                    .Text($"Average for {year.YearLabel}").Bold().Italic();
                                table.Cell().PaddingVertical(cellPadV)
                                    .Text(year.YearAverage.ToString("F2")).Bold();
                                table.Cell(); // empty median
                            }

                            // Overall average (value only in Average column)
                            table.Cell().ColumnSpan(5).BorderTop(1.5f).BorderColor("#666666")
                                .Background("#E0E0E0").PaddingVertical(cellPadV).AlignRight().PaddingRight(4)
                                .Text("Overall Average:").Bold();
                            table.Cell().BorderTop(1.5f).BorderColor("#666666")
                                .Background("#E0E0E0").PaddingVertical(cellPadV)
                                .Text(report.EvalSection.OverallAverage.ToString("F2")).Bold();
                            table.Cell().BorderTop(1.5f).BorderColor("#666666")
                                .Background("#E0E0E0"); // empty median

                            // Department average
                            if (report.EvalSection.DepartmentAverage.HasValue)
                            {
                                table.Cell().ColumnSpan(5).PaddingVertical(cellPadV).AlignRight().PaddingRight(4)
                                    .Text($"{report.Department} Department Average for {report.StartYear} - {report.EndYear}").Italic();
                                table.Cell().PaddingVertical(cellPadV)
                                    .Text(report.EvalSection.DepartmentAverage.Value.ToString("F2"));
                                table.Cell(); // empty median
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }
            else
            {
                // No per-course eval data — render summary page matching HTML fallback
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter.Landscape());
                    page.MarginHorizontal(0.5f, Unit.Inch);
                    page.MarginVertical(0.3f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(fontSize));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("UCD School of Veterinary Medicine").Bold().FontSize(11);
                            row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("d MMMM yyyy")).Bold().FontSize(11);
                        });
                        col.Item().PaddingVertical(4).Text("Multi-Year Evaluation Report").SemiBold().FontSize(12);
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"{report.Instructor} ({report.Department})").SemiBold().FontSize(10);
                            row.RelativeItem().AlignRight().Text(yearLabel).SemiBold().FontSize(10);
                        });
                    });

                    page.Content().PaddingTop(8).Column(col =>
                    {
                        col.Item().Text($"Evaluation Multi-Year \u2014 {report.Instructor}").SemiBold().FontSize(11);
                        col.Item().PaddingTop(6).Text("Instructor").Bold();
                        // Dept average label is the longest — use it to size the label column
                        var deptLabel = report.EvalSection.DepartmentAverage.HasValue
                            ? $"{report.Department} Department Average for {report.StartYear} - {report.EndYear}:"
                            : null;

                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.AutoItem().Column(labelCol =>
                            {
                                labelCol.Item().AlignRight().Text("Overall Average:").Bold().Italic();
                                if (deptLabel != null)
                                    labelCol.Item().AlignRight().Text(deptLabel).Bold().Italic();
                            });
                            row.AutoItem().PaddingLeft(4).Column(valCol =>
                            {
                                valCol.Item().Text(report.EvalSection.OverallAverage.ToString("F2")).Bold();
                                if (report.EvalSection.DepartmentAverage.HasValue)
                                    valCol.Item().Text(report.EvalSection.DepartmentAverage.Value.ToString("F2")).Bold();
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }
        });

        return Task.FromResult(document.GeneratePdf());
    }

    // ============================================
    // Year Range
    // ============================================

    public async Task<InstructorYearRangeDto?> GetInstructorYearRangeAsync(int personId, CancellationToken ct = default)
    {
        var termCodes = await _context.Persons
            .AsNoTracking()
            .Where(p => p.PersonId == personId)
            .Select(p => p.TermCode)
            .ToListAsync(ct);

        if (termCodes.Count == 0)
        {
            return null;
        }

        return new InstructorYearRangeDto
        {
            MinYear = termCodes.Min() / 100,
            MaxYear = termCodes.Max() / 100,
        };
    }
}
