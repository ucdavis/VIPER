using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Viper.Areas.ReportBuilder.Models;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>A SQL statement and its parameter values, ready for the query executor.</summary>
public sealed record ReportSqlCommand
{
    public required string Text { get; init; }

    /// <summary>A second statement returning the total row count, for paged views.</summary>
    public string? CountText { get; init; }

    public required IReadOnlyDictionary<string, object> Parameters { get; init; }
}

/// <summary>What to read from a view: columns, filters, search, order and paging.</summary>
public sealed record ReportViewQuery
{
    public required string Source { get; init; }
    public required IReadOnlyList<string> Columns { get; init; }
    public IReadOnlyList<BoundReportParameter> Filters { get; init; } = [];

    /// <summary>Text columns the search looks in.</summary>
    public IReadOnlyList<string> SearchColumns { get; init; } = [];

    public string? Search { get; init; }
    public string? SortColumn { get; init; }
    public bool SortDescending { get; init; }

    /// <summary>Rows to skip and take for one page; both null to read up to <see cref="MaxRows"/>.</summary>
    public int? Skip { get; init; }

    public int? Take { get; init; }

    /// <summary>The cap when not paging. One extra row is read so the caller can tell it was hit.</summary>
    public int MaxRows { get; init; } = ReportLimits.MaxRows;
}

/// <summary>
/// Writes the SQL that reads a report. Object, column and parameter names come only from the
/// saved report, already checked against the [report] catalog, and are always bracket-quoted;
/// every value is a SQL parameter, never part of the SQL text.
/// </summary>
public static partial class ReportSqlBuilder
{
    internal const string SkipParameter = "@__skip";
    internal const string TakeParameter = "@__take";
    internal const string TopParameter = "@__top";
    internal const string SearchParameter = "@__search";

    private const char LikeEscape = '\\';

    /// <summary>The SELECT (and, when paged, the COUNT) for a view.</summary>
    public static ReportSqlCommand BuildView(ReportViewQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Columns.Count == 0)
        {
            throw new ArgumentException("A report needs at least one column.", nameof(query));
        }

        var parameters = new Dictionary<string, object>(StringComparer.Ordinal);
        string where = WhereClause(query, parameters);
        string source = Qualified(query.Source);
        string columns = string.Join(", ", query.Columns.Select(Quote));
        string direction = query.SortDescending ? "DESC" : "ASC";
        string orderBy = query.SortColumn is null
            ? "ORDER BY (SELECT NULL)"
            : $"ORDER BY {Quote(query.SortColumn)} {direction}";

        if (query.Skip is { } skip && query.Take is { } take)
        {
            parameters[SkipParameter] = skip;
            parameters[TakeParameter] = take;
            return new ReportSqlCommand
            {
                Text = $"SELECT {columns} FROM {source}{where} {orderBy} OFFSET {SkipParameter} ROWS FETCH NEXT {TakeParameter} ROWS ONLY",
                CountText = $"SELECT COUNT(*) FROM {source}{where}",
                Parameters = parameters,
            };
        }

