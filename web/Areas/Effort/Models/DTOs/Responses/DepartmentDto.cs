namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for department lookup values with grouping.
/// </summary>
public class DepartmentDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
}
