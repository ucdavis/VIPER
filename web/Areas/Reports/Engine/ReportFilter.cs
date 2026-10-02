namespace Viper.Areas.Reports.Engine;

/// <summary>
/// One parameter as the user chose it, for example "Faculty type: Senate", shown in export headers
/// so a downloaded file records how it was produced.
/// </summary>
public sealed record ReportFilter(string Label, string Value);
