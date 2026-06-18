namespace Viper.Areas.Eval.Models.Entities;

public class Template
{
    public int TemplateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Course { get; set; }
    public int? Crn { get; set; }
    public int? TermCode { get; set; }
    public string? Type { get; set; }
    public int? Order { get; set; }
    public bool? Active { get; set; }
    public DateTime? LastEdit { get; set; }
    public string? LastEditMothraId { get; set; }
    public bool Approved { get; set; }
    public int? TypeId { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
