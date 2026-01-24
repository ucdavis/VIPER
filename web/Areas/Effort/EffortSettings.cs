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
    /// From address for verification emails.
    /// </summary>
    public string VerificationEmailFrom { get; set; } = "svmeffort@ucdavis.edu";

    /// <summary>
    /// Base URL for the Effort application (used in verification email links).
    /// If not set, defaults to the current request's host.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Number of days instructors have to respond to verification emails.
    /// </summary>
    public int VerificationReplyDays { get; set; } = 7;
}
