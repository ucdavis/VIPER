using System.Globalization;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.Areas.ReportBuilder.Exporters;

/// <summary>Everything a download shows: the heading, the columns and the rows.</summary>
public sealed record ReportExportDocument
{
    public required string Title { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyList<ReportAppliedFilter> Filters { get; init; } = [];

    /// <summary>The report's confidentiality notice, repeated on every PDF page; null when it has none.</summary>
    public string? SensitiveNotice { get; init; }

    /// <summary>PDF page orientation.</summary>
    public bool Landscape { get; init; }

    public required IReadOnlyList<ReportResultColumn> Columns { get; init; }
    public required IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; }

    /// <summary>True when the row cap cut the report short.</summary>
    public bool Truncated { get; init; }
}

/// <summary>Wording shared by the Excel and PDF downloads, so both read the same.</summary>
public static class ReportExportText
{
    private static readonly CultureInfo UsEnglish = CultureInfo.GetCultureInfo("en-US");

    public static string Generated(DateTimeOffset generatedAt)
    {
        return "Generated " + generatedAt.ToString(ReportCellFormatter.DateTimeFormat, UsEnglish);
    }

    /// <summary>"Filters: Department: Pathology; Hired from: 07/01/2026", or null when nothing was filled in.</summary>
    public static string? Filters(IReadOnlyList<ReportAppliedFilter> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);
        return filters.Count == 0
            ? null
            : "Filters: " + string.Join("; ", filters.Select(filter => $"{filter.Label}: {filter.Value}"));
    }

    /// <summary>Said when the row cap cut the report short.</summary>
    public static string Truncated()
    {
        return string.Create(UsEnglish, $"Only the first {ReportLimits.MaxRows:N0} rows are included.");
    }
}
