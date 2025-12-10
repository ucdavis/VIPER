namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents the workflow status for a term in the effort system.
/// Maps to effort.TermStatus table.
/// Term metadata (name, dates) comes from VIPER.dbo.vwTerms.
/// </summary>
public class EffortTerm
{
    public int TermCode { get; set; }
    public string Status { get; set; } = "Created";
    public DateTime? HarvestedDate { get; set; }
    public DateTime? OpenedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public int ModifiedBy { get; set; }

    // Navigation properties
    public virtual ICollection<EffortPerson> Persons { get; set; } = new List<EffortPerson>();
    public virtual ICollection<EffortCourse> Courses { get; set; } = new List<EffortCourse>();
    public virtual ICollection<EffortRecord> Records { get; set; } = new List<EffortRecord>();
}
