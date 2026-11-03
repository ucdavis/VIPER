namespace Viper.Areas.ReportBuilder.Models.Entities;

/// <summary>A saved copy of a report definition, taken on every change.</summary>
public class ReportDefinitionHistory
{
    public int Id { get; set; }
    public int ReportDefinitionId { get; set; }
    public ReportHistoryAction Action { get; set; }

    /// <summary>The whole definition, with its columns, parameters and permissions, as JSON.</summary>
    public string Snapshot { get; set; } = string.Empty;

    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedOn { get; set; }
}
