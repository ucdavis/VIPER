namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents report units for effort reporting hierarchy.
/// Maps to effort.ReportUnits table.
/// </summary>
public class ReportUnit
{
    public int Id { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public int? ParentUnitId { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SortOrder { get; set; }

    // Navigation properties
    public virtual ReportUnit? ParentUnit { get; set; }
    public virtual ICollection<ReportUnit> ChildUnits { get; set; } = new List<ReportUnit>();
}
