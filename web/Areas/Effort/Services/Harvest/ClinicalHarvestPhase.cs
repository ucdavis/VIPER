using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services.Harvest;

/// <summary>
/// Phase 3: Clinical Scheduler import.
/// Imports clinical rotation data from the Clinical Scheduler system.
/// Only executes for semester terms (not quarter terms).
/// </summary>
public sealed class ClinicalHarvestPhase : HarvestPhaseBase
{
    /// <inheritdoc />
    public override string PhaseName => EffortConstants.PhaseClinical;

    /// <inheritdoc />
    public override int Order => 30;

    /// <inheritdoc />
    public override bool ShouldExecute(int termCode) => IsSemesterTerm(termCode);

    /// <summary>
    /// Clinical course priority lookup. Lower values = higher priority.
    /// When an instructor is assigned to multiple rotations in the same week,
    /// only the highest priority course is credited.
    /// </summary>
    private static readonly Dictionary<string, int> ClinicalCoursePriority = new()
    {
        ["DVM 453"] = -100, // Comm surgery - highest priority
        ["DVM 491"] = 10,   // SA emergency over ICU
        ["DVM 492"] = 11,
        ["DVM 476"] = 20,   // LA anest over SA anest
        ["DVM 490"] = 21,
        ["DVM 477"] = 30,   // LA radio over SA radio
        ["DVM 494"] = 31,
        ["DVM 482"] = 40,   // Med onc over rad onc
        ["DVM 487"] = 41,
        ["DVM 493"] = 50,   // Internal med: Med A first
        ["DVM 466"] = 51,
        ["DVM 443"] = 52,
        ["DVM 451"] = 60,   // Clin path over anat path
        ["DVM 485"] = 61,
        ["DVM 457"] = 70,   // Equine emergency over S&L
        ["DVM 462"] = 71,
        ["DVM 459"] = 80,   // Equine field over in-house
        ["DVM 460"] = 81,
        ["DVM 447"] = 100,  // EduLead - lowest priority
    };

