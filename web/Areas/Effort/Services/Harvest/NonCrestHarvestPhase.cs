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

        // Get CRNs already in CREST (from Phase 1)
        var existingCrns = context.Preview.CrestCourses.Select(c => c.Crn).ToHashSet();

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
                        b.BaseinfoCrseNumb != null && EF.Functions.Like(b.BaseinfoCrseNumb, "%R"))
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

        // Combine courses and separate into those to import vs those already in CREST
        var allScheduleCourses = coursesWithEnrollment
            .Concat(researchCourses)
            .DistinctBy(c => new { c.Crn, c.Units })
            .ToList();

        // Add courses not in CREST (these will be imported)
        var allNonCrestCourses = allScheduleCourses
            .Where(c => !existingCrns.Contains(c.Crn))
            .ToList();

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
                CustDept = course.DeptCode,
                Source = EffortConstants.SourceNonCrest
            });
        }

        // Add courses that ARE in CREST (for transparency)
        var crestDuplicates = allScheduleCourses
            .Where(c => existingCrns.Contains(c.Crn))
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
                CustDept = course.DeptCode,
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

        foreach (var effort in context.Preview.NonCrestEffort)
        {
            var result = await ImportEffortRecordAsync(effort, context, ct);
            if (result.Record != null && result.Preview != null)
            {
                context.CreatedRecords.Add((result.Record, result.Preview));
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

        // Get POA entries for these courses
        var poaEntries = await context.CoursesContext.Poas
            .AsNoTracking()
            .Where(p => p.PoaTermCode == termCodeStr && nonCrestCrns.Contains(p.PoaCrn))
            .Join(
                context.CoursesContext.VwPoaPidmNames.AsNoTracking(),
                p => p.PoaPidm,
                v => v.IdsPidm,
                (p, v) => new { p.PoaCrn, p.PoaPidm, v.PersonClientid })
            .Distinct()
            .ToListAsync(ct);

        // Get person info from VIPER
        var clientIds = poaEntries.Select(p => p.PersonClientid).Distinct().ToList();
        var instructorDetails = await context.ViperContext.People
            .AsNoTracking()
            .Where(p => clientIds.Contains(p.ClientId))
            .Select(p => new { p.PersonId, p.MothraId, p.FirstName, p.LastName, p.ClientId })
            .ToListAsync(ct);

        var clientIdToInstructor = instructorDetails.ToDictionary(i => i.ClientId, i => i);

        // Get AAUD employee data
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
        var nonCrestEmployees = await context.AaudContext.Employees
            .AsNoTracking()
            .Where(e => e.EmpTermCode == termCodeStr && nonCrestPKeys.Contains(e.EmpPKey))
            .ToDictionaryAsync(e => e.EmpPKey ?? "", e => e, ct);

        var nonCrestPersons = await context.AaudContext.People
            .AsNoTracking()
            .Where(p => nonCrestPKeys.Contains(p.PersonPKey))
            .ToDictionaryAsync(p => p.PersonPKey ?? "", p => p, ct);

        // Get lookups
        context.TitleLookup ??= (await context.InstructorService.GetTitleCodesAsync(ct))
            .ToDictionary(t => t.Code, t => t.Name, StringComparer.OrdinalIgnoreCase);

        context.DeptSimpleNameLookup ??= await context.InstructorService.GetDepartmentSimpleNameLookupAsync(ct);

        // Build course lookup for effort records
        var courseLookup = allNonCrestCourses
            .ToDictionary(c => c.Crn, c => c);

        // Create instructor previews and effort records
        foreach (var poa in poaEntries)
        {
            var pidmStr = poa.PoaPidm.ToString();

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

            var rawDeptCode = emp.EmpHomeDept?.Trim() ?? "";
            var dept = context.DeptSimpleNameLookup != null && context.DeptSimpleNameLookup.TryGetValue(rawDeptCode, out var deptName)
                ? deptName
                : rawDeptCode;
            if (string.IsNullOrEmpty(dept)) dept = "UNK";

            var titleDesc = context.TitleLookup.TryGetValue(titleCode, out var desc) ? desc : titleCode;

            // Add instructor if not already added - skip if no valid VIPER person record
            if (!context.Preview.NonCrestInstructors.Any(i => i.MothraId == mothraId))
            {
                if (poa.PersonClientid == null || !clientIdToInstructor.TryGetValue(poa.PersonClientid, out var viperPerson))
                {
                    context.Logger.LogWarning(
                        "Skipping Non-CREST instructor {MothraId} ({FullName}): no matching VIPER person record",
                        mothraId, fullName);
                    continue;
                }

                context.Preview.NonCrestInstructors.Add(new HarvestPersonPreview
                {
                    MothraId = mothraId,
                    PersonId = viperPerson.PersonId,
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
                Weeks = 0,
                Source = EffortConstants.SourceNonCrest
            });
        }

        // Sort instructors
        var sortedInstructors = context.Preview.NonCrestInstructors.OrderBy(i => i.FullName).ToList();
        context.Preview.NonCrestInstructors.Clear();
        context.Preview.NonCrestInstructors.AddRange(sortedInstructors);
    }
}
