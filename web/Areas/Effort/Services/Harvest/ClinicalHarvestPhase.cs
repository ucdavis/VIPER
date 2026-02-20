using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services.Harvest;

/// <summary>
/// Phase 3: Clinical Scheduler import.
/// Preview generation builds harvest-specific DTOs; execution delegates to
/// ClinicalImportService (single code path for clinical record creation).
/// Only executes for semester terms (not quarter terms).
/// </summary>
public sealed class ClinicalHarvestPhase : HarvestPhaseBase
{
    private readonly IClinicalImportService _clinicalImportService;

    public ClinicalHarvestPhase(IClinicalImportService clinicalImportService)
    {
        _clinicalImportService = clinicalImportService;
    }

    /// <inheritdoc />
    public override string PhaseName => EffortConstants.PhaseClinical;

    /// <inheritdoc />
    public override int Order => 30;

    /// <inheritdoc />
    public override bool ShouldExecute(int termCode) => IsSemesterTerm(termCode);

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

        // Validate which instructors have valid AAUD data for import
        // (shared validation with ClinicalImportService — single source of truth)
        var uniqueMothraIds = personWeekCourses.Keys.ToList();
        var validation = await _clinicalImportService.ValidateImportableInstructorsAsync(
            uniqueMothraIds, context.TermCode, ct);

        var importableMothraIds = validation.ImportableMothraIds;
        var aaudTitleLookup = validation.TitleCodeByMothraId;

        // Populate title lookup on context for BuildClinicalInstructorPreviewsAsync
        context.TitleLookup ??= (await context.InstructorService.GetTitleCodesAsync(ct))
            .ToDictionary(t => t.Code, t => t.Name, StringComparer.OrdinalIgnoreCase);

        // Build effort records with priority resolution (uses shared priority from ClinicalImportService)
        var effortByPersonCourse = new Dictionary<string, HarvestRecordPreview>();

        foreach (var schedule in clinicalData.Where(s =>
            !string.IsNullOrEmpty(s.MothraId) && !string.IsNullOrEmpty(s.SubjCode)
            && importableMothraIds.Contains(s.MothraId)))
        {
            var courseKey = $"{schedule.SubjCode} {schedule.CrseNumb}";
            var effortKey = $"{schedule.MothraId}-{courseKey}";

            // Check if this course is the priority for this person/week
            var weekCourses = personWeekCourses[schedule.MothraId][schedule.WeekId];
            var priorityCourse = ClinicalImportService.GetPriorityCourse(weekCourses);

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

        // Build clinical instructor previews (only for importable instructors, reuse AAUD data)
        await BuildClinicalInstructorPreviewsAsync(context,
            clinicalData.Select(c => ((string?)c.MothraId, (string?)c.PersonName)).ToList(),
            importableMothraIds, aaudTitleLookup, ct);
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync(HarvestContext context, CancellationToken ct = default)
    {
        if (!ShouldExecute(context.TermCode))
        {
            return;
        }

        // Single code path: delegate to ClinicalImportService for all clinical record creation
        // (persons, courses, and effort records are created as needed)
        context.Logger.LogInformation(
            "Importing clinical effort for term {TermCode} via ClinicalImportService",
            context.TermCode);

        var result = await _clinicalImportService.ExecuteImportAsync(
            context.TermCode, ClinicalImportMode.AddNewOnly, context.ModifiedBy, ct);

        context.RecordsImported += result.RecordsAdded;
        await context.ForceReportProgressAsync(PhaseName);

        if (result.RecordsSkipped > 0)
        {
            var details = result.SkippedInstructors.Count > 0
                ? $"Skipped MothraIds: {string.Join(", ", result.SkippedInstructors)}"
                : "Check instructor AAUD data for the term.";

            context.Warnings.Add(new HarvestWarning
            {
                Phase = EffortConstants.PhaseClinical,
                Message = $"{result.RecordsSkipped} clinical records skipped (no AAUD record or invalid title)",
                Details = details
            });
        }
    }

    private async Task BuildClinicalInstructorPreviewsAsync(
        HarvestContext context,
        List<(string? MothraId, string? PersonName)> clinicalPersonData,
        HashSet<string> importableMothraIds,
        Dictionary<string, string> aaudTitleLookup,
        CancellationToken ct)
    {
        // Only build previews for importable instructors (those with valid AAUD data + title code)
        var clinicalMothraIds = clinicalPersonData
            .Where(c => !string.IsNullOrEmpty(c.MothraId) && importableMothraIds.Contains(c.MothraId!))
            .Select(c => c.MothraId!)
            .Distinct()
            .ToList();

        // Title lookup already populated by GeneratePreviewAsync
        // Batch-resolve departments only for importable instructors
        var batchDepts = await context.InstructorService.BatchResolveDepartmentsAsync(clinicalMothraIds, context.TermCode, ct);

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

        var titleLookup = context.TitleLookup!;
        foreach (var mothraId in clinicalMothraIds)
        {
            var personName = clinicalInstructorNames.GetValueOrDefault(mothraId, "");

            var titleCode = aaudTitleLookup.GetValueOrDefault(mothraId, "");
            var titleDesc = !string.IsNullOrEmpty(titleCode) && titleLookup.TryGetValue(titleCode, out var desc)
                ? desc
                : titleCode;

            var dept = batchDepts.GetValueOrDefault(mothraId, "UNK");

            context.Preview.ClinicalInstructors.Add(new HarvestPersonPreview
            {
                MothraId = mothraId,
                FullName = personName,
                Department = dept,
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
}
