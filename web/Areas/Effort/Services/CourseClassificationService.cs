using System.Text.RegularExpressions;
using Viper.Areas.Effort.Models;
using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for classifying courses in the effort system.
/// Provides centralized logic for identifying course types and determining
/// which effort types are allowed based on course characteristics.
/// </summary>
public class CourseClassificationService : ICourseClassificationService
{
    private static readonly string[] DvmSubjectCodes = { "DVM" };

    /// <summary>
    /// Checks if the subject code is a DVM course.
    /// </summary>
    public bool IsDvmCourse(string? subjCode)
    {
        if (string.IsNullOrWhiteSpace(subjCode)) return false;
        return DvmSubjectCodes.Contains(subjCode.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if course is a 199/299 course.
    /// Handles suffixed course numbers like "199A", "299R", "199", "299".
    /// Pattern requires 199/299 followed by non-digit or end of string
    /// to avoid false positives like "1990" or "2995".
    /// </summary>
    public bool Is199299Course(string? crseNumb)
    {
        if (string.IsNullOrEmpty(crseNumb)) return false;
        // Match 199 or 299 followed by non-digit or end of string
        // Valid: "199", "299", "199A", "299R", "199B"
        // Invalid: "1990", "2995", "19900"
        return Regex.IsMatch(crseNumb.Trim(), @"^(199|299)([^0-9]|$)", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Checks if the course number ends with 'R' (resident course).
    /// </summary>
    public bool IsRCourse(string? crseNumb)
    {
        if (string.IsNullOrWhiteSpace(crseNumb)) return false;
        return crseNumb.Trim().EndsWith("R", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if this is the generic auto-generated R-course (CRN = "RESID").
    /// </summary>
    public bool IsGenericRCourse(string? crn)
    {
        if (string.IsNullOrEmpty(crn)) return false;
        return string.Equals(crn, "RESID", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Classifies a course and returns all classification flags.
    /// This is the primary method used throughout the system to get course classification.
    /// </summary>
    public CourseClassification Classify(EffortCourse course)
    {
        return new CourseClassification
        {
            IsDvmCourse = IsDvmCourse(course.SubjCode),
            Is199299Course = Is199299Course(course.CrseNumb),
            IsRCourse = IsRCourse(course.CrseNumb),
            IsGenericRCourse = IsGenericRCourse(course.Crn)
        };
    }
}
