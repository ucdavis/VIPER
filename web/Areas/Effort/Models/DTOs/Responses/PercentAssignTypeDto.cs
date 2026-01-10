namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for percent assignment type information (Admin, Clinical, Other classifications).
/// </summary>
public class PercentAssignTypeDto
{
    public int Id { get; set; }
    public string Class { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool ShowOnTemplate { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// Count of instructors who have this type assigned.
    /// </summary>
    public int InstructorCount { get; set; }
}
