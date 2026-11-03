namespace Viper.Areas.ReportBuilder.Models.Entities;

/// <summary>A RAPS permission that lets a user view a report. Holding any one is enough.</summary>
public class ReportPermission
{
    public int ReportDefinitionId { get; set; }
    public string Permission { get; set; } = string.Empty;
    public ReportDefinition? ReportDefinition { get; set; }
}
