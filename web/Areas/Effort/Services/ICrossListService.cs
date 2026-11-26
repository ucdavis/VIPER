namespace Viper.Areas.Effort.Services;

/// <summary>
/// Links cross-listed courses from Banner's cross-list groups.
/// Shared by the harvest and the Banner course import.
/// </summary>
public interface ICrossListService
{
    /// <summary>
    /// Work out the parent/child links Banner implies for the given courses, without saving.
    /// </summary>
    /// <param name="termCode">The term the courses belong to.</param>
    /// <param name="courses">Courses to link, with trimmed codes.</param>
    /// <param name="existingParentIds">Courses that are already parents in the term; they stay parents.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<CrossListLink>> PlanAsync(int termCode, IReadOnlyCollection<CrossListCourse> courses,
        IReadOnlySet<int>? existingParentIds = null, CancellationToken ct = default);

    /// <summary>
    /// Create the missing links for a term and move child effort onto the parent.
    /// </summary>
    /// <param name="termCode">The term to link.</param>
    /// <param name="onlyCrn">When set, only links involving this CRN are created.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<CrossListResult> ApplyAsync(int termCode, string? onlyCrn = null, CancellationToken ct = default);
}

/// <summary>
/// A course as the cross-list matcher sees it. Callers pass trimmed values.
/// </summary>
public sealed record CrossListCourse(int Id, string Crn, string SubjCode, string CrseNumb, string SeqNumb, int Enrollment)
{
    public string CourseCode => $"{SubjCode} {CrseNumb}";

    public string Label => $"{CourseCode}-{SeqNumb}";
}

/// <summary>
/// A planned parent/child link and why the parent was chosen.
/// </summary>
public sealed record CrossListLink(CrossListCourse Parent, CrossListCourse Child, string RelationshipType,
    string Reason, int? PriorTermCode = null);

/// <summary>
/// Links created by <see cref="ICrossListService.ApplyAsync"/>, plus child effort that could not be merged.
/// </summary>
public sealed record CrossListResult(List<CrossListLink> Links, List<string> Conflicts);
