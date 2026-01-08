namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for session type data returned from the API.
/// </summary>
public class SessionTypeDto
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool UsesWeeks { get; set; }
    public bool IsActive { get; set; }
    public bool FacultyCanEnter { get; set; }
    public bool AllowedOnDvm { get; set; }
    public bool AllowedOn199299 { get; set; }
    public bool AllowedOnRCourses { get; set; }
    public int UsageCount { get; set; }
    public bool CanDelete { get; set; }
}
