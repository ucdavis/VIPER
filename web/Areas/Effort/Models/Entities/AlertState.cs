namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Persisted state for data hygiene alerts.
/// Alert data is computed dynamically from EffortPerson/EffortCourse/EffortRecord tables.
/// This entity stores only the review/ignore state for each alert.
/// Maps to effort.AlertStates table.
/// </summary>
public class AlertState
{
    public int Id { get; set; }
    public int TermCode { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public int? IgnoredBy { get; set; }
    public DateTime? IgnoredDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public int? ModifiedBy { get; set; }

    // Navigation properties
    public virtual EffortTerm? Term { get; set; }
    public virtual ViperPerson? IgnoredByPerson { get; set; }
    public virtual ViperPerson? ModifiedByPerson { get; set; }
}
