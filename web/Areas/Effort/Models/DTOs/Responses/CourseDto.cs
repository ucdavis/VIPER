namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for course information in the Effort system.
/// </summary>
public class CourseDto : ICourseClassificationFlags
{
    public int Id { get; set; }
    public string Crn { get; set; } = string.Empty;
    public int TermCode { get; set; }
    public string SubjCode { get; set; } = string.Empty;
    public string CrseNumb { get; set; } = string.Empty;
    public string SeqNumb { get; set; } = string.Empty;

    /// <summary>
    /// Combined course identifier (e.g., "VET 410").
    /// </summary>
    public string CourseCode => $"{SubjCode.Trim()} {CrseNumb.Trim()}";

    public int Enrollment { get; set; }
    public decimal Units { get; set; }
    public string CustDept { get; set; } = string.Empty;

    /// <summary>
    /// Parent course ID if this course is linked as a child. Null if not a child.
    /// Used to determine if the link button should be hidden (child courses cannot become parents).
    /// </summary>
    public int? ParentCourseId { get; set; }

    // ICourseClassificationFlags implementation
    // Populated by CourseClassificationService.Classify() to ensure consistency with backend validation
    public bool IsDvm { get; set; }
    public bool Is199299 { get; set; }
    public bool IsRCourse { get; set; }
    public bool IsGenericRCourse { get; set; }
}
