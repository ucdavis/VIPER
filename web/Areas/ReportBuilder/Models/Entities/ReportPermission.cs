namespace Viper.Areas.ReportBuilder.Models.Entities;

/// <summary>A RAPS permission that lets a user view a report (<c>report.report_permission</c>). Holding any one is enough.</summary>
public class ReportPermission
{
    public int ReportId { get; set; }
    public string Permission { get; set; } = string.Empty;
    public ReportDefinition? Report { get; set; }
}
