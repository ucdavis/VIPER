using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>Runs a saved report for the signed-in user, on screen or for a download.</summary>
public interface IReportRunService
{
    /// <summary>
    /// One page of the report (or, for downloads, every row up to the cap). The user's view
    /// permission is checked before anything they sent is read.
    /// </summary>
    Task<ReportRunOutcome> RunAsync(string slug, ReportRunRequest request, ReportRunMode mode, CancellationToken ct);
}

/// <summary>
/// Runs a report: finds it by slug, checks the user may view it, checks its source and columns
/// against the [report] catalog, binds the parameters, builds and runs the SQL, and logs the run.
/// Views are filtered, searched, sorted and paged in SQL. Procedures that take @Skip and @Take
/// page themselves; other procedures are read up to the row cap and searched, sorted and paged
/// here.
/// </summary>
public sealed class ReportRunService : IReportRunService
{
    /// <summary>The column a self-paging procedure returns its total row count in.</summary>
    internal const string TotalRowsColumn = "TotalRows";

    private const string DateFormat = "yyyy-MM-dd";

    private readonly ReportBuilderDbContext _context;
    private readonly IReportUserService _users;
    private readonly IReportSourceCatalogService _catalog;
    private readonly IReportChoiceService _choices;
    private readonly IReportQueryExecutor _executor;
    private readonly ILogger<ReportRunService> _logger;
    private readonly TimeProvider _clock;

