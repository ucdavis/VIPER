namespace Viper.Areas.ReportBuilder.Models.Entities;

/// <summary>One column of a report: which source column it shows, its label, order and format.</summary>
public class ReportColumn
{
    public int Id { get; set; }
    public int ReportDefinitionId { get; set; }
    public string SourceColumn { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool Visible { get; set; } = true;
    public ReportColumnFormat Format { get; set; }
    public bool Sortable { get; set; } = true;
    public ReportDefinition? ReportDefinition { get; set; }
}
