namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents sabbatical exclusion terms for a person.
/// Maps to effort.Sabbaticals table.
/// </summary>
public class Sabbatical
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public string? ExcludeClinicalTerms { get; set; }
    public string? ExcludeDidacticTerms { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedBy { get; set; }
}
