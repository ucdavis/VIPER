namespace Viper.Areas.ReportBuilder.Models.Entities;

/// <summary>A saved copy of a report, taken on every change (<c>report.report_history</c>).</summary>
public class ReportDefinitionHistory
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public ReportHistoryAction? Action { get; set; }

    /// <summary>The whole report, with its columns, parameters and permissions, as JSON.</summary>
    public string? Snapshot { get; set; }

    public DateTime? ChangedOn { get; set; }
    public string? ChangedBy { get; set; }
}