    public ReportRunService(
        ReportBuilderDbContext context,
        IReportUserService users,
        IReportSourceCatalogService catalog,
        IReportChoiceService choices,
        IReportQueryExecutor executor,
        ILogger<ReportRunService> logger,
        TimeProvider? clock = null)
    {
        _context = context;
        _users = users;
        _catalog = catalog;
        _choices = choices;
        _executor = executor;
        _logger = logger;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ReportRunOutcome> RunAsync(string slug, ReportRunRequest request, ReportRunMode mode, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ReportCurrentUser? user = _users.GetCurrentUser();
        if (user is null)
        {
            return ReportRunOutcome.Fail(ReportRunStatus.Forbidden);
        }

        ReportDefinition? report = await FindAsync(slug, ct);
        if (report is null)
        {
            return ReportRunOutcome.Fail(ReportRunStatus.NotFound);
        }

        if (!CanView(report) || !AllowsMode(report, mode))
        {
            return ReportRunOutcome.Fail(ReportRunStatus.Forbidden);
        }

        long started = _clock.GetTimestamp();
        ReportRunOutcome outcome;
        IReadOnlyList<BoundReportParameter> parameters;
        try
        {
            ReportSourceDetail? source = await _catalog.GetSourceAsync(report.Source, ct);
            if (source is null)
            {
                return ReportRunOutcome.Fail(ReportRunStatus.Unavailable, "This report's data source is no longer available.");
            }

            IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> choices = await _choices.GetChoicesAsync(report.Parameters, ct);
            ReportBindResult bound = ReportParameterBinder.Bind(report.Parameters, request.Parameters, user, choices);
            if (bound.Errors.Count > 0)
            {
                return ReportRunOutcome.Fail(ReportRunStatus.Invalid, [.. bound.Errors]);
            }

            parameters = bound.Parameters;
            outcome = Plan(report, source, request, mode, parameters, out RunPlan? plan);
            if (plan is not null)
            {
                outcome = new ReportRunOutcome
                {
                    Status = ReportRunStatus.Ok,
                    Result = source.Kind == ReportSourceKind.View
                        ? await RunViewAsync(plan, ct)
                        : await RunProcedureAsync(plan, ct),
                };
            }
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Report {ReportId} failed to run", report.Id);
            return ReportRunOutcome.Fail(ReportRunStatus.Unavailable, "The report couldn't be run. Try again later.");
        }

        if (outcome.Result is not null)
        {
            await LogRunAsync(report, user, mode, parameters, outcome.Result, _clock.GetElapsedTime(started), ct);
        }

        return outcome;
    }

    private async Task<ReportDefinition?> FindAsync(string slug, CancellationToken ct)
    {
        if (!Guid.TryParse(slug, out Guid id))
        {
            return null;
        }

        return await _context.Reports
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Include(r => r.Columns)
            .Include(r => r.Parameters)
            .FirstOrDefaultAsync(r => r.Slug == id && r.DeletedOn == null, ct);
    }

    /// <summary>Admins see every report; others need one of its permissions. A report with none is admin-only.</summary>
    private bool CanView(ReportDefinition report)
    {
        return _users.HasPermission(ReportBuilderPermissions.Admin)
            || report.Permissions.Any(p => !string.IsNullOrWhiteSpace(p.Permission) && _users.HasPermission(p.Permission));
    }

    private static bool AllowsMode(ReportDefinition report, ReportRunMode mode)
    {
        return mode switch
        {
            ReportRunMode.ExportExcel => report.AllowExcel,
            ReportRunMode.ExportPdf => report.AllowPdf,
            _ => true,
        };
    }

    /// <summary>Everything a run needs, checked against the catalog and the request.</summary>
    private sealed record RunPlan
    {
        public required ReportSourceDetail Source { get; init; }
        public required IReadOnlyList<ReportResultColumn> Columns { get; init; }
        public required IReadOnlyList<BoundReportParameter> Parameters { get; init; }
        public string? SortColumn { get; init; }
        public bool SortDescending { get; init; }
        public string? Search { get; init; }
        public required bool Paginate { get; init; }
        public required int Page { get; init; }
        public required int PerPage { get; init; }
        public required TimeSpan Timeout { get; init; }

        public int Skip => (Page - 1) * PerPage;
    }

    private static ReportRunOutcome Plan(
        ReportDefinition report,
        ReportSourceDetail source,
        ReportRunRequest request,
        ReportRunMode mode,
        IReadOnlyList<BoundReportParameter> parameters,
        out RunPlan? plan)
    {
        plan = null;
        var known = new Dictionary<string, ReportSourceColumn>(StringComparer.OrdinalIgnoreCase);
        foreach (ReportSourceColumn column in source.Columns)
        {
            known.TryAdd(column.Name, column);
        }

        // A procedure whose result SQL Server couldn't describe has no known columns; its
        // columns are then taken on trust and a missing one shows as empty.
        bool checkColumns = known.Count > 0;

        List<ReportResultColumn> columns = [.. report.Columns
            .Where(c => c.Visible && !string.IsNullOrWhiteSpace(c.SourceColumn))
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Id)
            .Select(c => new ReportResultColumn
            {
                Name = known.TryGetValue(c.SourceColumn!, out ReportSourceColumn? match) ? match.Name : c.SourceColumn!,
                Label = string.IsNullOrWhiteSpace(c.Label) ? c.SourceColumn! : c.Label,
                Format = c.Format ?? ReportColumnFormat.Text,
                Sortable = c.Sortable,
            })];
        if (columns.Count == 0)
        {
            return ReportRunOutcome.Fail(ReportRunStatus.Unavailable, "This report has no columns to show.");
        }

        string? missing = columns.Select(c => c.Name).FirstOrDefault(name => checkColumns && !known.ContainsKey(name));
        if (missing is not null)
        {
            return ReportRunOutcome.Fail(ReportRunStatus.Unavailable, $"The column '{missing}' is no longer in this report's data source.");
        }

        if (!TryMatchParameters(source, parameters, known, out IReadOnlyList<BoundReportParameter> matched, out string? stale))
        {
            return ReportRunOutcome.Fail(ReportRunStatus.Unavailable, $"The parameter '{stale}' is no longer in this report's data source.");
        }

        var errors = new List<string>();
        (string? sortColumn, bool descending, bool sortMissing) = Sort(report, request, columns, known, checkColumns, errors);
        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        if (search is { Length: > ReportLimits.MaxTextLength })
        {
            errors.Add($"Search text must be {ReportLimits.MaxTextLength} characters or fewer.");
        }

        bool screen = mode == ReportRunMode.Screen;
        bool paginate = screen && report.Paginate != false;
        int perPage = Math.Clamp(report.PageSize ?? ReportLimits.DefaultPageSize, 1, ReportLimits.MaxPageSize);
        if (paginate && request.PerPage is int asked)
        {
            if (asked is < 1 or > ReportLimits.MaxPageSize)
            {
                errors.Add($"Rows per page must be between 1 and {ReportLimits.MaxPageSize}.");
            }

            perPage = asked;
        }

        // Pages past the row cap can't hold rows, so they're refused (and the skip can't overflow).
        if (paginate && (request.Page < 1 || (long)(request.Page - 1) * perPage >= ReportLimits.MaxRows))
        {
            errors.Add("That page doesn't exist.");
        }

        if (errors.Count > 0)
        {
            return ReportRunOutcome.Fail(ReportRunStatus.Invalid, [.. errors]);
        }

        if (sortMissing)
        {
            return ReportRunOutcome.Fail(ReportRunStatus.Unavailable, $"The sort column '{report.OrderBy}' is no longer in this report's data source.");
        }

        plan = new RunPlan
        {
            Source = source,
            Columns = columns,
            Parameters = matched,
            SortColumn = sortColumn,
            SortDescending = descending,
            Search = search,
            Paginate = paginate,
            Page = paginate ? request.Page : 1,
            PerPage = perPage,
            Timeout = screen ? ReportLimits.ScreenTimeout : ReportLimits.ExportTimeout,
        };
        return new ReportRunOutcome { Status = ReportRunStatus.Ok };
    }

