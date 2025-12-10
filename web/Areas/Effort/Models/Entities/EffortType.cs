namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents effort type classifications (e.g., Teaching, Research, Admin).
/// Maps to effort.EffortTypes table.
/// </summary>
public class EffortType
{
    public int Id { get; set; }
    public string Class { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool ShowOnTemplate { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Percentage> Percentages { get; set; } = new List<Percentage>();
}
