namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Result of a self-service course import operation.
/// </summary>
public class ImportCourseForSelfResult
{
    /// <summary>
    /// Whether the import was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the import failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The imported or existing course. Null if import failed.
    /// </summary>
    public CourseDto? Course { get; set; }

    /// <summary>
    /// True if the course already existed (idempotent import).
    /// </summary>
    public bool WasExisting { get; set; }

    /// <summary>
    /// User-friendly message about the import result.
    /// </summary>
    public string? Message { get; set; }

    public static ImportCourseForSelfResult SuccessNew(CourseDto course)
    {
        return new ImportCourseForSelfResult
        {
            Success = true,
            Course = course,
            WasExisting = false,
            Message = "Course imported successfully"
        };
    }

    public static ImportCourseForSelfResult SuccessExisting(CourseDto course)
    {
        return new ImportCourseForSelfResult
        {
            Success = true,
            Course = course,
            WasExisting = true,
            Message = "Course was already imported"
        };
    }

    public static ImportCourseForSelfResult Failure(string error)
    {
        return new ImportCourseForSelfResult
        {
            Success = false,
            Error = error
        };
    }
}
