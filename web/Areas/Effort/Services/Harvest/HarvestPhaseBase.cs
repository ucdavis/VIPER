using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services.Harvest;

/// <summary>
/// Base class for harvest phases with shared helper methods.
/// </summary>
public abstract class HarvestPhaseBase : IHarvestPhase
{
    /// <inheritdoc />
    public abstract string PhaseName { get; }

    /// <inheritdoc />
    public abstract int Order { get; }

    /// <inheritdoc />
    public virtual bool ShouldExecute(int termCode) => true;

    /// <inheritdoc />
    public abstract Task GeneratePreviewAsync(HarvestContext context, CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task ExecuteAsync(HarvestContext context, CancellationToken ct = default);

    #region Import Helpers

    /// <summary>
    /// Import a person into effort.Persons.
    /// De-duplicates by MothraId.
    /// </summary>
    protected async Task ImportPersonAsync(
        HarvestPersonPreview person,
        HarvestContext ctx,
        CancellationToken ct)
    {
        // De-duplicate by MothraId
        if (ctx.ImportedMothraIds.Contains(person.MothraId))
        {
            return;
        }

        // Check if person already exists in database
        var exists = await ctx.EffortContext.Persons
            .AsNoTracking()
            .AnyAsync(p => p.TermCode == ctx.TermCode && p.PersonId == person.PersonId, ct);

        if (exists)
        {
            ctx.ImportedMothraIds.Add(person.MothraId);
            return;
        }

        var effortPerson = new EffortPerson
        {
            PersonId = person.PersonId,
            TermCode = ctx.TermCode,
            FirstName = person.FirstName.ToUpperInvariant(),
            LastName = person.LastName.ToUpperInvariant(),
            MiddleInitial = null,
            EffortTitleCode = person.TitleCode,
            EffortDept = person.Department,
            PercentAdmin = 0,
            JobGroupId = !string.IsNullOrEmpty(person.TitleCode) ? ctx.JobGroupLookup?.GetValueOrDefault(person.TitleCode.Trim()) : null,
            Title = null,
            AdminUnit = null
        };

        ctx.EffortContext.Persons.Add(effortPerson);
        ctx.ImportedMothraIds.Add(person.MothraId);

        ctx.AuditService.AddHarvestPersonAudit(
            person.PersonId, ctx.TermCode, person.FirstName, person.LastName, person.Department);

        // Update progress
        ctx.InstructorsImported++;
        await ctx.ReportProgressAsync(PhaseName);
    }

    /// <summary>
    /// Import a course into effort.Courses.
    /// Returns the course ID (existing or newly created).
    /// </summary>
    protected async Task<int> ImportCourseAsync(
        HarvestCoursePreview course,
        HarvestContext ctx,
        CancellationToken ct)
    {
        // Check if course already exists. Match by (CRN, Units) when CRN is
        // present so variable-unit CRNs become one effort.Course row per unit
        // variant rather than collapsing to the first one.
        var existing = await ctx.EffortContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TermCode == ctx.TermCode &&
                c.Units == course.Units &&
                (string.IsNullOrWhiteSpace(course.Crn)
                    ? c.SubjCode == course.SubjCode &&
                      c.CrseNumb == course.CrseNumb &&
                      c.SeqNumb == course.SeqNumb
                    : c.Crn == course.Crn), ct);

        if (existing != null)
        {
            return existing.Id;
        }

        var effortCourse = new EffortCourse
        {
            Crn = course.Crn,
            TermCode = ctx.TermCode,
            SubjCode = course.SubjCode,
            CrseNumb = course.CrseNumb,
            SeqNumb = course.SeqNumb,
            Enrollment = course.Enrollment,
            Units = course.Units,
            CustDept = course.CustDept
        };

        ctx.EffortContext.Courses.Add(effortCourse);
        await ctx.EffortContext.SaveChangesAsync(ct);

        ctx.AuditService.AddHarvestCourseAudit(
            effortCourse.Id, ctx.TermCode, course.SubjCode, course.CrseNumb, course.Crn);

        // Update progress
        ctx.CoursesImported++;
        await ctx.ReportProgressAsync(PhaseName);

        return effortCourse.Id;
    }

