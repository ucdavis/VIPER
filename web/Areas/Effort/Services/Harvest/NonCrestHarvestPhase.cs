using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services.Harvest;

/// <summary>
/// Phase 2: Non-CREST course import.
/// Imports courses from Banner (courses.dbo.baseinfo) that are not in CREST.
/// </summary>
public sealed class NonCrestHarvestPhase : HarvestPhaseBase
{
    /// <inheritdoc />
    public override string PhaseName => "Non-CREST";

    /// <inheritdoc />
    public override int Order => 20;

    /// <inheritdoc />
    public override async Task GeneratePreviewAsync(HarvestContext context, CancellationToken ct = default)
    {
        var termCodeStr = context.TermCode.ToString();

        // Get (CRN, Units) pairs already in CREST (from Phase 1). Filtering by
        // CRN alone would drop other unit variants of variable-unit CRNs that
        // CREST only sees at one unit value.
        var existingCrnUnits = context.Preview.CrestCourses
            .Select(c => (c.Crn, c.Units))
            .ToHashSet();

        // Get courses with enrollment from roster (excluding DVM courses which are clinical)
        var coursesWithEnrollment = await context.CoursesContext.Rosters
            .AsNoTracking()
            .Where(r => r.RosterTermCode == termCodeStr && r.RosterUnit > 0 && r.RosterCrn != null)
            .Join(
                context.CoursesContext.Baseinfos.AsNoTracking(),
                r => new { TermCode = r.RosterTermCode!, Crn = r.RosterCrn! },
                b => new { TermCode = b.BaseinfoTermCode, Crn = b.BaseinfoCrn },
                (r, b) => new { Roster = r, Baseinfo = b })
            .Where(x => x.Baseinfo.BaseinfoSubjCode != "DVM")
            .GroupBy(x => new
            {
                x.Baseinfo.BaseinfoTermCode,
                x.Baseinfo.BaseinfoCrn,
                x.Roster.RosterUnit,
                x.Baseinfo.BaseinfoSubjCode,
                x.Baseinfo.BaseinfoCrseNumb,
                x.Baseinfo.BaseinfoSeqNumb,
                x.Baseinfo.BaseinfoDeptCode
            })
            .Select(g => new
            {
                Crn = g.Key.BaseinfoCrn ?? "",
                SubjCode = g.Key.BaseinfoSubjCode ?? "",
                CrseNumb = g.Key.BaseinfoCrseNumb ?? "",
                SeqNumb = g.Key.BaseinfoSeqNumb ?? "001",
                Units = g.Key.RosterUnit ?? 0,
                Enrollment = g.Count(),
                DeptCode = g.Key.BaseinfoDeptCode ?? ""
            })
            .ToListAsync(ct);

        // Get research courses (course number ending in 'R') - these may have 0 enrollment
        var researchCourses = await context.CoursesContext.Baseinfos
            .AsNoTracking()
            .Where(b => b.BaseinfoTermCode == termCodeStr &&
                        b.BaseinfoCrseNumb != null && b.BaseinfoCrseNumb.EndsWith('R'))
            .Select(b => new
            {
                Crn = b.BaseinfoCrn,
                SubjCode = b.BaseinfoSubjCode,
                CrseNumb = b.BaseinfoCrseNumb,
                SeqNumb = b.BaseinfoSeqNumb,
                Units = (double)b.BaseinfoUnitLow,
                Enrollment = 0,
                DeptCode = b.BaseinfoDeptCode
            })
            .ToListAsync(ct);

        // Preserve one row per (Crn, Units) so the preview surfaces all enrollment variants
        // of a variable-unit CRN. The CRN-keyed courseLookup below dedupes separately.
        var allScheduleCourses = coursesWithEnrollment
            .Concat(researchCourses)
            .DistinctBy(c => new { c.Crn, c.Units })
            .ToList();

        // Add courses not in CREST (these will be imported)
        var allNonCrestCourses = allScheduleCourses
            .Where(c => !existingCrnUnits.Contains((c.Crn, (decimal)c.Units)))
            .ToList();

        // Map each CRN to its IOR-resolved custodial department code from the vw_xtnd_baseinfo view.
        // The view derives that code from the course POA (instructor of record) when the baseinfo
        // dept is not an SVM academic department. The legacy harvest relied on it; without it,
        // non-SVM-subject courses such as IMM 294 resolve to "UNK".
        // Only the scheduled CRNs are ever looked up, so filter the registrar-wide view to them
        // instead of loading every row for the term. BaseinfoCrn is CHAR(5): compare the column bare
        // (keeping the predicate SARGable) and pass trimmed CRNs, relying on SQL Server's
        // trailing-space-insensitive IN to match the padded column.
        var scheduleCrns = allScheduleCourses.Select(c => c.Crn.Trim()).Distinct().ToList();
        var custodialByCrn = (await context.CoursesContext.VwXtndBaseinfos
                .AsNoTracking()
                .Where(v => v.BaseinfoTermCode == termCodeStr
                            && v.CustodialDeptCode != null
                            && EF.Parameter(scheduleCrns).Contains(v.BaseinfoCrn))
                .Select(v => new { v.BaseinfoCrn, v.CustodialDeptCode })
                .ToListAsync(ct))
            .GroupBy(v => v.BaseinfoCrn.Trim())
            // OrderBy makes the pick deterministic when a CRN has multiple instructors (POAs)
            // whose resolved custodial_dept_code differs; an unordered First() would be arbitrary.
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.CustodialDeptCode).First().CustodialDeptCode, StringComparer.OrdinalIgnoreCase);

        // Layer the IOR-resolved custodial code from the view on top of subject/dept resolution.
        // Shared by the not-in-CREST and in-CREST preview loops below.
        string ResolveCustDept(string subjCode, string deptCode, string crn) =>
            CustodialDepartmentResolver.ResolveWithCustodialCode(
                subjCode, deptCode, custodialByCrn.GetValueOrDefault(crn.Trim()));

        foreach (var course in allNonCrestCourses)
        {
            context.Preview.NonCrestCourses.Add(new HarvestCoursePreview
            {
                Crn = course.Crn,
                SubjCode = course.SubjCode,
                CrseNumb = course.CrseNumb,
                SeqNumb = course.SeqNumb,
                Enrollment = course.Enrollment,
                Units = (decimal)course.Units,
                CustDept = ResolveCustDept(course.SubjCode, course.DeptCode, course.Crn),
                Source = EffortConstants.SourceNonCrest
            });
        }

        // Add courses that ARE in CREST (for transparency)
        var crestDuplicates = allScheduleCourses
            .Where(c => existingCrnUnits.Contains((c.Crn, (decimal)c.Units)))
            .ToList();

        foreach (var course in crestDuplicates)
        {
            context.Preview.NonCrestCourses.Add(new HarvestCoursePreview
            {
                Crn = course.Crn,
                SubjCode = course.SubjCode,
                CrseNumb = course.CrseNumb,
                SeqNumb = course.SeqNumb,
                Enrollment = course.Enrollment,
                Units = (decimal)course.Units,
                CustDept = ResolveCustDept(course.SubjCode, course.DeptCode, course.Crn),
                Source = EffortConstants.SourceInCrest
            });
        }

        // Get instructors and effort records
        var courseData = allNonCrestCourses
            .Select(c => new NonCrestCourseData(c.Crn, c.SubjCode, c.CrseNumb))
            .ToList();
        await BuildNonCrestInstructorsAndEffortAsync(context, courseData, ct);
    }

    /// <summary>
    /// Data transfer record for Non-CREST course information.
    /// </summary>
    private sealed record NonCrestCourseData(string Crn, string SubjCode, string CrseNumb);

    /// <inheritdoc />
    public override async Task ExecuteAsync(HarvestContext context, CancellationToken ct = default)
    {
        // Import Non-CREST courses (excluding "In CREST" courses)
        var coursesToImport = context.Preview.NonCrestCourses
            .Where(c => c.Source != EffortConstants.SourceInCrest)
            .ToList();

        context.Logger.LogInformation(
            "Importing {Count} Non-CREST courses for term {TermCode}",
            coursesToImport.Count,
            context.TermCode);

        foreach (var course in coursesToImport)
        {
            var courseId = await ImportCourseAsync(course, context, ct);
            var key = BuildCourseLookupKey(course);
            context.CourseIdLookup[key] = courseId;
        }

        // Import Non-CREST instructors
        context.Logger.LogInformation(
            "Importing {Count} Non-CREST instructors for term {TermCode}",
            context.Preview.NonCrestInstructors.Count,
            context.TermCode);

        foreach (var instructor in context.Preview.NonCrestInstructors)
        {
            await ImportPersonAsync(instructor, context, ct);
        }

        // Import Non-CREST effort records
        context.Logger.LogInformation(
            "Importing {Count} Non-CREST effort records for term {TermCode}",
            context.Preview.NonCrestEffort.Count,
            context.TermCode);

        var importTasks = context.Preview.NonCrestEffort
            .Select(effort => ImportEffortRecordAsync(effort, context, ct));

        foreach (var importTask in importTasks)
        {
            var (record, preview) = await importTask;
            if (record != null && preview != null)
            {
                context.CreatedRecords.Add((record, preview));
            }
        }
    }

    private async Task BuildNonCrestInstructorsAndEffortAsync(
        HarvestContext context,
        List<NonCrestCourseData> allNonCrestCourses,
        CancellationToken ct)
    {
        var termCodeStr = context.TermCode.ToString();
        var nonCrestCrns = allNonCrestCourses
            .Select(c => c.Crn)
            .Distinct()
            .ToList();

        // Get POA entries for these courses (no view join - mirrors legacy query)
        var poaEntries = await context.CoursesContext.Poas
            .AsNoTracking()
            .Where(p => p.PoaTermCode == termCodeStr && nonCrestCrns.Contains(p.PoaCrn))
            .Select(p => new { p.PoaCrn, p.PoaPidm })
            .Distinct()
            .ToListAsync(ct);

        // Get AAUD IDS data for POA PIDMs (mirrors legacy join: poa_pidm = ids_pidm AND poa_term_code = ids_term_code)
        var nonCrestPidms = poaEntries.Select(p => p.PoaPidm.ToString()).Distinct().ToList();
        var nonCrestIdsRecords = await context.AaudContext.Ids
            .AsNoTracking()
            .Where(i => i.IdsPidm != null && nonCrestPidms.Contains(i.IdsPidm) && i.IdsTermCode == termCodeStr)
            .Select(i => new { i.IdsPidm, i.IdsMothraid, i.IdsPKey })
            .Distinct()
            .ToListAsync(ct);

        var nonCrestPidmToPKey = nonCrestIdsRecords
            .Where(i => !string.IsNullOrEmpty(i.IdsPidm) && !string.IsNullOrEmpty(i.IdsPKey))
            .GroupBy(i => i.IdsPidm!)
            .ToDictionary(g => g.Key, g => g.First().IdsPKey ?? "");

        var nonCrestPidmToMothraId = nonCrestIdsRecords
            .Where(i => !string.IsNullOrEmpty(i.IdsPidm) && !string.IsNullOrEmpty(i.IdsMothraid))
            .GroupBy(i => i.IdsPidm!)
            .ToDictionary(g => g.Key, g => g.First().IdsMothraid ?? "");

        var nonCrestPKeys = nonCrestPidmToPKey.Values.Where(pk => !string.IsNullOrEmpty(pk)).Distinct().ToList();

        // Get employees - no term filter (mirrors legacy: INNER JOIN employees on ids_pkey = emp_pkey)
        // Prefer current term if multiple records exist, otherwise use most recent term
        var nonCrestEmployees = await context.AaudContext.Employees
            .AsNoTracking()
            .Where(e => nonCrestPKeys.Contains(e.EmpPKey))
            .GroupBy(e => e.EmpPKey)
            .Select(g => g
                .OrderByDescending(e => e.EmpTermCode == termCodeStr)
                .ThenByDescending(e => e.EmpTermCode)
                .First())
            .ToDictionaryAsync(e => e.EmpPKey ?? "", e => e, ct);

        var nonCrestPersons = await context.AaudContext.People
            .AsNoTracking()
            .Where(p => nonCrestPKeys.Contains(p.PersonPKey))
            .ToDictionaryAsync(p => p.PersonPKey ?? "", p => p, ct);

        // Get VIPER person records by MothraId for PersonId lookup
        var nonCrestMothraIds = nonCrestPidmToMothraId.Values.Where(m => !string.IsNullOrEmpty(m)).Distinct().ToList();
        var viperPersonLookup = await context.ViperContext.People
            .AsNoTracking()
            .Where(p => nonCrestMothraIds.Contains(p.MothraId))
            .Select(p => new { p.PersonId, p.MothraId })
            .ToDictionaryAsync(p => p.MothraId ?? "", p => p.PersonId, ct);

        // Get lookups
        context.TitleLookup ??= (await context.InstructorService.GetTitleCodesAsync(ct))
            .ToDictionary(t => t.Code, t => t.Name, StringComparer.OrdinalIgnoreCase);

        context.JobGroupLookup ??= (await context.DictionaryContext.Titles
            .AsNoTracking()
            .Where(t => t.Code != null && t.JobGroupId != null)
            .Select(t => new { Code = t.Code!.Trim(), t.JobGroupId })
            .ToListAsync(ct))
            .GroupBy(t => t.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().JobGroupId, StringComparer.OrdinalIgnoreCase);

        // Batch-resolve departments using full resolution chain (jobs → employee fields → fallback)
        var batchDepts = await context.InstructorService.BatchResolveDepartmentsAsync(nonCrestMothraIds, context.TermCode, ct);
        var excludedTitleCodes = context.ExcludedTitleCodes ??= await context.InstructorService.GetExcludedTitleCodesAsync(ct);

        // CRN-keyed lookup for SubjCode/CrseNumb resolution from POA rows (POA has no unit info).
        // Variable-unit CRNs produce multiple allNonCrestCourses rows; take the first since
        // SubjCode and CrseNumb are identical across unit variants of the same CRN.
        var courseLookup = allNonCrestCourses
            .GroupBy(c => c.Crn)
            .ToDictionary(g => g.Key, g => g.First());

        // Create instructor previews and effort records
        var excludedMothraIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var excludedInstructors = new List<string>();
        var addedInstructorMothraIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var poa in poaEntries)
        {
            var pidmStr = poa.PoaPidm;

            if (!courseLookup.TryGetValue(poa.PoaCrn, out var course)) continue;

            if (!nonCrestPidmToPKey.TryGetValue(pidmStr, out var pKey) || string.IsNullOrEmpty(pKey)) continue;
            if (!nonCrestEmployees.TryGetValue(pKey, out var emp)) continue;

            var titleCode = emp.EmpEffortTitleCode?.Trim() ?? "";
            if (string.IsNullOrEmpty(titleCode)) continue;

            if (!nonCrestPidmToMothraId.TryGetValue(pidmStr, out var mothraId) || string.IsNullOrEmpty(mothraId)) continue;
            if (!nonCrestPersons.TryGetValue(pKey, out var aaudPerson)) continue;

            var firstName = aaudPerson.PersonFirstName?.Trim() ?? "";
            var lastName = aaudPerson.PersonLastName?.Trim() ?? "";
            var fullName = $"{lastName}, {firstName}";

            // Department already fully resolved by BatchResolveDepartmentsAsync
            var dept = batchDepts.GetValueOrDefault(mothraId, "UNK");

            var titleDesc = context.TitleLookup.TryGetValue(titleCode, out var desc) ? desc : titleCode;

            // Exclude emeritus/recall appointments from harvest (skips both the instructor
            // and the effort record built below).
            if (excludedTitleCodes.Contains(titleCode))
            {
                if (excludedMothraIds.Add(mothraId))
                {
                    excludedInstructors.Add($"{fullName} ({titleDesc})");
                }
                continue;
            }

            // Add instructor if not already added - skip if no valid VIPER person record
            if (!addedInstructorMothraIds.Contains(mothraId))
            {
                if (!viperPersonLookup.TryGetValue(mothraId, out var personId))
                {
                    context.Logger.LogWarning(
                        "Skipping Non-CREST instructor {MothraId} ({FullName}): no matching VIPER person record",
                        mothraId, fullName);
                    continue;
                }

                addedInstructorMothraIds.Add(mothraId);
                context.Preview.NonCrestInstructors.Add(new HarvestPersonPreview
                {
                    MothraId = mothraId,
                    PersonId = personId,
                    FullName = fullName,
                    FirstName = firstName,
                    LastName = lastName,
                    Department = dept,
                    TitleCode = titleCode,
                    TitleDescription = titleDesc,
                    Source = EffortConstants.SourceNonCrest
                });
            }

            // Determine effort type
            var isResearchCourse = course.CrseNumb?.EndsWith('R') ?? false;
            var effortType = isResearchCourse ? EffortConstants.ResearchEffortType : EffortConstants.VariableEffortType;

            context.Preview.NonCrestEffort.Add(new HarvestRecordPreview
            {
                MothraId = mothraId,
                PersonName = fullName,
                Crn = poa.PoaCrn,
                CourseCode = $"{course.SubjCode} {course.CrseNumb}",
                EffortType = effortType,
                RoleId = EffortConstants.DirectorRoleId,
                RoleName = "Director",
                Hours = 0,
                Weeks = null,
                Source = EffortConstants.SourceNonCrest
            });
        }

        AddEmeritusExclusionWarning(context, PhaseName, excludedInstructors);

        // Restore legacy IOR custodial-dept fallback (legacy Import.cfm Phase 2): when a
        // non-CREST course's own Banner dept doesn't resolve to an academic department,
        // inherit the custodial dept from its Director/IOR when that instructor's resolved
        // department is academic. Without this, such courses harvest as "UNK".
        ApplyDirectorCustodialDeptFallback(
            context.Preview.NonCrestCourses, context.Preview.NonCrestEffort, batchDepts);

        // Sort instructors
        var sortedInstructors = context.Preview.NonCrestInstructors.OrderBy(i => i.FullName).ToList();
        context.Preview.NonCrestInstructors.Clear();
        context.Preview.NonCrestInstructors.AddRange(sortedInstructors);
    }

    /// <summary>
    /// For non-CREST courses whose custodial dept is not one of the academic departments,
    /// overwrite it with the Director/IOR's resolved department when that is academic.
    /// Mirrors legacy Import.cfm Phase 2 ("look to the dept of the IOR"). Applies to all
    /// unit variants of a CRN.
    /// </summary>
    internal static void ApplyDirectorCustodialDeptFallback(
        List<HarvestCoursePreview> courses,
        List<HarvestRecordPreview> effort,
        Dictionary<string, string> batchDepts)
    {
        var academicDepts = EffortConstants.AcademicDepartments.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Map each CRN to its Director's department, keeping only academic departments.
        var directorDeptByCrn = effort
            .Where(e => e.RoleId == EffortConstants.DirectorRoleId && !string.IsNullOrWhiteSpace(e.Crn))
            .Select(e => new { e.Crn, Dept = batchDepts.GetValueOrDefault(e.MothraId, "UNK") })
            .Where(x => academicDepts.Contains(x.Dept))
            .GroupBy(x => x.Crn, StringComparer.OrdinalIgnoreCase)
            // OrderBy makes the choice deterministic when a CRN has multiple academic IOR depts.
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Dept).First().Dept, StringComparer.OrdinalIgnoreCase);

        if (directorDeptByCrn.Count == 0)
        {
            return;
        }

        foreach (var course in courses
            .Where(c => c.Source == EffortConstants.SourceNonCrest
                && !academicDepts.Contains(c.CustDept)
                && !string.IsNullOrWhiteSpace(c.Crn)
                && directorDeptByCrn.ContainsKey(c.Crn)))
        {
            course.CustDept = directorDeptByCrn[course.Crn];
        }
    }
}
