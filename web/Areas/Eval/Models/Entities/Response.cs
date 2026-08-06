namespace Viper.Areas.Eval.Models.Entities;

public class Response
{
    public int ResponseId { get; set; }
    public string? Content { get; set; }
    public int QuestionId { get; set; }
    public int InstanceId { get; set; }
    public int? EvaluateeId { get; set; }
    public string? Comment { get; set; }
}
