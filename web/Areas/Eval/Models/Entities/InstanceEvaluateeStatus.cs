namespace Viper.Areas.Eval.Models.Entities;

public class InstanceEvaluateeStatus
{
    public int InstEvalStatusId { get; set; }
    public int InstanceId { get; set; }
    public int EvaluateeId { get; set; }
    public int EvalId { get; set; }
    public string? Status { get; set; }
    public DateTime? LastModified { get; set; }
    public bool? InsufficientContact { get; set; }
    public bool? Submitted { get; set; }
    public bool? PrimaryEvaluator { get; set; }
    public DateTime? SubmitDate { get; set; }
    public DateTime? RemindedDate { get; set; }
}
