namespace Viper.Areas.ReportBuilder.Models;

/// <summary>A view or procedure in the [report] schema that a report can read.</summary>
public sealed record ReportSourceSummary
{
    public required string Name { get; init; }
    public required ReportSourceKind Kind { get; init; }

    /// <summary>The object's MS_Description extended property, when it has one.</summary>
    public string? Description { get; init; }
}

/// <summary>A source with the columns it returns and, for procedures, the parameters it takes.</summary>
public sealed record ReportSourceDetail
{
    public required string Name { get; init; }
    public required ReportSourceKind Kind { get; init; }
    public string? Description { get; init; }
    public required IReadOnlyList<ReportSourceColumn> Columns { get; init; }
    public required IReadOnlyList<ReportSourceParameter> Parameters { get; init; }

    /// <summary>
    /// Why SQL Server couldn't describe a procedure's result columns (for example, it builds its
    /// result from a temp table). Null when the columns are known.
    /// </summary>
    public string? ColumnsError { get; init; }

    /// <summary>
    /// Whether the procedure takes @Skip and @Take, so the builder can page it in SQL like a view.
    /// Always true for views.
    /// </summary>
    public required bool PagesInSql { get; init; }
}

/// <summary>One column a source returns.</summary>
public sealed record ReportSourceColumn
{
    public required string Name { get; init; }
    public required string SqlType { get; init; }
    public required ReportValueKind Kind { get; init; }
    public required bool IsNullable { get; init; }

    /// <summary>False for binary and unknown types, which a report can't show.</summary>
    public required bool IsSupported { get; init; }
}

/// <summary>One parameter a procedure takes.</summary>
public sealed record ReportSourceParameter
{
    /// <summary>The name without the leading @.</summary>
    public required string Name { get; init; }

    public required string SqlType { get; init; }
    public required ReportValueKind Kind { get; init; }

    /// <summary>False for output parameters and types a report can't supply.</summary>
    public required bool IsSupported { get; init; }
}
