namespace Viper.Areas.ReportBuilder.Models.Entities;

/// <summary>One run or export of a report: who, when, with which parameters, and how many rows.</summary>
public class ReportRunLog
{
    public long Id { get; set; }
    public int ReportDefinitionId { get; set; }
    public ReportRunAction Action { get; set; }
    public string LoginId { get; set; } = string.Empty;
    public DateTime RanOn { get; set; }

    /// <summary>The parameter values as JSON.</summary>
    public string? Parameters { get; set; }

    public int RowsReturned { get; set; }
    public int DurationMs { get; set; }
}
