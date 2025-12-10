namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents a person's effort data for a specific term.
/// Maps to effort.Persons table.
/// PersonId references users.Person for identity.
/// </summary>
public class EffortPerson
{
    public int PersonId { get; set; }
    public int TermCode { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleInitial { get; set; }
    public string EffortTitleCode { get; set; } = string.Empty;
    public string EffortDept { get; set; } = string.Empty;
    public decimal PercentAdmin { get; set; }
    public string? JobGroupId { get; set; }
    public string? Title { get; set; }
    public string? AdminUnit { get; set; }
    public DateTime? EffortVerified { get; set; }
    public string? ReportUnit { get; set; }
    public byte? VolunteerWos { get; set; }
    public decimal? PercentClinical { get; set; }

    // Navigation properties
    public virtual EffortTerm Term { get; set; } = null!;
    public virtual ICollection<EffortRecord> Records { get; set; } = new List<EffortRecord>();
}
