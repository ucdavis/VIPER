namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for job group lookup options in the Edit Instructor dialog.
/// </summary>
public class JobGroupDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
