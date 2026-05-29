namespace Viper.Areas.Effort.Models.DTOs;

/// <summary>
/// Interface for DTOs that include course classification flags.
/// Ensures consistent flag definitions across CourseDto and CourseOptionDto.
/// Flags are populated by CourseClassificationService.Classify() for consistency.
/// </summary>
public interface ICourseClassificationFlags
{
    /// <summary>
    /// True if this is a DVM course (SubjCode is "DVM").
    /// </summary>
    bool IsDvm { get; set; }

    /// <summary>
    /// True if this is a 199/299 course (course number starts with 199 or 299).
    /// </summary>
    bool Is199299 { get; set; }

    /// <summary>
    /// True if this is an R-course (course number ends with "R").
    /// </summary>
    bool IsRCourse { get; set; }

    /// <summary>
    /// True if this is the generic auto-generated R-course (CRN = "RESID").
    /// </summary>
    bool IsGenericRCourse { get; set; }
}

/// <summary>
/// Extension methods for ICourseClassificationFlags.
/// </summary>
public static class CourseClassificationFlagsExtensions
{
    /// <summary>
    /// Copies classification flags from a CourseClassification to the DTO.
    /// </summary>
    public static T WithClassification<T>(this T dto, CourseClassification classification)
        where T : ICourseClassificationFlags
    {
        dto.IsDvm = classification.IsDvmCourse;
        dto.Is199299 = classification.Is199299Course;
        dto.IsRCourse = classification.IsRCourse;
        dto.IsGenericRCourse = classification.IsGenericRCourse;
        return dto;
    }
}
