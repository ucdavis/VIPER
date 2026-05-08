// ReSharper disable UnusedAutoPropertyAccessor.Global
// Property getters are read by System.Text.Json when this DTO is serialized
// to API responses; ReSharper cannot see that reflection-based usage.
namespace Viper.Areas.Scheduler.Models.DTOs.Responses;

/// <summary>
/// Result of a pause or resume operation. Carries the post-operation state so
/// callers can update their UI without a follow-up GET.
/// </summary>
public class PauseResumeResultDto
{
    /// <summary>Recurring-job id that was acted on.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Final paused state after the operation completed.</summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// True when the deregistration leg of pause failed and the reconciler
    /// will finish the job (HTTP 202). Always false on resume.
    /// </summary>
    public bool DeregistrationPending { get; set; }

    /// <summary>Updated rowversion (base64) for follow-up calls; null when not paused.</summary>
    public string? RowVersion { get; set; }
}
