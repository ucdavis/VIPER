namespace Viper.Areas.Eval.Models.Entities;

public class Evaluatee
{
    public int EvaluateeId { get; set; }
    public string MothraId { get; set; } = string.Empty;
    public int EvalId { get; set; }
    public int Position { get; set; }
    public string? Annotation { get; set; }
    public string? Type { get; set; }
    public string? TypeDetail { get; set; }
    public bool Required { get; set; }
    public DateTime? DueDate { get; set; }
    public int? PeerTeamId { get; set; }
    public int? StartWeekId { get; set; }
    public string? ExtName { get; set; }
    public string? ExtEmail { get; set; }
    public int? ExtPracticeId { get; set; }
    public Guid? ExtGuid { get; set; }
    public bool? Submitted { get; set; }
    public bool? Incomplete { get; set; }
    public bool? Reviewed { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? ReviewedMothraId { get; set; }
    public int? EvalLength { get; set; }
}
