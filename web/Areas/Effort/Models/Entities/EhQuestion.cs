namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents a question in the evalHarvest database.
/// Maps to dbo.eh_Questions table.
/// </summary>
public class EhQuestion
{
    public int QuestId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Order { get; set; }
    public int Crn { get; set; }
    public int TermCode { get; set; }
    public bool IsOverall { get; set; }
    public string? EvaluateeType { get; set; }
    public int? FacilitatorEvalId { get; set; }

    // Navigation properties
    public virtual ICollection<EhQuant> Quants { get; set; } = new List<EhQuant>();
}
