namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents alternate titles for persons in the effort system.
/// Maps to effort.AlternateTitles table.
/// </summary>
public class AlternateTitle
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public string AlternateTitleText { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ViperPerson? ViperPerson { get; set; }
    public virtual ViperPerson? ModifiedByPerson { get; set; }
}
