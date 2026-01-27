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
    /// Base URL for the Effort application (used in verification email links).
    /// Required in production. A fallback is configured for development only.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Number of days instructors have to respond to verification emails.
    /// </summary>
    public int VerificationReplyDays { get; set; } = 7;
}
