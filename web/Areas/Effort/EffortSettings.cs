namespace Viper.Areas.Effort;

/// <summary>
/// Configuration settings for the Effort system.
/// </summary>
public class EffortSettings
{
    /// <summary>
    /// Email subject line for verification reminder emails.
    /// </summary>
    public string VerificationEmailSubject { get; set; } = "Action required, timely ask - Effort data verification";

    /// <summary>
    /// Number of days instructors have to respond to verification emails.
    /// </summary>
    public int VerificationReplyDays { get; set; } = 7;

    /// <summary>
    /// When true, the harvest and on-demand effort entry auto-create the generic
    /// "RES 000R-001" resident-teaching course (CRN "RESID") and its effort records.
    /// Defaults to false — the placeholder course is not currently in use.
    /// </summary>
    public bool AutoCreateGenericRCourse { get; set; }
}