    /// <summary>
    /// Import an effort record into effort.Records.
    /// Returns the created record and preview, or nulls if skipped.
    /// </summary>
    protected async Task<(EffortRecord? Record, HarvestRecordPreview? Preview)> ImportEffortRecordAsync(
        HarvestRecordPreview effort,
        HarvestContext ctx,
        CancellationToken ct)
    {
        // Skip effort records for persons who weren't imported
        if (!ctx.ImportedMothraIds.Contains(effort.MothraId))
        {
            ctx.Logger.LogDebug(
                "Skipping effort record for non-imported person: {MothraId}",
                LogSanitizer.SanitizeId(effort.MothraId));
            return (null, null);
        }

        // Find the course ID. Lookup keys carry units, but effort records only
        // carry the CRN, so for variable-unit CRNs we may match multiple keys
        // and link to the first variant (the instructor teaches one section
        // regardless of how students enrolled at different unit values).
        int courseId;
        if (!string.IsNullOrWhiteSpace(effort.Crn))
        {
            var crnPrefix = $"CRN:{effort.Crn}-";
            var crnMatches = ctx.CourseIdLookup
                .Where(kv => kv.Key.StartsWith(crnPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Value)
                .ToList();

            if (crnMatches.Count > 1)
            {
                ctx.Logger.LogDebug(
                    "Multiple unit variants for CRN {Crn}; linking effort to first variant",
                    LogSanitizer.SanitizeId(effort.Crn));
            }

            if (crnMatches.Count >= 1)
            {
                courseId = crnMatches[0];
            }
            else
            {
                courseId = -1; // sentinel; fall through to course-code fallback
            }
        }
        else
        {
            courseId = -1;
        }

        if (courseId < 0)
        {
            // Fall back to course code prefix match
            var courseKey = effort.CourseCode.Replace(" ", "") + "-";
            var matchingKeys = ctx.CourseIdLookup.Keys
                .Where(k => !k.StartsWith("CRN:") && k.StartsWith(courseKey))
                .ToList();

            if (matchingKeys.Count == 0)
            {
                ctx.Logger.LogWarning(
                    "Course not found for effort record: {CourseCode}",
                    LogSanitizer.SanitizeId(effort.CourseCode));
                return (null, null);
            }

            if (matchingKeys.Count > 1)
            {
                ctx.Logger.LogWarning(
                    "Multiple courses match effort record {CourseCode}: {MatchingKeys}. Skipping ambiguous match.",
                    LogSanitizer.SanitizeId(effort.CourseCode),
                    LogSanitizer.SanitizeString(string.Join(", ", matchingKeys)));
                return (null, null);
            }

            courseId = ctx.CourseIdLookup[matchingKeys[0]];
        }

        // Find the person ID
        var person = await ctx.ViperContext.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MothraId == effort.MothraId, ct);

        if (person == null)
        {
            ctx.Logger.LogWarning(
                "Person not found for effort record: {MothraId}",
                LogSanitizer.SanitizeId(effort.MothraId));
            return (null, null);
        }

        var effortRecord = new EffortRecord
        {
            CourseId = courseId,
            PersonId = person.PersonId,
            TermCode = ctx.TermCode,
            EffortTypeId = effort.EffortType,
            RoleId = effort.RoleId,
            Hours = effort.Hours,
            Weeks = effort.Weeks,
            Crn = effort.Crn,
            ModifiedDate = DateTime.Now,
            ModifiedBy = ctx.ModifiedBy
        };

        ctx.EffortContext.Records.Add(effortRecord);

        // Update progress
        ctx.RecordsImported++;
        await ctx.ReportProgressAsync(PhaseName);

        return (effortRecord, effort);
    }

    #endregion

    #region Key Building Helpers

    /// <summary>
    /// Builds a unique key for a course preview, used for deduplication and tracking.
    /// </summary>
    protected static string BuildCourseKey(HarvestCoursePreview course)
    {
        return string.IsNullOrWhiteSpace(course.Crn)
            ? $"{course.SubjCode}-{course.CrseNumb}-{course.SeqNumb}-{course.Units}"
            : $"CRN:{course.Crn}-{course.Units}";
    }

    /// <summary>
    /// Builds a course lookup key for the CourseIdLookup dictionary. Units are
    /// part of the key so variable-unit CRNs can map each unit variant to its
    /// own CourseId.
    /// </summary>
    protected static string BuildCourseLookupKey(HarvestCoursePreview course)
    {
        return string.IsNullOrWhiteSpace(course.Crn)
            ? $"{course.SubjCode}{course.CrseNumb}-{course.Units}"
            : $"CRN:{course.Crn}-{course.Units}";
    }

    #endregion

    #region Term Helpers

    /// <summary>
    /// Check if a term code represents a semester term (not a quarter term).
    /// Clinical scheduler data is only imported for semester terms.
    /// </summary>
    protected static bool IsSemesterTerm(int termCode)
    {
        // Term code format: YYYYTT where TT is the term type
        // Semester terms: 2 (Spring), 4 (Summer), 9 (Fall)
        var termType = termCode % 100;
        return termType == 2 || termType == 4 || termType == 9;
    }

    #endregion

    #region Department Resolution

    /// <summary>
    /// Resolve department code to simple name using the lookup dictionary.
    /// </summary>
    protected static string? ResolveDeptSimpleName(string? deptCode, Dictionary<string, string>? lookup)
    {
        if (string.IsNullOrWhiteSpace(deptCode) || lookup == null)
        {
            return null;
        }

        var trimmed = deptCode.Trim();
        return lookup.TryGetValue(trimmed, out var simpleName) ? simpleName : null;
    }

    /// <summary>
    /// Resolve the effort department for an instructor using the instructor service.
    /// </summary>
    protected static async Task<string> ResolveDepartmentAsync(
        int personId,
        HarvestContext ctx,
        CancellationToken ct)
    {
        var dept = await ctx.InstructorService.ResolveInstructorDepartmentAsync(personId, ctx.TermCode, ct);
        return dept ?? "UNK";
    }

    #endregion
}
