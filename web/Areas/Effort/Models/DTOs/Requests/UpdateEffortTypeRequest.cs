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

    public bool UsesWeeks { get; set; }
    public bool IsActive { get; set; } = true;
    public bool FacultyCanEnter { get; set; } = true;
    public bool AllowedOnDvm { get; set; } = true;
    public bool AllowedOn199299 { get; set; } = true;
    public bool AllowedOnRCourses { get; set; } = true;
}
