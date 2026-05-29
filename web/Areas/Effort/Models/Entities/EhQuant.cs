namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents quantitative evaluation data in the evalHarvest database.
/// Maps to dbo.eh_Quant table.
/// </summary>
public class EhQuant
{
    public int QuantId { get; set; }
    public int QuestionIdFk { get; set; }
    public string? MailId { get; set; }
    public int NoOpN { get; set; }
    public double NoOpP { get; set; }
    public int Count1N { get; set; }
    public int Count2N { get; set; }
    public int Count3N { get; set; }
    public int Count4N { get; set; }
    public int Count5N { get; set; }
    public double Count1P { get; set; }
    public double Count2P { get; set; }
    public double Count3P { get; set; }
    public double Count4P { get; set; }
    public double Count5P { get; set; }
    public double Mean { get; set; }
    public double Sd { get; set; }
    public int Enrolled { get; set; }
    public int Respondents { get; set; }
    public string? EvaluateeType { get; set; }

    // Navigation properties
    public virtual EhQuestion Question { get; set; } = null!;
}
