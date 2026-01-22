namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for role dropdown options.
/// </summary>
public class RoleOptionDto
{
    /// <summary>
    /// The role ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The role description (e.g., "Instructor", "Instructor of Record").
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
