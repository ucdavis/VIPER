namespace Viper.Areas.Effort.Services.Harvest;

/// <summary>
/// Interface for harvest phase implementations.
/// Each phase handles a specific data source (CREST, Non-CREST, Clinical, Guest).
/// </summary>
public interface IHarvestPhase
{
    /// <summary>
    /// Display name of this phase for logging and UI.
    /// </summary>
    string PhaseName { get; }

    /// <summary>
    /// Execution order (lower numbers execute first).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Determine if this phase should execute for the given term.
    /// For example, Clinical phase only runs for semester terms.
    /// </summary>
    /// <param name="termCode">The term code being harvested.</param>
    /// <returns>True if this phase should execute.</returns>
    bool ShouldExecute(int termCode);

    /// <summary>
    /// Generate preview data for this phase without saving.
    /// Populates the context's Preview with instructors, courses, and effort records.
    /// </summary>
    /// <param name="context">Shared harvest context.</param>
    /// <param name="ct">Cancellation token.</param>
    Task GeneratePreviewAsync(HarvestContext context, CancellationToken ct = default);

    /// <summary>
    /// Execute the import for this phase.
    /// Imports instructors, courses, and effort records to the database.
    /// </summary>
    /// <param name="context">Shared harvest context.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExecuteAsync(HarvestContext context, CancellationToken ct = default);
}