    /// <inheritdoc />
    public override async Task GeneratePreviewAsync(HarvestContext context, CancellationToken ct = default)
    {
        if (!ShouldExecute(context.TermCode))
        {
            context.Logger.LogInformation(
                "Skipping Clinical Scheduler import for quarter term {TermCode}",
                context.TermCode);
            context.Preview.Warnings.Add(new HarvestWarning
            {
                Phase = EffortConstants.PhaseClinical,
                Message = "Clinical Scheduler import skipped",
                Details = "Clinical Scheduler data is only imported for semester terms, not quarter terms."
            });
            return;
        }

        // Get week IDs for this term
        var weekIds = await context.ViperContext.Weeks
            .AsNoTracking()
            .Where(w => w.TermCode == context.TermCode)
            .Select(w => w.WeekId)
            .ToListAsync(ct);

        // Get clinical instructor schedules
        var clinicalData = await context.ViperContext.InstructorSchedules
            .AsNoTracking()
            .Where(s => weekIds.Contains(s.WeekId))
            .Where(s => !string.IsNullOrEmpty(s.SubjCode) && !string.IsNullOrEmpty(s.CrseNumb))
            .Select(s => new
            {
                s.MothraId,
                s.WeekId,
                s.SubjCode,
                s.CrseNumb,
                s.RotationId,
                s.Role,
                PersonName = s.LastName + ", " + s.FirstName
            })
            .ToListAsync(ct);

        // Group by person and course to count weeks
        var personWeekCourses = new Dictionary<string, Dictionary<int, List<string>>>();

        foreach (var schedule in clinicalData.Where(s => !string.IsNullOrEmpty(s.MothraId)))
        {
            if (!personWeekCourses.ContainsKey(schedule.MothraId))
            {
                personWeekCourses[schedule.MothraId] = new Dictionary<int, List<string>>();
            }

            if (!personWeekCourses[schedule.MothraId].ContainsKey(schedule.WeekId))
            {
                personWeekCourses[schedule.MothraId][schedule.WeekId] = [];
            }

            var courseKey = $"{schedule.SubjCode} {schedule.CrseNumb}";
            if (!personWeekCourses[schedule.MothraId][schedule.WeekId].Contains(courseKey))
            {
                personWeekCourses[schedule.MothraId][schedule.WeekId].Add(courseKey);
            }
        }

        // Build effort records with priority resolution
        var effortByPersonCourse = new Dictionary<string, HarvestRecordPreview>();

        foreach (var schedule in clinicalData.Where(s =>
            !string.IsNullOrEmpty(s.MothraId) && !string.IsNullOrEmpty(s.SubjCode)))
        {
            var courseKey = $"{schedule.SubjCode} {schedule.CrseNumb}";
            var effortKey = $"{schedule.MothraId}-{courseKey}";

            // Check if this course is the priority for this person/week
            var weekCourses = personWeekCourses[schedule.MothraId][schedule.WeekId];
            var priorityCourse = GetPriorityCourse(weekCourses);

            if (courseKey != priorityCourse) continue;

            if (!effortByPersonCourse.ContainsKey(effortKey))
            {
                effortByPersonCourse[effortKey] = new HarvestRecordPreview
                {
                    MothraId = schedule.MothraId,
                    PersonName = schedule.PersonName ?? "",
                    Crn = "",
                    CourseCode = courseKey,
                    EffortType = EffortConstants.ClinicalEffortType,
                    Weeks = 0,
                    RoleId = EffortConstants.ClinicalInstructorRoleId,
                    RoleName = "Instructor",
                    Source = EffortConstants.SourceClinical
                };
            }

            effortByPersonCourse[effortKey].Weeks = (effortByPersonCourse[effortKey].Weeks ?? 0) + 1;
        }

        context.Preview.ClinicalEffort.AddRange(effortByPersonCourse.Values.Where(e => e.Weeks > 0));

        // Get unique clinical courses
        var clinicalCourses = clinicalData
            .Where(c => !string.IsNullOrEmpty(c.SubjCode) && !string.IsNullOrEmpty(c.CrseNumb))
            .Select(c => new { c.SubjCode, c.CrseNumb })
            .Distinct()
            .ToList();

        // Look up CRNs from Banner for clinical courses
        var termCodeStr = context.TermCode.ToString();
        var bannerCrns = await context.CoursesContext.Baseinfos
            .AsNoTracking()
            .Where(b => b.BaseinfoTermCode == termCodeStr && b.BaseinfoSeqNumb == "001")
            .Select(b => new { b.BaseinfoSubjCode, b.BaseinfoCrseNumb, b.BaseinfoCrn })
            .ToListAsync(ct);

        var crnLookup = bannerCrns
            .GroupBy(b => $"{b.BaseinfoSubjCode}-{b.BaseinfoCrseNumb}")
            .ToDictionary(g => g.Key, g => g.First().BaseinfoCrn, StringComparer.OrdinalIgnoreCase);

        foreach (var course in clinicalCourses)
        {
            var lookupKey = $"{course.SubjCode}-{course.CrseNumb}";
            var crn = crnLookup.GetValueOrDefault(lookupKey, "");

            context.Preview.ClinicalCourses.Add(new HarvestCoursePreview
            {
                Crn = crn,
                SubjCode = course.SubjCode ?? "",
                CrseNumb = course.CrseNumb ?? "",
                SeqNumb = "001",
                Enrollment = EffortConstants.DefaultClinicalEnrollment,
                Units = EffortConstants.DefaultClinicalUnits,
                CustDept = EffortConstants.ClinicalCustodialDept,
                Source = EffortConstants.SourceClinical
            });
        }

        // Build clinical instructor previews
        await BuildClinicalInstructorPreviewsAsync(context, clinicalData.Select(c => ((string?)c.MothraId, (string?)c.PersonName)).ToList(), ct);
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync(HarvestContext context, CancellationToken ct = default)
    {
        if (!ShouldExecute(context.TermCode))
        {
            return;
        }

        // Import clinical courses
        context.Logger.LogInformation(
            "Importing {Count} clinical courses for term {TermCode}",
            context.Preview.ClinicalCourses.Count,
            context.TermCode);

        foreach (var course in context.Preview.ClinicalCourses)
        {
            var key = BuildCourseLookupKey(course);
            if (!context.CourseIdLookup.ContainsKey(key))
            {
                var courseId = await ImportCourseAsync(course, context, ct);
                context.CourseIdLookup[key] = courseId;
            }
        }

        // Import clinical effort records
        context.Logger.LogInformation(
            "Importing {Count} clinical effort records for term {TermCode}",
            context.Preview.ClinicalEffort.Count,
            context.TermCode);

        // Get all instructors from all phases for lookup
        var allInstructors = context.Preview.CrestInstructors
            .Concat(context.Preview.NonCrestInstructors)
            .Concat(context.Preview.GuestAccounts)
            .DistinctBy(p => p.MothraId)
            .ToList();

        foreach (var effort in context.Preview.ClinicalEffort)
        {
            // Look up instructor - first check existing preview lists
            var instructorPreview = allInstructors.FirstOrDefault(i => i.MothraId == effort.MothraId);

            // If not found, look up directly from VIPER (clinical-only instructor)
            if (instructorPreview == null)
            {
                var person = await context.ViperContext.People
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.MothraId == effort.MothraId, ct);

                if (person == null)
                {
                    context.Warnings.Add(new HarvestWarning
                    {
                        Phase = EffortConstants.PhaseClinical,
                        Message = $"Instructor {effort.MothraId} not found in VIPER",
                        Details = effort.CourseCode
                    });
                    continue;
                }

                // Create preview record for this clinical-only instructor
                var dept = await ResolveDepartmentAsync(person.PersonId, context, ct);
                instructorPreview = new HarvestPersonPreview
                {
                    MothraId = person.MothraId ?? "",
                    PersonId = person.PersonId,
                    FullName = $"{person.LastName}, {person.FirstName}",
                    FirstName = person.FirstName ?? "",
                    LastName = person.LastName ?? "",
                    Department = dept,
                    TitleCode = "",
                    TitleDescription = "",
                    Source = EffortConstants.SourceClinical
                };
            }

            // Import instructor if not already imported
            await ImportPersonAsync(instructorPreview, context, ct);

            var result = await ImportEffortRecordAsync(effort, context, ct);
            if (result.Record != null && result.Preview != null)
            {
                context.CreatedRecords.Add((result.Record, result.Preview));
            }
        }
    }