        parameters[TopParameter] = query.MaxRows + 1;
        return new ReportSqlCommand
        {
            Text = $"SELECT TOP ({TopParameter}) {columns} FROM {source}{where} {orderBy}",
            Parameters = parameters,
        };
    }

    /// <summary>
    /// The EXEC for a procedure. Skip and take are passed only to procedures that page in SQL;
    /// their names are the catalog's <c>@Skip</c> and <c>@Take</c>.
    /// </summary>
    public static ReportSqlCommand BuildProcedure(
        string source, IReadOnlyList<BoundReportParameter> arguments, int? skip = null, int? take = null)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var parameters = new Dictionary<string, object>(StringComparer.Ordinal);
        var assignments = new List<string>();
        for (int i = 0; i < arguments.Count; i++)
        {
            string placeholder = Placeholder(i);
            assignments.Add($"{ParameterName(arguments[i].Name)} = {placeholder}");
            parameters[placeholder] = ScalarValue(arguments[i]);
        }

        if (skip is { } skipRows && take is { } takeRows)
        {
            assignments.Add($"@{ReportSourceCatalogService.SkipParameter} = {SkipParameter}");
            assignments.Add($"@{ReportSourceCatalogService.TakeParameter} = {TakeParameter}");
            parameters[SkipParameter] = skipRows;
            parameters[TakeParameter] = takeRows;
        }

        string call = $"EXEC {Qualified(source)}";
        return new ReportSqlCommand
        {
            Text = assignments.Count == 0 ? call : $"{call} {string.Join(", ", assignments)}",
            Parameters = parameters,
        };
    }

    /// <summary>The value and label columns of a choice list's view, ordered by label.</summary>
    public static ReportSqlCommand BuildChoices(string source)
    {
        return new ReportSqlCommand
        {
            Text = $"SELECT TOP ({TopParameter}) [Value], [Label] FROM {Qualified(source)} ORDER BY [Label]",
            Parameters = new Dictionary<string, object> { [TopParameter] = ReportLimits.MaxChoices },
        };
    }

    /// <summary>A name in brackets, with any closing bracket doubled.</summary>
    public static string Quote(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return $"[{name.Replace("]", "]]", StringComparison.Ordinal)}]";
    }

    private static string Qualified(string source)
    {
        return $"{Quote(ReportBuilderDbContext.Schema)}.{Quote(source)}";
    }

    private static string WhereClause(ReportViewQuery query, Dictionary<string, object> parameters)
    {
        var conditions = new List<string>();
        for (int i = 0; i < query.Filters.Count; i++)
        {
            conditions.Add(Condition(query.Filters[i], Placeholder(i), parameters));
        }

        if (!string.IsNullOrWhiteSpace(query.Search) && query.SearchColumns.Count > 0)
        {
            parameters[SearchParameter] = ContainsPattern(query.Search.Trim());
            conditions.Add("(" + string.Join(" OR ", query.SearchColumns.Select(c => Like(Quote(c), SearchParameter))) + ")");
        }

        return conditions.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", conditions);
    }

    private static string Condition(BoundReportParameter filter, string placeholder, Dictionary<string, object> parameters)
    {
        string column = Quote(filter.Name);
        if (filter.Value is IReadOnlyList<string> list)
        {
            return InList(column, placeholder, list, parameters);
        }

        ReportFilterOperator op = filter.Operator ?? ReportFilterOperator.Equals;
        switch (op)
        {
            case ReportFilterOperator.Contains:
                parameters[placeholder] = ContainsPattern(Convert.ToString(filter.Value, CultureInfo.InvariantCulture) ?? string.Empty);
                return Like(column, placeholder);
            case ReportFilterOperator.From:
                parameters[placeholder] = filter.Value;
                return $"{column} >= {placeholder}";
            case ReportFilterOperator.To when filter.Value is DateTime date:
                // A date's whole day counts, whatever time of day the column holds.
                parameters[placeholder] = date.Date.AddDays(1);
                return $"{column} < {placeholder}";
            case ReportFilterOperator.To:
                parameters[placeholder] = filter.Value;
                return $"{column} <= {placeholder}";
            case ReportFilterOperator.In:
                return InList(column, placeholder, [Convert.ToString(filter.Value, CultureInfo.InvariantCulture) ?? string.Empty], parameters);
            default:
                parameters[placeholder] = filter.Value;
                return $"{column} = {placeholder}";
        }
    }

    private static string InList(string column, string placeholder, IReadOnlyList<string> values, Dictionary<string, object> parameters)
    {
        var names = new List<string>();
        for (int i = 0; i < values.Count; i++)
        {
            string name = $"{placeholder}_{i}";
            parameters[name] = values[i];
            names.Add(name);
        }

        return $"{column} IN ({string.Join(", ", names)})";
    }

    private static string Like(string column, string placeholder)
    {
        return $"{column} LIKE {placeholder} ESCAPE '{LikeEscape}'";
    }

    /// <summary>%text%, with LIKE's own wildcards in the text escaped so they match literally.</summary>
    internal static string ContainsPattern(string text)
    {
        var escaped = new StringBuilder(text.Length + 2).Append('%');
        foreach (char c in text)
        {
            if (c is '%' or '_' or '[' or LikeEscape)
            {
                escaped.Append(LikeEscape);
            }

            escaped.Append(c);
        }

        return escaped.Append('%').ToString();
    }

    private static object ScalarValue(BoundReportParameter argument)
    {
        // A procedure takes one value per parameter, so a multi-choice list is passed comma-separated.
        return argument.Value is IReadOnlyList<string> list ? string.Join(",", list) : argument.Value;
    }

    private static string Placeholder(int index)
    {
        return string.Create(CultureInfo.InvariantCulture, $"@p{index}");
    }

    /// <summary>
    /// A procedure parameter name for EXEC. Names come from sys.parameters, but they can't be
    /// bracket-quoted, so anything other than a plain identifier is refused.
    /// </summary>
    private static string ParameterName(string name)
    {
        string bare = name.TrimStart('@');
        if (!IdentifierPattern().IsMatch(bare))
        {
            throw new ArgumentException($"'{bare}' is not a valid parameter name.", nameof(name));
        }

        return "@" + bare;
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*\z", RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex IdentifierPattern();
}
