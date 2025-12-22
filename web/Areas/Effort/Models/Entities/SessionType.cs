namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents a session type (e.g., LEC, LAB, CLI).
/// Maps to effort.SessionTypes table.
/// CLI is the only session type that uses weeks instead of hours.
/// </summary>
public class SessionType
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool UsesWeeks { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<EffortRecord> Records { get; set; } = new List<EffortRecord>();
}
