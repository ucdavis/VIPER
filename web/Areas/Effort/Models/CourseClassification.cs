namespace Viper.Areas.Effort.Models;

/// <summary>
/// Represents the classification flags for a course in the effort system.
/// Used to determine which effort types are allowed and how the course should be handled.
/// </summary>
public record CourseClassification
{
    /// <summary>
    /// True if the course subject code is DVM.
    /// </summary>
    public bool IsDvmCourse { get; init; }

    /// <summary>
    /// True if the course number is 199 or 299 (with or without suffix).
    /// </summary>
    public bool Is199299Course { get; init; }

    /// <summary>
    /// True if the course number ends with 'R' (e.g., 200R, 299R).
    /// </summary>
    public bool IsRCourse { get; init; }

    /// <summary>
    /// True if this is the generic auto-generated R-course (CRN = "RESID").
    /// </summary>
    public bool IsGenericRCourse { get; init; }
}
