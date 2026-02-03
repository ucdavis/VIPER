namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for available courses when creating effort records.
/// Separates courses into those with existing effort and all available courses.
/// </summary>
public class AvailableCoursesDto
{
    /// <summary>
    /// Courses that already have effort records for this instructor.
    /// </summary>
    public List<CourseOptionDto> ExistingCourses { get; set; } = new();

    /// <summary>
    /// All courses available for the term (excluding child courses).
    /// </summary>
    public List<CourseOptionDto> AllCourses { get; set; } = new();
}

/// <summary>
/// DTO for a course option in dropdowns.
/// </summary>
public class CourseOptionDto : ICourseClassificationFlags
{
    /// <summary>
    /// The course ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Subject code (e.g., "VET").
    /// </summary>
    public string SubjCode { get; set; } = string.Empty;

    /// <summary>
    /// Course number (e.g., "410").
    /// </summary>
    public string CrseNumb { get; set; } = string.Empty;

    /// <summary>
    /// Section/sequence number (e.g., "01").
    /// </summary>
    public string SeqNumb { get; set; } = string.Empty;

    /// <summary>
    /// Course units.
    /// </summary>
    public decimal Units { get; set; }

    /// <summary>
    /// Display label for the dropdown (e.g., "VET 410-01 (4 units)").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The course CRN.
    /// </summary>
    public string Crn { get; set; } = string.Empty;

    // ICourseClassificationFlags implementation
    public bool IsDvm { get; set; }
    public bool Is199299 { get; set; }
    public bool IsRCourse { get; set; }
}
