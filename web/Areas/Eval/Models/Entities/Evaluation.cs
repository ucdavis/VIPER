namespace Viper.Areas.Eval.Models.Entities;

public class Evaluation
{
    public int EvalId { get; set; }
    public string Course { get; set; } = string.Empty;
    public int? Crn { get; set; }
    public int TermCode { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime? OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public int? CourseTemplateId { get; set; }
    public int? SchoolTemplateId { get; set; }
    public bool? SelfEval { get; set; }
    public int? PeerTeamCollectionId { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public bool? External { get; set; }
    public int? RotId { get; set; }
    public int? ProgramId { get; set; }
    public bool? Residents { get; set; }
    public bool? Notify { get; set; }
}
