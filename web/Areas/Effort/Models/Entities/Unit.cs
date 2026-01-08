namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents organizational units for effort percent assignment dropdowns.
/// Maps to effort.Units table.
/// </summary>
public class Unit
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
