using Viper.Areas.Effort.Models;
using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for classifying courses in the effort system.
/// Provides methods to identify course types and determine which effort types are allowed.
/// </summary>
public interface ICourseClassificationService
{
    /// <summary>
    /// Checks if the subject code is a DVM or VET course.
    /// </summary>
    /// <param name="subjCode">The subject code to check (e.g., "DVM", "VET", "BIS").</param>
    /// <returns>True if the subject code is DVM or VET.</returns>
    bool IsDvmCourse(string? subjCode);

    /// <summary>
    /// Checks if the course number is 199 or 299 (with or without suffix).
    /// Handles suffixed course numbers like "199A", "299R", "199", "299".
    /// Pattern requires 199/299 followed by non-digit or end of string
    /// to avoid false positives like "1990" or "2995".
    /// </summary>
    /// <param name="crseNumb">The course number to check (e.g., "199", "299A", "200").</param>
    /// <returns>True if the course number is 199 or 299 with optional suffix.</returns>
    bool Is199299Course(string? crseNumb);

    /// <summary>
    /// Checks if the course number ends with 'R' (resident course).
    /// </summary>
    /// <param name="crseNumb">The course number to check (e.g., "200R", "299R").</param>
    /// <returns>True if the course number ends with 'R'.</returns>
    bool IsRCourse(string? crseNumb);

    /// <summary>
    /// Checks if this is the generic auto-generated R-course.
    /// </summary>
    /// <param name="crn">The course reference number to check.</param>
    /// <returns>True if the CRN is "RESID".</returns>
    bool IsGenericRCourse(string? crn);

    /// <summary>
    /// Checks if the course is allowed for self-import by instructors.
    /// Instructors cannot self-import DVM/VET courses.
    /// </summary>
    /// <param name="subjCode">The subject code to check.</param>
    /// <returns>True if the course can be self-imported by instructors.</returns>
    bool IsAllowedForSelfImport(string? subjCode);

    /// <summary>
    /// Classifies a course and returns all classification flags.
    /// </summary>
    /// <param name="course">The course to classify.</param>
    /// <returns>A record containing all classification flags for the course.</returns>
    CourseClassification Classify(EffortCourse course);
}