    private async Task BuildClinicalInstructorPreviewsAsync(
        HarvestContext context,
        List<(string? MothraId, string? PersonName)> clinicalPersonData,
        CancellationToken ct)
    {
        var termCodeStr = context.TermCode.ToString();

        // Get unique clinical instructor MothraIds
        var clinicalMothraIds = clinicalPersonData
            .Where(c => !string.IsNullOrEmpty(c.MothraId))
            .Select(c => c.MothraId!)
            .Distinct()
            .ToList();

        // Look up AAUD info for clinical instructors
        var clinicalAaudInfo = await context.AaudContext.Ids
            .AsNoTracking()
            .Where(ids => ids.IdsMothraid != null && clinicalMothraIds.Contains(ids.IdsMothraid))
            .Where(ids => ids.IdsTermCode == termCodeStr)
            .Join(context.AaudContext.Employees.Where(e => e.EmpTermCode == termCodeStr),
                ids => ids.IdsPKey,
                emp => emp.EmpPKey,
                (ids, emp) => new
                {
                    MothraId = ids.IdsMothraid,
                    emp.EmpEffortTitleCode,
                    emp.EmpEffortHomeDept
                })
            .ToListAsync(ct);

        var clinicalAaudLookup = clinicalAaudInfo
            .Where(x => !string.IsNullOrEmpty(x.MothraId))
            .GroupBy(x => x.MothraId!)
            .ToDictionary(g => g.Key, g => g.First());

        // Get title and department lookups
        context.TitleLookup ??= (await context.InstructorService.GetTitleCodesAsync(ct))
            .ToDictionary(t => t.Code, t => t.Name, StringComparer.OrdinalIgnoreCase);

        context.DeptSimpleNameLookup ??= await context.InstructorService.GetDepartmentSimpleNameLookupAsync(ct);

        // Get existing instructor MothraIds for IsNew determination
        var existingMothraIds = context.Preview.CrestInstructors
            .Concat(context.Preview.NonCrestInstructors)
            .Select(i => i.MothraId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Build instructor name lookup
        var clinicalInstructorNames = clinicalPersonData
            .Where(c => !string.IsNullOrEmpty(c.MothraId))
            .GroupBy(c => c.MothraId!)
            .ToDictionary(g => g.Key, g => g.First().PersonName ?? "");

        foreach (var mothraId in clinicalMothraIds)
        {
            var personName = clinicalInstructorNames.GetValueOrDefault(mothraId, "");
            var aaudInfo = clinicalAaudLookup.GetValueOrDefault(mothraId);

            var titleCode = aaudInfo?.EmpEffortTitleCode?.Trim() ?? "";
            var titleDesc = !string.IsNullOrEmpty(titleCode) && context.TitleLookup.TryGetValue(titleCode, out var desc)
                ? desc
                : titleCode;

            var effortDept = aaudInfo?.EmpEffortHomeDept?.Trim() ?? "";
            var deptName = context.DeptSimpleNameLookup != null &&
                           !string.IsNullOrEmpty(effortDept) &&
                           context.DeptSimpleNameLookup.TryGetValue(effortDept, out var dn)
                ? dn
                : effortDept;

            context.Preview.ClinicalInstructors.Add(new HarvestPersonPreview
            {
                MothraId = mothraId,
                FullName = personName,
                Department = deptName,
                TitleCode = titleCode,
                TitleDescription = titleDesc,
                Source = EffortConstants.SourceClinical,
                IsNew = !existingMothraIds.Contains(mothraId)
            });
        }

        // Sort clinical instructors by name
        context.Preview.ClinicalInstructors = context.Preview.ClinicalInstructors
            .OrderBy(i => i.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string GetPriorityCourse(List<string> courses)
    {
        if (courses.Count == 1)
        {
            return courses[0];
        }

        return courses.MinBy(c => ClinicalCoursePriority.GetValueOrDefault(c, 0)) ?? courses[0];
    }
}
