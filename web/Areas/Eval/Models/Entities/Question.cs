namespace Viper.Areas.Eval.Models.Entities;

public class Question
{
    public int QuestionId { get; set; }
    public int ResponseTypeId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? Type { get; set; }
    public string? TypeDetail { get; set; }
    public bool Overall { get; set; }
    public int TemplateId { get; set; }
    public string? Header { get; set; }
    public string? AuthorMothraId { get; set; }
    public bool? Required { get; set; }
    public int? ParentId { get; set; }
    public string? Comment { get; set; }
    public int? SisHeading { get; set; }
}
