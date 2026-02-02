using Viper.EmailTemplates.Models;

namespace Viper.Areas.ClinicalScheduler.EmailTemplates.Models;

/// <summary>
/// View model for the primary evaluator removed notification email.
/// </summary>
public class PrimaryEvaluatorRemovedViewModel : EmailViewModelBase
{
    /// <summary>
    /// Name of the instructor who was removed as primary evaluator.
    /// </summary>
    public string InstructorName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the rotation.
    /// </summary>
    public string RotationName { get; set; } = string.Empty;

    /// <summary>
    /// URL to the rotation detail page.
    /// </summary>
    public string RotationLink { get; set; } = string.Empty;

    /// <summary>
    /// Week number (e.g., "12").
    /// </summary>
    public string WeekNumber { get; set; } = string.Empty;

    /// <summary>
    /// Name of the person who made the change.
    /// </summary>
    public string ModifierName { get; set; } = string.Empty;

    /// <summary>
    /// Email of the person who made the change (optional).
    /// </summary>
    public string? ModifierEmail { get; set; }

    /// <summary>
    /// Whether this week requires a primary evaluator (shows warning if true).
    /// </summary>
    public bool RequiresPrimaryEvaluator { get; set; }
}
