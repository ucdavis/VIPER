namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents effort percentage allocations for a person by academic year.
/// Maps to effort.Percentages table.
/// </summary>
public class Percentage
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public double PercentageValue { get; set; }
    public int PercentAssignTypeId { get; set; }
    public int? UnitId { get; set; }
    public string? Modifier { get; set; }
    public string? Comment { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public bool Compensated { get; set; }

    // Navigation properties
    public virtual PercentAssignType PercentAssignType { get; set; } = null!;
    public virtual Unit? Unit { get; set; }
}
