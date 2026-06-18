namespace Viper.Areas.Eval.Models.Entities;

public class TeamCollection
{
    public int CollectionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? ClassYear { get; set; }
    public string? ClassLevel { get; set; }
    public bool? StandardSections { get; set; }
}
