namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents a course in the effort system.
/// Maps to effort.Courses table.
/// Course titles should be fetched from the VIPER course catalog.
/// </summary>
public class EffortCourse
{
    public int Id { get; set; }
    public string Crn { get; set; } = string.Empty;
    public int TermCode { get; set; }
    public string SubjCode { get; set; } = string.Empty;
    public string CrseNumb { get; set; } = string.Empty;
    public string SeqNumb { get; set; } = string.Empty;
    public int Enrollment { get; set; }
    public decimal Units { get; set; }
    public string CustDept { get; set; } = string.Empty;

    // Navigation properties
    public virtual EffortTerm Term { get; set; } = null!;
    public virtual ICollection<EffortRecord> Records { get; set; } = new List<EffortRecord>();
    public virtual ICollection<CourseRelationship> ParentRelationships { get; set; } = new List<CourseRelationship>();
    public virtual ICollection<CourseRelationship> ChildRelationships { get; set; } = new List<CourseRelationship>();
}
