using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for creating a new effort record.
/// </summary>
public class CreateEffortRecordRequest
{
    /// <summary>
    /// The person (instructor) ID.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Person ID is required")]
    public int PersonId { get; set; }

    /// <summary>
    /// The term code (e.g., 202510 for Fall 2025).
    /// Must be a valid 6-digit term code.
    /// </summary>
    [Range(100000, 999999, ErrorMessage = "Term code must be a valid 6-digit number")]
    public int TermCode { get; set; }

    /// <summary>
    /// The course ID.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Course is required")]
    public int CourseId { get; set; }

    /// <summary>
    /// The effort type ID (e.g., "LEC", "LAB", "CLI").
    /// </summary>
    [Required(ErrorMessage = "Effort type is required")]
    public string EffortTypeId { get; set; } = string.Empty;

    /// <summary>
    /// The role ID.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Role is required")]
    public int RoleId { get; set; }

    /// <summary>
    /// The effort value (hours or weeks depending on effort type and term).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Effort value is required")]
    public int EffortValue { get; set; }
}
