namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents an effort record linking a person to a course with time allocation.
/// Maps to effort.Records table.
/// Hours OR Weeks is populated depending on EffortType (CLI uses Weeks).
/// </summary>
public class EffortRecord
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int PersonId { get; set; }
    public int TermCode { get; set; }
    public string EffortTypeId { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int? Hours { get; set; }
    public int? Weeks { get; set; }
    public string Crn { get; set; } = string.Empty;
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedBy { get; set; }

    // Navigation properties
    public virtual EffortCourse Course { get; set; } = null!;
    public virtual EffortPerson Person { get; set; } = null!;
    public virtual EffortTerm Term { get; set; } = null!;
    public virtual EffortRole RoleNavigation { get; set; } = null!;
    public virtual EffortType EffortTypeNavigation { get; set; } = null!;
    public virtual ViperPerson? ViperPerson { get; set; }
    public virtual ViperPerson? ModifiedByPerson { get; set; }
}
