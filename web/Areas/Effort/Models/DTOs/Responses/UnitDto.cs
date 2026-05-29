namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for unit data returned from the API.
/// </summary>
public class UnitDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }
    public bool CanDelete { get; set; }
}
