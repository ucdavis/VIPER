namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents percent assignment type classifications (e.g., Teaching, Research, Admin).
/// Maps to effort.PercentAssignTypes table.
/// </summary>
public class PercentAssignType
{
    public int Id { get; set; }
    public string Class { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool ShowOnTemplate { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Percentage> Percentages { get; set; } = new List<Percentage>();
}
