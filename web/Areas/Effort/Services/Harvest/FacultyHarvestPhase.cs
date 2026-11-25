using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services.Harvest;

/// <summary>
/// Phase 4: adds active senate, federation, and teaching faculty from PPS who have no effort
/// in the earlier phases, so they can verify "no effort" for the term.
/// Runs last because it only adds people the other phases did not find.
/// </summary>
public sealed class FacultyHarvestPhase : HarvestPhaseBase
{
    /// <inheritdoc />
    public override string PhaseName => EffortConstants.PhaseFaculty;

    /// <inheritdoc />
    public override int Order => 40;

    /// <inheritdoc />
    public override async Task GeneratePreviewAsync(HarvestContext context, CancellationToken ct = default)
    {
        var excludedTitleCodes = context.ExcludedTitleCodes ??= await context.InstructorService.GetExcludedTitleCodesAsync(ct);

        // Emeriti hold active WOS jobs in PPS; drop them before anything else. The PPS title is
        // checked too because some PPS job codes are missing from the title dictionary the
        // excluded codes come from.
        var faculty = (await context.InstructorService.GetPpsFacultyAsync(ct))
            .Where(f => !excludedTitleCodes.Contains(f.JobCode)
                     && !f.JobTitle.Contains("emerit", StringComparison.OrdinalIgnoreCase)
                     && !f.JobTitle.Contains("recall", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var alreadyFound = context.Preview.CrestInstructors
            .Concat(context.Preview.NonCrestInstructors)
            .Concat(context.Preview.ClinicalInstructors)
            .Select(i => i.MothraId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = faculty
            .Where(f => f.MothraId != null && !alreadyFound.Contains(f.MothraId))
            .DistinctBy(f => f.MothraId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var mothraIds = candidates.Select(f => f.MothraId!).ToList();

        await EnsureTitleLookupsAsync(context, ct);
        context.DeptSimpleNameLookup ??= await context.InstructorService.GetDepartmentSimpleNameLookupAsync(ct);

        // Same title and department the other phases would give this person: AAUD for the
        // term. PPS is the fallback for someone with no AAUD term row, such as on leave.
        var termCodeStr = context.TermCode.ToString();
        var aaudTitles = (await context.AaudContext.Ids
            .AsNoTracking()
            .Where(i => i.IdsTermCode == termCodeStr && i.IdsMothraid != null && EF.Parameter(mothraIds).Contains(i.IdsMothraid))
            .Join(
                context.AaudContext.Employees.Where(e => e.EmpTermCode == termCodeStr),
                ids => ids.IdsPKey,
                emp => emp.EmpPKey,
                (ids, emp) => new { MothraId = ids.IdsMothraid!, emp.EmpEffortTitleCode })
            .ToListAsync(ct))
            .Where(a => !string.IsNullOrWhiteSpace(a.EmpEffortTitleCode))
            .GroupBy(a => a.MothraId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().EmpEffortTitleCode!.Trim(), StringComparer.OrdinalIgnoreCase);

        var aaudDepts = await context.InstructorService.BatchResolveDepartmentsAsync(mothraIds, context.TermCode, ct);

        var personIds = (await context.ViperContext.People
            .AsNoTracking()
            .Where(p => EF.Parameter(mothraIds).Contains(p.MothraId))
            .Select(p => new { p.PersonId, p.MothraId })
            .ToListAsync(ct))
            .GroupBy(p => p.MothraId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().PersonId, StringComparer.OrdinalIgnoreCase);

        var notFound = faculty
            .Where(f => f.MothraId == null)
            .Select(f => $"{f.LastName}, {f.FirstName}")
            .ToList();

        foreach (var member in candidates)
        {
            var mothraId = member.MothraId!;
            var fullName = $"{member.LastName}, {member.FirstName}";

            if (!personIds.TryGetValue(mothraId, out var personId))
            {
                notFound.Add(fullName);
                continue;
            }

            var titleCode = aaudTitles.GetValueOrDefault(mothraId, member.JobCode);
            if (excludedTitleCodes.Contains(titleCode))
            {
                continue;
            }

            var dept = aaudDepts.GetValueOrDefault(mothraId, "UNK");
            if (dept == "UNK")
            {
                dept = ResolveDeptSimpleName(member.DeptId, context.DeptSimpleNameLookup) ?? "UNK";
            }

            context.Preview.FacultyInstructors.Add(new HarvestPersonPreview
            {
                MothraId = mothraId,
                PersonId = personId,
                FullName = fullName,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Department = dept,
                TitleCode = titleCode,
                TitleDescription = context.TitleLookup!.GetValueOrDefault(
                    titleCode,
                    titleCode == member.JobCode && member.JobTitle.Length > 0 ? member.JobTitle : titleCode),
                Source = EffortConstants.SourceFaculty
            });
        }

        context.Preview.FacultyInstructors.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));

        if (notFound.Count > 0)
        {
            var warning = new HarvestWarning
            {
                Phase = PhaseName,
                Message = $"{notFound.Count} faculty not added: no matching VIPER person record",
                Details = string.Join("; ", notFound.Order())
            };
            context.Warnings.Add(warning);
            context.Preview.Warnings.Add(warning);
        }
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync(HarvestContext context, CancellationToken ct = default)
    {
        context.Logger.LogInformation(
            "Importing {Count} faculty with no effort for term {TermCode}",
            context.Preview.FacultyInstructors.Count,
            context.TermCode);

        foreach (var instructor in context.Preview.FacultyInstructors)
        {
            await ImportPersonAsync(instructor, context, ct);
        }
    }
}
