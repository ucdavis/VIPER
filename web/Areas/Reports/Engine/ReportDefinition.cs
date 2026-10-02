namespace Viper.Areas.Reports.Engine;

/// <summary>
/// A report the engine can list, describe and run, independent of its row and parameter types.
/// </summary>
public interface IReportDefinition : IReportIdentity
{
    /// <summary>
    /// True for reports with confidential personnel data; exports carry a confidentiality notice.
    /// </summary>
    bool IsSensitive { get; }

    /// <summary>
    /// The class the report's parameters are bound to from the request body.
    /// </summary>
    Type ParametersType { get; }

    bool CanRun(ReportContext context);

    ReportDefinitionMetadata GetMetadata(ReportContext context);

    /// <summary>
    /// Fills empty parameters with their defaults, then validates them. Returns an empty list
    /// when the parameters are ready to run.
    /// </summary>
    IReadOnlyList<ReportValidationError> PrepareParameters(object parameters);

    /// <summary>
    /// The non-empty parameters as labelled, human-readable filters for export headers.
    /// </summary>
    IReadOnlyList<ReportFilter> DescribeParameters(object parameters);

    /// <summary>
    /// Fetches the report's rows for prepared parameters and shapes them into a result.
    /// </summary>
    Task<ReportResult> RunAsync(object parameters, ReportContext context, DateTimeOffset generatedAt, CancellationToken ct);
}

/// <summary>
/// Base class for a code-defined report. <typeparamref name="TRow"/> is one row of the report's
/// data, <typeparamref name="TParams"/> holds the user's parameter values.
/// </summary>
public abstract class ReportDefinition<TRow, TParams> : IReportDefinition where TParams : class
{
    private ReportBuilder<TRow, TParams>? _layout;

    public abstract string Key { get; }

    public abstract string Title { get; }

    public abstract string Area { get; }

    public abstract string Description { get; }

    public abstract IReadOnlyList<string> Permissions { get; }

    public Type ParametersType => typeof(TParams);

    public bool IsSensitive => Layout.IsSensitive;

    /// <summary>
    /// The configured parameters, rules and columns. Built on first use so a report that is
    /// only listed in the catalog never pays for compiling its column selectors.
    /// </summary>
    internal ReportBuilder<TRow, TParams> Layout => _layout ??= BuildLayout();

    public bool CanRun(ReportContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.HasAnyPermission(Permissions);
    }

    public ReportDefinitionMetadata GetMetadata(ReportContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ReportBuilder<TRow, TParams> layout = Layout;
        return new ReportDefinitionMetadata(
            Key,
            Title,
            Area,
            Description,
            [.. layout.Parameters.Select(parameter => parameter.ToMetadata())],
            [.. layout.Columns.Where(column => column.IsVisible(context)).Select(column => column.ToMetadata())]);
    }

    public IReadOnlyList<ReportValidationError> PrepareParameters(object parameters)
    {
        TParams typed = AsParameters(parameters);
        ReportBuilder<TRow, TParams> layout = Layout;
        foreach (ReportParameter<TParams> parameter in layout.Parameters)
        {
            parameter.ApplyDefault(typed);
        }

        List<ReportValidationError> errors =
            [.. layout.Parameters.Select(parameter => parameter.Validate(typed)).OfType<ReportValidationError>()];
        if (errors.Count > 0)
        {
            return errors;
        }

        return [.. layout.Rules
            .Where(rule => !rule.IsValid(typed))
            .Select(rule => new ReportValidationError(rule.Parameter, rule.Message))];
    }

    public IReadOnlyList<ReportFilter> DescribeParameters(object parameters)
    {
        TParams typed = AsParameters(parameters);
        return [.. Layout.Parameters.Select(parameter => parameter.Describe(typed)).OfType<ReportFilter>()];
    }

    public async Task<ReportResult> RunAsync(
        object parameters, ReportContext context, DateTimeOffset generatedAt, CancellationToken ct)
    {
        TParams typed = AsParameters(parameters);
        ArgumentNullException.ThrowIfNull(context);
        IReadOnlyList<TRow> rows = await FetchAsync(typed, context, ct);
        return CreateResult(rows, context, generatedAt);
    }

    /// <summary>
    /// Shapes fetched rows into the result the client and exporters render.
    /// </summary>
    internal ReportResult CreateResult(IReadOnlyList<TRow> rows, ReportContext context, DateTimeOffset generatedAt)
    {
        return ReportPipeline.Build(Key, Title, Layout, rows, context, generatedAt);
    }

    /// <summary>
    /// Declares the report's parameters, cross-parameter rules, columns and layout.
    /// </summary>
    protected abstract void Configure(ReportBuilder<TRow, TParams> report);

    /// <summary>
    /// Loads the report's rows. Parameters have already been defaulted and validated.
    /// <paramref name="context"/> identifies the user, for reports scoped to the user's own units.
    /// </summary>
    protected abstract Task<IReadOnlyList<TRow>> FetchAsync(TParams parameters, ReportContext context, CancellationToken ct);

    private TParams AsParameters(object parameters)
    {
        return parameters as TParams
            ?? throw new ArgumentException(
                $"Report '{Key}' expects parameters of type {typeof(TParams).Name}.", nameof(parameters));
    }

    private ReportBuilder<TRow, TParams> BuildLayout()
    {
        var builder = new ReportBuilder<TRow, TParams>();
        Configure(builder);
        builder.EnsureValid(Key);
        return builder;
    }
}
