namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents organizational units for effort reporting.
/// Maps to effort.Units table.
/// </summary>
public class Unit
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SortOrder { get; set; }
}
