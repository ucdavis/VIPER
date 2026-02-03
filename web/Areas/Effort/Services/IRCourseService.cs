using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Specifies the context in which an R-course effort record was created.
/// Used for audit logging to distinguish between different creation scenarios.
/// </summary>
public enum RCourseCreationContext
{
    /// <summary>R-course created during the harvest process for all eligible instructors.</summary>
    Harvest,

    /// <summary>R-course created on-demand when an instructor adds their first non-R-course effort record.</summary>
    OnDemand
}

/// <summary>
/// Service interface for managing generic R-course (resident teaching) effort records.
/// Provides centralized logic for creating R-courses for instructors who teach non-R-course classes.
/// </summary>
public interface IRCourseService
{
    /// <summary>
    /// Gets or creates the generic R-course (CRN="RESID", RES 000R) for a term.
    /// This is the placeholder course used to record resident teaching effort.
    /// </summary>
    /// <param name="termCode">The term code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The existing or newly created generic R-course.</returns>
    Task<EffortCourse> GetOrCreateGenericRCourseAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Creates an R-course effort record for an instructor. Idempotent - does not create
    /// duplicate records if one already exists for the instructor in the given term.
    /// </summary>
    /// <param name="personId">The instructor's person ID.</param>
    /// <param name="termCode">The term code.</param>
    /// <param name="modifiedBy">The person ID of the user making the change.</param>
    /// <param name="context">The context in which the R-course is being created (for audit logging).</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateRCourseEffortRecordAsync(int personId, int termCode, int modifiedBy, RCourseCreationContext context, CancellationToken ct = default);
}
