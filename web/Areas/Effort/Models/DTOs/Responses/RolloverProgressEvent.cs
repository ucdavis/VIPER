namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Progress event for SSE streaming during percent assignment rollover.
/// </summary>
public class RolloverProgressEvent
{
    /// <summary>
    /// Event type: "progress", "complete", or "error".
    /// </summary>
    public string Type { get; set; } = "progress";

    /// <summary>
    /// Current phase: "preparing", "rolling", "finalizing".
    /// </summary>
    public string Phase { get; set; } = string.Empty;

    /// <summary>
    /// Progress within current phase (0.0 to 1.0).
    /// </summary>
    public double Progress { get; set; }

    /// <summary>
    /// Human-readable message for current operation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Current item count being processed (e.g., "15 of 42 assignments").
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Final result (only set when Type is "complete").
    /// </summary>
    public RolloverResultDto? Result { get; set; }

    /// <summary>
    /// Error message (only set when Type is "error").
    /// </summary>
    public string? Error { get; set; }

    // Factory methods for common events
    public static RolloverProgressEvent Preparing() => new()
    {
        Phase = "preparing",
        Progress = 0.1,
        Message = "Preparing rollover..."
    };

    public static RolloverProgressEvent Rolling(int current, int total) => new()
    {
        Phase = "rolling",
        Progress = 0.1 + (0.8 * current / Math.Max(total, 1)),
        Message = "Rolling over percent assignments...",
        Detail = $"{current} of {total} assignments"
    };

    public static RolloverProgressEvent Finalizing() => new()
    {
        Phase = "finalizing",
        Progress = 0.95,
        Message = "Finalizing rollover..."
    };

    public static RolloverProgressEvent Complete(RolloverResultDto result) => new()
    {
        Type = "complete",
        Phase = "complete",
        Progress = 1.0,
        Message = "Rollover complete!",
        Result = result
    };

    public static RolloverProgressEvent Failed(string error) => new()
    {
        Type = "error",
        Phase = "error",
        Progress = 0,
        Message = "Rollover failed",
        Error = error
    };
}

/// <summary>
/// Result of percent assignment rollover execution.
/// </summary>
public class RolloverResultDto
{
    public bool Success { get; set; }
    public int AssignmentsCreated { get; set; }
    public string SourceAcademicYear { get; set; } = string.Empty;
    public string TargetAcademicYear { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
