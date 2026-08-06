namespace Viper.Areas.Eval.Models.Entities;

public class ResponseType
{
    public int ResponseTypeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool? InlineLabels { get; set; }
}
