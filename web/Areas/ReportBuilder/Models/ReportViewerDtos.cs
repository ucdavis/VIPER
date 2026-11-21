namespace Viper.Areas.ReportBuilder.Models;

/// <summary>A report the user may view, for the list of reports.</summary>
public sealed record ReportCatalogItem
{
    /// <summary>The report's URL key.</summary>
    public required Guid Slug { get; init; }

    public required string Title { get; init; }
    public string? Description { get; init; }

    /// <summary>Groups reports in the list, such as "Effort" or "Personnel".</summary>
    public string? Area { get; init; }
}

/// <summary>What the viewer needs to show a report's form and table before running it.</summary>
public sealed record ReportViewerDefinition
{
    public required Guid Slug { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Area { get; init; }

    /// <summary>Shown above the table when the data is confidential.</summary>
    public string? SensitiveNotice { get; init; }

    /// <summary>Whether the table shows a page at a time.</summary>
    public required bool Paginate { get; init; }

    public required int PageSize { get; init; }
    public required bool AllowExcel { get; init; }
    public required bool AllowPdf { get; init; }

    /// <summary>The column the report is sorted by until the user picks another.</summary>
    public string? SortBy { get; init; }

    public required bool SortDescending { get; init; }
    public required IReadOnlyList<ReportResultColumn> Columns { get; init; }

    /// <summary>The parameters the user fills in; ones set from the signed-in user aren't listed.</summary>
    public required IReadOnlyList<ReportViewerParameter> Parameters { get; init; }
}

/// <summary>One field of a report's form.</summary>
public sealed record ReportViewerParameter
{
    /// <summary>The key to send the value under when running the report.</summary>
    public required string Name { get; init; }

    public required string Label { get; init; }
    public required ReportParameterType Type { get; init; }
    public required bool Required { get; init; }
    public string? DefaultValue { get; init; }

    /// <summary>The choices, for choice parameters; null for the others.</summary>
    public IReadOnlyList<ReportChoice>? Choices { get; init; }
}

/// <summary>A report's viewer definition, or why there is none.</summary>
public sealed record ReportDefinitionOutcome
{
    public required ReportRunStatus Status { get; init; }
    public ReportViewerDefinition? Definition { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>A download, ready to send.</summary>
public sealed record ReportFile
{
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}

/// <summary>A download, or why there is none. An Ok outcome without a file means the report had no rows.</summary>
public sealed record ReportExportOutcome
{
    public required ReportRunStatus Status { get; init; }
    public ReportFile? File { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}
