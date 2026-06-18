namespace Viper.Areas.Eval.Models.Entities;

public class Term
{
    public int TermCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public DateTime? HarvestDate { get; set; }
    public int? GradYear { get; set; }
}
