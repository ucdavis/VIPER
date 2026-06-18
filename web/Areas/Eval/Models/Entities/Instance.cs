namespace Viper.Areas.Eval.Models.Entities;

public class Instance
{
    public int InstanceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastModified { get; set; }
    public DateTime Created { get; set; }
    public int EvalId { get; set; }
    public string? Mode { get; set; }
    public string MothraId { get; set; } = string.Empty;
    public string? CourseQuestionsStatus { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? StartWeek { get; set; }
    public int? PeerTeamId { get; set; }
    public int? StartWeekId { get; set; }
    public DateTime? RemindedDate { get; set; }
}
