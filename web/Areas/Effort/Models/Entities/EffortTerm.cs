namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents the workflow status for a term in the effort system.
/// Maps to effort.TermStatus table.
/// Term metadata (name, dates) comes from VIPER.dbo.vwTerms.
/// Status is derived from date fields (not stored) to stay in sync with legacy system.
/// </summary>
public class EffortTerm
{
    public int TermCode { get; set; }
    public DateTime? HarvestedDate { get; set; }
    public DateTime? OpenedDate { get; set; }
    public DateTime? ClosedDate { get; set; }

    /// <summary>
    /// Computed status derived from date fields (matches legacy ColdFusion logic).
    /// </summary>
    public string Status
    {
        get
        {
            if (ClosedDate.HasValue) return "Closed";
            if (OpenedDate.HasValue) return "Opened";
            if (HarvestedDate.HasValue) return "Harvested";
            return "Created";
        }
    }

    // Navigation properties
    public virtual ICollection<EffortPerson> Persons { get; set; } = new List<EffortPerson>();
    public virtual ICollection<EffortCourse> Courses { get; set; } = new List<EffortCourse>();
    public virtual ICollection<EffortRecord> Records { get; set; } = new List<EffortRecord>();
}
