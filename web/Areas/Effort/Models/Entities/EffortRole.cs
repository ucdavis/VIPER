namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents a role in the effort system (e.g., Instructor, Co-Instructor).
/// Maps to effort.Roles table.
/// </summary>
public class EffortRole
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int? SortOrder { get; set; }

    // Navigation properties
    public virtual ICollection<EffortRecord> Records { get; set; } = new List<EffortRecord>();
}
