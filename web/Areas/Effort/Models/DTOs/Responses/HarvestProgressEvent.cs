namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Progress event for SSE streaming during harvest.
/// </summary>
public class HarvestProgressEvent
{
    /// <summary>
    /// Event type: "progress", "complete", or "error".
    /// </summary>
    public string Type { get; set; } = "progress";

    /// <summary>
    /// Current phase: "clearing", "instructors", "courses", "records", "clinical", "finalizing".
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
    /// Current item count being processed (e.g., "150 of 266 instructors").
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Final result (only set when Type is "complete").
    /// </summary>
    public HarvestResultDto? Result { get; set; }

    /// <summary>
    /// Error message (only set when Type is "error").
    /// </summary>
    public string? Error { get; set; }

    // Factory methods for common events
    public static HarvestProgressEvent Clearing() => new()
    {
        Phase = "clearing",
        Progress = 0.05,
        Message = "Clearing existing data..."
    };

    public static HarvestProgressEvent ImportingInstructors(int current, int total) => new()
    {
        Phase = "instructors",
        Progress = 0.1 + (0.2 * current / Math.Max(total, 1)),
        Message = "Importing instructors...",
        Detail = $"{current} of {total} instructors"
    };

    public static HarvestProgressEvent ImportingCourses(int current, int total) => new()
    {
        Phase = "courses",
        Progress = 0.3 + (0.2 * current / Math.Max(total, 1)),
        Message = "Importing courses...",
        Detail = $"{current} of {total} courses"
    };

    public static HarvestProgressEvent ImportingRecords(int current, int total) => new()
    {
        Phase = "records",
        Progress = 0.5 + (0.25 * current / Math.Max(total, 1)),
        Message = "Importing effort records...",
        Detail = $"{current} of {total} records"
    };

    public static HarvestProgressEvent ImportingClinical(int current, int total) => new()
    {
        Phase = "clinical",
        Progress = 0.75 + (0.15 * current / Math.Max(total, 1)),
        Message = "Importing clinical data...",
        Detail = $"{current} of {total} clinical records"
    };

    public static HarvestProgressEvent Finalizing() => new()
    {
        Phase = "finalizing",
        Progress = 0.95,
        Message = "Finalizing harvest..."
    };

    public static HarvestProgressEvent Complete(HarvestResultDto result) => new()
    {
        Type = "complete",
        Phase = "complete",
        Progress = 1.0,
        Message = "Harvest complete!",
        Result = result
    };

    public static HarvestProgressEvent Failed(string error) => new()
    {
        Type = "error",
        Phase = "error",
        Progress = 0,
        Message = "Harvest failed",
        Error = error
    };
}