    /// <summary>
    /// The requested sort, which must be a visible sortable column, or else the report's default
    /// order. <c>Missing</c> is true when the default sort column is no longer in the source.
    /// </summary>
    private static (string? Column, bool Descending, bool Missing) Sort(
        ReportDefinition report,
        ReportRunRequest request,
        IReadOnlyList<ReportResultColumn> columns,
        Dictionary<string, ReportSourceColumn> known,
        bool checkColumns,
        List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            ReportResultColumn? column = columns.FirstOrDefault(c =>
                c.Sortable && string.Equals(c.Name, request.SortBy, StringComparison.OrdinalIgnoreCase));
            if (column is null)
            {
                errors.Add("This report can't be sorted by that column.");
                return (null, false, false);
            }

            return (column.Name, request.SortDescending, false);
        }

        if (string.IsNullOrWhiteSpace(report.OrderBy))
        {
            return (null, false, false);
        }

        string orderBy = report.OrderBy.Trim();
        bool descending = report.OrderByDescending == true;
        if (!checkColumns)
        {
            return (orderBy, descending, false);
        }

        return known.TryGetValue(orderBy, out ReportSourceColumn? match)
            ? (match.Name, descending, false)
            : (null, false, true);
    }

    /// <summary>
    /// Each parameter's name as the source spells it: a column for a view's filters, a supported
    /// parameter for a procedure. False, with the name, if one is no longer there.
    /// </summary>
    private static bool TryMatchParameters(
        ReportSourceDetail source,
        IReadOnlyList<BoundReportParameter> parameters,
        Dictionary<string, ReportSourceColumn> columns,
        out IReadOnlyList<BoundReportParameter> matched,
        out string? stale)
    {
        var names = source.Kind == ReportSourceKind.View
            ? columns.Keys.ToDictionary(name => name, StringComparer.OrdinalIgnoreCase)
            : source.Parameters.Where(p => p.IsSupported).Select(p => p.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(name => name, StringComparer.OrdinalIgnoreCase);
        var list = new List<BoundReportParameter>();
        foreach (BoundReportParameter parameter in parameters)
        {
            if (!names.TryGetValue(parameter.Name, out string? name))
            {
                matched = [];
                stale = parameter.Name;
                return false;
            }

            list.Add(parameter with { Name = name });
        }

        matched = list;
        stale = null;
        return true;
    }

    private async Task<ReportRunResult> RunViewAsync(RunPlan plan, CancellationToken ct)
    {
        IReadOnlyList<string> textColumns = [.. plan.Columns
            .Where(c => plan.Source.Columns.Any(s => s.Kind == ReportValueKind.Text && string.Equals(s.Name, c.Name, StringComparison.OrdinalIgnoreCase)))
            .Select(c => c.Name)];
        ReportSqlCommand command = ReportSqlBuilder.BuildView(new ReportViewQuery
        {
            Source = plan.Source.Name,
            Columns = [.. plan.Columns.Select(c => c.Name)],
            Filters = plan.Parameters,
            SearchColumns = textColumns,
            Search = plan.Search,
            SortColumn = plan.SortColumn,
            SortDescending = plan.SortDescending,
            Skip = plan.Paginate ? plan.Skip : null,
            Take = plan.Paginate ? plan.PerPage : null,
        });

        ReportQueryResult result = await _executor.QueryAsync(
            command, plan.Paginate ? plan.PerPage : ReportLimits.MaxRows, plan.Timeout, ct);
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows = Project(result.Rows, plan.Columns);
        return new ReportRunResult
        {
            Columns = plan.Columns,
            Rows = rows,
            Page = plan.Page,
            PerPage = plan.Paginate ? plan.PerPage : rows.Count,
            TotalRows = plan.Paginate ? result.TotalRows : rows.Count,
            Truncated = !plan.Paginate && result.Truncated,
        };
    }

    private async Task<ReportRunResult> RunProcedureAsync(RunPlan plan, CancellationToken ct)
    {
        bool pagesInSql = plan.Paginate && plan.Source.PagesInSql;
        ReportSqlCommand command = ReportSqlBuilder.BuildProcedure(
            plan.Source.Name, plan.Parameters, pagesInSql ? plan.Skip : null, pagesInSql ? plan.PerPage : null);
        ReportQueryResult result = await _executor.QueryAsync(
            command, pagesInSql ? plan.PerPage : ReportLimits.MaxRows, plan.Timeout, ct);

        if (pagesInSql)
        {
            // The procedure picks the page, so its own order stands; it reports the total in a TotalRows column.
            IReadOnlyList<IReadOnlyDictionary<string, object?>> page = Project(result.Rows, plan.Columns);
            return new ReportRunResult
            {
                Columns = plan.Columns,
                Rows = page,
                Page = plan.Page,
                PerPage = plan.PerPage,
                TotalRows = TotalFrom(result.Rows),
                Truncated = false,
            };
        }

        IEnumerable<IReadOnlyDictionary<string, object?>> rows = Project(result.Rows, plan.Columns);
        if (plan.Search is not null)
        {
            rows = rows.Where(row => Matches(row, plan.Search));
        }

        if (plan.SortColumn is not null)
        {
            var comparer = new ReportValueComparer();
            rows = plan.SortDescending
                ? rows.OrderByDescending(row => Value(row, plan.SortColumn), comparer)
                : rows.OrderBy(row => Value(row, plan.SortColumn), comparer);
        }

        List<IReadOnlyDictionary<string, object?>> all = [.. rows];
        List<IReadOnlyDictionary<string, object?>> shown = plan.Paginate
            ? [.. all.Skip(plan.Skip).Take(plan.PerPage)]
            : all;
        return new ReportRunResult
        {
            Columns = plan.Columns,
            Rows = shown,
            Page = plan.Page,
            PerPage = plan.Paginate ? plan.PerPage : shown.Count,
            TotalRows = all.Count,
            Truncated = result.Truncated,
        };
    }

    /// <summary>Each row reduced to the report's columns, in order, keyed by column name.</summary>
    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> Project(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, IReadOnlyList<ReportResultColumn> columns)
    {
        return [.. rows.Select(row =>
        {
            var projected = new Dictionary<string, object?>(columns.Count, StringComparer.OrdinalIgnoreCase);
            foreach (string name in columns.Select(column => column.Name))
            {
                projected[name] = Value(row, name);
            }

            return (IReadOnlyDictionary<string, object?>)projected;
        })];
    }

    private static object? Value(IReadOnlyDictionary<string, object?> row, string column)
    {
        return row.TryGetValue(column, out object? value) ? value : null;
    }

    private static bool Matches(IReadOnlyDictionary<string, object?> row, string search)
    {
        return row.Values.OfType<string>().Any(text => text.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private static int? TotalFrom(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        if (rows.Count == 0 || !rows[0].TryGetValue(TotalRowsColumn, out object? total) || total is null)
        {
            return null;
        }

        try
        {
            return Convert.ToInt32(total, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return null;
        }
    }

    private async Task LogRunAsync(
        ReportDefinition report,
        ReportCurrentUser user,
        ReportRunMode mode,
        IReadOnlyList<BoundReportParameter> parameters,
        ReportRunResult result,
        TimeSpan duration,
        CancellationToken ct)
    {
        _context.ReportRunLog.Add(new ReportRunLog
        {
            ReportId = report.Id,
            Action = mode switch
            {
                ReportRunMode.ExportExcel => ReportRunAction.ExportExcel,
                ReportRunMode.ExportPdf => ReportRunAction.ExportPdf,
                _ => ReportRunAction.Run,
            },
            LoginId = user.LoginId,
            RanOn = _clock.GetLocalNow().DateTime,
            Parameters = LoggedParameters(parameters),
            RowsReturned = result.Rows.Count,
            DurationMs = (int)Math.Min(int.MaxValue, Math.Round(duration.TotalMilliseconds)),
        });

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // The user still gets their report; a missing log entry isn't worth failing the run.
            _logger.LogWarning(ex, "Couldn't log a run of report {ReportId}", report.Id);
        }
    }

    /// <summary>The values the user chose, as JSON; current-user values aren't repeated.</summary>
    private static string? LoggedParameters(IReadOnlyList<BoundReportParameter> parameters)
    {
        var values = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (BoundReportParameter parameter in parameters.Where(p => p.FromUser))
        {
            values[parameter.Name] = parameter.Value switch
            {
                DateTime date => date.ToString(DateFormat, CultureInfo.InvariantCulture),
                decimal number => number.ToString(CultureInfo.InvariantCulture),
                bool flag => flag,
                IReadOnlyList<string> list => list,
                _ => Convert.ToString(parameter.Value, CultureInfo.InvariantCulture) ?? string.Empty,
            };
        }

        return values.Count == 0 ? null : JsonSerializer.Serialize(values);
    }

    /// <summary>
    /// Orders a column's values for in-memory sorting: empty values first, then by the values'
    /// own order, with text compared ignoring case.
    /// </summary>
    private sealed class ReportValueComparer : IComparer<object?>
    {
        public int Compare(object? x, object? y)
        {
            if (x is null || y is null)
            {
                return (x is null ? 0 : 1) - (y is null ? 0 : 1);
            }

            if (x is string a && y is string b)
            {
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            }

            if (x.GetType() == y.GetType() && x is IComparable comparable)
            {
                return comparable.CompareTo(y);
            }

            return string.Compare(
                Convert.ToString(x, CultureInfo.InvariantCulture),
                Convert.ToString(y, CultureInfo.InvariantCulture),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
