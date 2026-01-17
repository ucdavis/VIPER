using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services.Harvest;

/// <summary>
/// Phase 1: CREST course and instructor import.
/// Imports course data from the CREST scheduling system.
/// </summary>
public sealed class CrestHarvestPhase : HarvestPhaseBase
{
    /// <inheritdoc />
    public override string PhaseName => EffortConstants.SourceCrest;

    /// <inheritdoc />
    public override int Order => 10;

    /// <summary>
    /// DTO for CREST instructor data.
    /// </summary>
    private sealed record CrestInstructorDto(
        string MothraId,
        string FirstName,
        string LastName,
        string TitleCode,
        string HomeDept);

    /// <inheritdoc />
    public override async Task GeneratePreviewAsync(HarvestContext context, CancellationToken ct = default)
    {
        var termCodeStr = context.TermCode.ToString();

        // Step 1: Get master course list from tbl_Block (excluding DVM/clinical courses)
        var blocks = await context.CrestContext.Blocks
            .AsNoTracking()
            .Where(b => b.AcademicYear == termCodeStr)
            .Where(b => b.SsaCourseNum != null && !b.SsaCourseNum.Trim().StartsWith("DVM"))
            .ToListAsync(ct);

        if (blocks.Count == 0)
        {
            context.Logger.LogInformation("No CREST courses found in tbl_Block for term {TermCode}", context.TermCode);
            return;
        }

        var blockCourseIds = blocks.Select(b => b.EdutaskId).Distinct().ToHashSet();

        // Step 2: Get course session offerings filtered by block course IDs and term
        var courseOfferings = await context.CrestContext.CourseSessionOfferings
            .AsNoTracking()
            .Where(cso => cso.AcademicYear == termCodeStr)
            .Where(cso => blockCourseIds.Contains(cso.CourseId))
            .Where(cso => cso.SessionType != EffortConstants.SessionTypeDebrief)
            .ToListAsync(ct);

        // Step 3: Build unique courses from session offerings
        var uniqueCourses = courseOfferings
            .Where(c => !string.IsNullOrEmpty(c.Crn) && !string.IsNullOrEmpty(c.SsaCourseNum))
            .GroupBy(c => new { c.Crn, c.SsaCourseNum, c.SeqNumb })
            .Select(g => g.First())
            .ToList();

        foreach (var course in uniqueCourses)
        {
            var subjCode = course.SsaCourseNum?.Length >= 3 ? course.SsaCourseNum[..3] : "";
            var crseNumb = course.SsaCourseNum?.Length > 3 ? course.SsaCourseNum[3..] : "";

            context.Preview.CrestCourses.Add(new HarvestCoursePreview
            {
                Crn = course.Crn ?? "",
                SubjCode = subjCode,
                CrseNumb = crseNumb,
                SeqNumb = course.SeqNumb ?? "001",
                Enrollment = 0,
                Units = 0,
                CustDept = "VET",
                Source = EffortConstants.SourceCrest
            });
        }

        // Populate units and enrollment from roster data
        var crestCrns = context.Preview.CrestCourses
            .Select(c => c.Crn)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        var rosterSummaries = await context.CoursesContext.Rosters
            .AsNoTracking()
            .Where(r => r.RosterTermCode == termCodeStr && crestCrns.Contains(r.RosterCrn!))
            .GroupBy(r => r.RosterCrn!)
            .Select(g => new { Crn = g.Key, Units = g.Max(r => r.RosterUnit ?? 0), Enrollment = g.Count() })
            .ToDictionaryAsync(x => x.Crn, x => x, ct);

        foreach (var course in context.Preview.CrestCourses)
        {
            if (rosterSummaries.TryGetValue(course.Crn, out var summary))
            {
                course.Units = (decimal)summary.Units;
                course.Enrollment = summary.Enrollment;
            }
        }

        // Step 4: Get CREST instructors
        var crestInstructors = await GetCrestInstructorsAsync(context, ct);

        // Get title descriptions and department lookups
        context.TitleLookup ??= (await context.InstructorService.GetTitleCodesAsync(ct))
            .ToDictionary(t => t.Code, t => t.Name, StringComparer.OrdinalIgnoreCase);

        context.DeptSimpleNameLookup ??= await context.InstructorService.GetDepartmentSimpleNameLookupAsync(ct);

        // Get VIPER person IDs for the instructors
        var mothraIds = crestInstructors.Select(i => i.MothraId).Distinct().ToList();
        var personDetails = await context.ViperContext.People
            .AsNoTracking()
            .Where(p => mothraIds.Contains(p.MothraId))
            .Select(p => new { p.PersonId, p.MothraId })
            .ToDictionaryAsync(p => p.MothraId ?? "", p => p.PersonId, ct);

        foreach (var instructor in crestInstructors)
        {
            if (!personDetails.TryGetValue(instructor.MothraId, out var personId))
            {
                context.Logger.LogWarning(
                    "Skipping CREST instructor {MothraId} ({LastName}, {FirstName}): no matching VIPER person record",
                    instructor.MothraId, instructor.LastName, instructor.FirstName);
                continue;
            }

            var titleDesc = context.TitleLookup.TryGetValue(instructor.TitleCode, out var desc) ? desc : instructor.TitleCode;
            var dept = context.DeptSimpleNameLookup != null && context.DeptSimpleNameLookup.TryGetValue(instructor.HomeDept, out var deptName)
                ? deptName
                : instructor.HomeDept;
            if (string.IsNullOrEmpty(dept)) dept = "UNK";

            context.Preview.CrestInstructors.Add(new HarvestPersonPreview
            {
                MothraId = instructor.MothraId,
                PersonId = personId,
                FullName = $"{instructor.LastName}, {instructor.FirstName}",
                FirstName = instructor.FirstName,
                LastName = instructor.LastName,
                Department = dept,
                TitleCode = instructor.TitleCode,
                TitleDescription = titleDesc,
                Source = EffortConstants.SourceCrest
            });
        }

        // Step 5: Build effort records
        await BuildCrestEffortRecordsAsync(context, courseOfferings, blockCourseIds, ct);

        // Sort instructors by name
        var sortedInstructors = context.Preview.CrestInstructors.OrderBy(i => i.FullName).ToList();
        context.Preview.CrestInstructors.Clear();
        context.Preview.CrestInstructors.AddRange(sortedInstructors);
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync(HarvestContext context, CancellationToken ct = default)
    {
        // Import CREST instructors
        context.Logger.LogInformation(
            "Importing {Count} CREST instructors for term {TermCode}",
            context.Preview.CrestInstructors.Count,
            context.TermCode);

        foreach (var instructor in context.Preview.CrestInstructors)
        {
            await ImportPersonAsync(instructor, context, ct);
        }

        // Import CREST courses
        context.Logger.LogInformation(
            "Importing {Count} CREST courses for term {TermCode}",
            context.Preview.CrestCourses.Count,
            context.TermCode);

        foreach (var course in context.Preview.CrestCourses)
        {
            var courseId = await ImportCourseAsync(course, context, ct);
            var key = BuildCourseLookupKey(course);
            context.CourseIdLookup[key] = courseId;
        }

        // Import CREST effort records
        context.Logger.LogInformation(
            "Importing {Count} CREST effort records for term {TermCode}",
            context.Preview.CrestEffort.Count,
            context.TermCode);

        foreach (var effort in context.Preview.CrestEffort)
        {
            var result = await ImportEffortRecordAsync(effort, context, ct);
            if (result.Record != null && result.Preview != null)
            {
                context.CreatedRecords.Add((result.Record, result.Preview));
            }
        }
    }

    private static async Task<List<CrestInstructorDto>> GetCrestInstructorsAsync(
        HarvestContext context,
        CancellationToken ct)
    {
        var termCodeStr = context.TermCode.ToString();

        // Step 1: Get unique candidate PIDs from CREST offerings
        var courseOfferings = await context.CrestContext.CourseSessionOfferings
            .AsNoTracking()
            .Where(cso => cso.AcademicYear == termCodeStr)
            .Select(cso => cso.EdutaskOfferId)
            .Distinct()
            .ToListAsync(ct);

        var offerPersons = await context.CrestContext.EdutaskOfferPersons
            .AsNoTracking()
            .Where(eop => courseOfferings.Contains(eop.EdutaskOfferId))
            .Select(eop => eop.PersonId)
            .Distinct()
            .ToListAsync(ct);

        var pidmStrings = offerPersons.Select(p => p.ToString()).ToList();

        // Step 2: Get MothraIDs for these PIDs from AAUD
        var idsRecords = await context.AaudContext.Ids
            .AsNoTracking()
            .Where(i => i.IdsTermCode == termCodeStr && i.IdsPidm != null && pidmStrings.Contains(i.IdsPidm))
            .Select(i => new { i.IdsMothraid, i.IdsPKey })
            .Distinct()
            .ToListAsync(ct);

        var candidateMothraIds = idsRecords
            .Where(i => !string.IsNullOrEmpty(i.IdsMothraid))
            .Select(i => i.IdsMothraid!)
            .Distinct()
            .ToList();

        context.Logger.LogInformation(
            "Found {Count} candidate MothraIDs from CREST for term {TermCode}",
            candidateMothraIds.Count, context.TermCode);

        // Step 3: Get AAUD employee details
        var aaudIds = await context.AaudContext.Ids
            .AsNoTracking()
            .Where(i => i.IdsTermCode == termCodeStr && i.IdsMothraid != null && candidateMothraIds.Contains(i.IdsMothraid))
            .Select(i => new { i.IdsMothraid, i.IdsPKey })
            .ToListAsync(ct);

        var pKeys = aaudIds.Where(i => !string.IsNullOrEmpty(i.IdsPKey)).Select(i => i.IdsPKey!).Distinct().ToList();

        var employees = await context.AaudContext.Employees
            .AsNoTracking()
            .Where(e => e.EmpTermCode == termCodeStr && pKeys.Contains(e.EmpPKey))
            .Select(e => new { e.EmpPKey, e.EmpEffortTitleCode, e.EmpHomeDept })
            .ToListAsync(ct);

        var persons = await context.AaudContext.People
            .AsNoTracking()
            .Where(p => pKeys.Contains(p.PersonPKey))
            .Select(p => new { p.PersonPKey, p.PersonFirstName, p.PersonLastName })
            .ToListAsync(ct);

        // Get valid title codes
        var titleCodes = employees
            .Where(e => !string.IsNullOrEmpty(e.EmpEffortTitleCode))
            .Select(e => e.EmpEffortTitleCode!.Trim())
            .Distinct()
            .ToList();

        var validTitleCodes = await context.DictionaryContext.Titles
            .AsNoTracking()
            .Where(t => t.Code != null && titleCodes.Contains(t.Code))
            .Select(t => t.Code!)
            .Distinct()
            .ToListAsync(ct);

        var validTitleCodesSet = validTitleCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        context.DeptSimpleNameLookup ??= await context.InstructorService.GetDepartmentSimpleNameLookupAsync(ct);

        // Build lookups
        var pKeyToEmployee = employees.ToDictionary(e => e.EmpPKey ?? "", e => e, StringComparer.OrdinalIgnoreCase);
        var pKeyToPerson = persons.ToDictionary(p => p.PersonPKey ?? "", p => p, StringComparer.OrdinalIgnoreCase);
        var mothraIdToPKey = aaudIds
            .Where(i => !string.IsNullOrEmpty(i.IdsMothraid) && !string.IsNullOrEmpty(i.IdsPKey))
            .GroupBy(i => i.IdsMothraid!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(i => i.IdsPKey!).ToList(), StringComparer.OrdinalIgnoreCase);

        // Step 4: Build instructor details
        var instructorDetails = new List<CrestInstructorDto>();

        foreach (var mothraId in candidateMothraIds)
        {
            if (!mothraIdToPKey.TryGetValue(mothraId, out var pKeyList) || pKeyList.Count == 0)
            {
                continue;
            }

            var detailsForMothraId = new List<CrestInstructorDto>();

            foreach (var pKey in pKeyList)
            {
                if (!pKeyToEmployee.TryGetValue(pKey, out var emp) ||
                    !pKeyToPerson.TryGetValue(pKey, out var person))
                {
                    continue;
                }

                var titleCode = emp.EmpEffortTitleCode?.Trim() ?? "";

                if (string.IsNullOrEmpty(titleCode) || !validTitleCodesSet.Contains(titleCode))
                {
                    continue;
                }

                var homeDept = ResolveDeptSimpleName(emp.EmpHomeDept, context.DeptSimpleNameLookup) ?? "";

                detailsForMothraId.Add(new CrestInstructorDto(
                    MothraId: mothraId,
                    FirstName: (person.PersonFirstName ?? "").ToUpperInvariant(),
                    LastName: (person.PersonLastName ?? "").ToUpperInvariant(),
                    TitleCode: titleCode,
                    HomeDept: homeDept));
            }

            // Only include if single record (legacy HAVING COUNT(*) = 1)
            if (detailsForMothraId.Count == 1)
            {
                instructorDetails.Add(detailsForMothraId[0]);
            }
        }

        var results = instructorDetails
            .OrderBy(i => i.LastName)
            .ThenBy(i => i.FirstName)
            .ToList();

        context.Logger.LogInformation(
            "EF query returned {Count} CREST instructors for term {TermCode}",
            results.Count, context.TermCode);

        return results;
    }

    private async Task BuildCrestEffortRecordsAsync(
        HarvestContext context,
        List<Viper.Models.Crest.CrestCourseSessionOffering> courseOfferings,
        HashSet<int> blockCourseIds,
        CancellationToken ct)
    {
        var termCodeStr = context.TermCode.ToString();

        // Get offer persons and edutask persons
        var offeringIds = courseOfferings.Select(c => c.EdutaskOfferId).Distinct().ToList();

        var offerPersons = await context.CrestContext.EdutaskOfferPersons
            .AsNoTracking()
            .Where(eop => offeringIds.Contains(eop.EdutaskOfferId))
            .ToListAsync(ct);

        var edutaskPersons = await context.CrestContext.EdutaskPersons
            .AsNoTracking()
            .Where(ep => blockCourseIds.Contains(ep.EdutaskId))
            .ToListAsync(ct);

        var personRoleLookup = edutaskPersons
            .GroupBy(ep => new { ep.EdutaskId, ep.PersonId })
            .ToDictionary(
                g => (g.Key.EdutaskId, g.Key.PersonId),
                g => g.FirstOrDefault(ep => ep.RoleCode == EffortConstants.CrestDirectorRoleCode)?.RoleCode);

        // Build PIDM to MothraId lookup
        var pidms = offerPersons.Select(op => op.PersonId.ToString()).Distinct().ToList();
        var idsRecords = await context.AaudContext.Ids
            .AsNoTracking()
            .Where(i => i.IdsPidm != null && pidms.Contains(i.IdsPidm) && i.IdsTermCode == termCodeStr)
            .Select(i => new { i.IdsPidm, i.IdsMothraid })
            .Distinct()
            .ToListAsync(ct);

        var pidmToMothraId = idsRecords
            .Where(i => !string.IsNullOrEmpty(i.IdsPidm) && !string.IsNullOrEmpty(i.IdsMothraid))
            .GroupBy(i => i.IdsPidm!)
            .ToDictionary(g => g.Key, g => g.First().IdsMothraid ?? "");

        // Build MothraId -> PersonName lookup
        var allMothraIds = pidmToMothraId.Values.Distinct().ToList();
        var personNames = await context.AaudContext.Ids
            .AsNoTracking()
            .Where(i => i.IdsMothraid != null && allMothraIds.Contains(i.IdsMothraid) && i.IdsTermCode == termCodeStr)
            .Join(context.AaudContext.People,
                ids => ids.IdsPKey,
                person => person.PersonPKey,
                (ids, person) => new { ids.IdsMothraid, person.PersonFirstName, person.PersonLastName })
            .Distinct()
            .ToListAsync(ct);

        var mothraIdToName = personNames
            .Where(p => !string.IsNullOrEmpty(p.IdsMothraid))
            .GroupBy(p => p.IdsMothraid!)
            .ToDictionary(
                g => g.Key,
                g => $"{g.First().PersonLastName?.ToUpper()}, {g.First().PersonFirstName?.ToUpper()}");

        // Log time data availability
        var offeringsWithTime = courseOfferings.Count(o => !string.IsNullOrEmpty(o.FromTime) && !string.IsNullOrEmpty(o.ThruTime));
        var offeringsWithDate = courseOfferings.Count(o => o.FromDate.HasValue && o.ThruDate.HasValue);
        context.Logger.LogInformation(
            "CREST offerings: {Total} total, {WithDate} with dates, {WithTime} with times",
            courseOfferings.Count, offeringsWithDate, offeringsWithTime);

        // Build effort records
        var effortByPersonCourse = new Dictionary<string, (HarvestRecordPreview Record, int TotalMinutes)>();

        var offeringAssignments = courseOfferings
            .Join(
                offerPersons,
                offering => offering.EdutaskOfferId,
                assignment => assignment.EdutaskOfferId,
                (offering, assignment) => new { Offering = offering, Assignment = assignment });

        foreach (var item in offeringAssignments)
        {
            var offering = item.Offering;
            var assignment = item.Assignment;

            if (!pidmToMothraId.TryGetValue(assignment.PersonId.ToString(), out var mothraId) || string.IsNullOrEmpty(mothraId))
            {
                continue;
            }

            var instructor = context.Preview.CrestInstructors.FirstOrDefault(i => i.MothraId == mothraId);
            var personName = instructor?.FullName ??
                (mothraIdToName.TryGetValue(mothraId, out var name) ? name : mothraId);

            var minutes = HarvestTimeParser.CalculateSessionMinutes(
                offering.FromDate, offering.FromTime,
                offering.ThruDate, offering.ThruTime,
                context.Logger, $"offering {offering.EdutaskOfferId}");

            var isDirector = personRoleLookup.TryGetValue((offering.CourseId, assignment.PersonId), out var roleCode)
                && roleCode == EffortConstants.CrestDirectorRoleCode;
            var roleId = isDirector ? EffortConstants.DirectorRoleId : EffortConstants.ClinicalInstructorRoleId;

            var subjCode = offering.SsaCourseNum?.Length >= 3 ? offering.SsaCourseNum[..3] : "";
            var crseNumb = offering.SsaCourseNum?.Length > 3 ? offering.SsaCourseNum[3..] : "";
            var courseCode = $"{subjCode} {crseNumb}";
            var effortKey = $"{mothraId}-{offering.Crn}-{courseCode}-{offering.SeqNumb}-{offering.SessionType}-{roleId}";

            if (!effortByPersonCourse.TryGetValue(effortKey, out var existing))
            {
                var record = new HarvestRecordPreview
                {
                    MothraId = mothraId,
                    PersonName = personName,
                    Crn = offering.Crn ?? "",
                    CourseCode = courseCode,
                    EffortType = offering.SessionType ?? "VAR",
                    Hours = 0,
                    RoleId = roleId,
                    RoleName = isDirector ? "Director" : "Instructor",
                    Source = EffortConstants.SourceCrest
                };
                effortByPersonCourse[effortKey] = (record, minutes);
            }
            else
            {
                effortByPersonCourse[effortKey] = (existing.Record, existing.TotalMinutes + minutes);
            }
        }

        // Convert minutes to hours
        foreach (var (record, totalMinutes) in effortByPersonCourse.Values)
        {
            record.Hours = (int)Math.Round(totalMinutes / 60.0);
            context.Preview.CrestEffort.Add(record);
        }

        context.Logger.LogInformation(
            "Extracted {Count} CREST effort records for term {TermCode}",
            context.Preview.CrestEffort.Count, context.TermCode);
    }
}
