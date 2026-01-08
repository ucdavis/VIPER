using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for creating a new session type.
/// </summary>
public class CreateSessionTypeRequest
{
    [Required]
    [MaxLength(3)]
    [RegularExpression(@"^[A-Za-z0-9/-]{1,3}$", ErrorMessage = "Id must be 1-3 alphanumeric characters, /, or - (will be converted to uppercase)")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Description cannot be empty or whitespace only.")]
    public string Description { get; set; } = string.Empty;

    public bool UsesWeeks { get; set; } = false;

    public bool FacultyCanEnter { get; set; } = true;
    public bool AllowedOnDvm { get; set; } = true;
    public bool AllowedOn199299 { get; set; } = true;
    public bool AllowedOnRCourses { get; set; } = true;
}
