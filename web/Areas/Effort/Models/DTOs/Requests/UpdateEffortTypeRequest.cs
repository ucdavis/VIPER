using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for updating an existing effort type.
/// Note: Id cannot be changed after creation.
/// </summary>
public class UpdateEffortTypeRequest
{
    [Required]
    [MaxLength(50)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Description cannot be empty or whitespace only.")]
    public string Description { get; set; } = string.Empty;

    public required bool UsesWeeks { get; set; }
    public required bool IsActive { get; set; }
    public required bool FacultyCanEnter { get; set; }
    public required bool AllowedOnDvm { get; set; }
    public required bool AllowedOn199299 { get; set; }
    public required bool AllowedOnRCourses { get; set; }
}
