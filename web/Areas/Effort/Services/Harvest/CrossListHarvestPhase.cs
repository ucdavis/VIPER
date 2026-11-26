using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services.Harvest;

/// <summary>
/// Phase 4: Link cross-listed courses.
/// Runs after every course and effort record is imported, so re-harvesting rebuilds the links.
/// </summary>
public sealed class CrossListHarvestPhase : HarvestPhaseBase
{
    private readonly ICrossListService _crossListService;

    public CrossListHarvestPhase(ICrossListService crossListService)
    {
        _crossListService = crossListService;
    }

    /// <inheritdoc />
    public override string PhaseName => "Cross-List";

    /// <inheritdoc />
    public override int Order => 40;

    /// <inheritdoc />
    public override async Task GeneratePreviewAsync(HarvestContext context, CancellationToken ct = default)
    {
        var courses = context.Preview.CrestCourses
            .Concat(context.Preview.NonCrestCourses.Where(c => c.Source != EffortConstants.SourceInCrest))
            .Concat(context.Preview.ClinicalCourses)
            .Select((c, index) => new CrossListCourse(index, c.Crn.Trim(), c.SubjCode.Trim(), c.CrseNumb.Trim(), c.SeqNumb.Trim(), c.Enrollment))
            .ToList();

        var links = await _crossListService.PlanAsync(context.TermCode, courses, ct: ct);

        context.Preview.CrossListLinks.AddRange(links.Select(l => new HarvestCrossListPreview
        {
            ParentCourse = l.Parent.Label,
            ChildCourse = l.Child.Label,
            RelationshipType = l.RelationshipType,
            Reason = l.PriorTermCode.HasValue
                ? $"Parent in {context.TermService.GetTermName(l.PriorTermCode.Value)}"
                : l.Reason
        }));
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync(HarvestContext context, CancellationToken ct = default)
    {
        var result = await _crossListService.ApplyAsync(context.TermCode, ct: ct);

        context.Warnings.AddRange(result.Conflicts.Select(conflict => new HarvestWarning
        {
            Phase = PhaseName,
            Message = "Effort left on a cross-listed child course",
            Details = conflict
        }));
    }
}
