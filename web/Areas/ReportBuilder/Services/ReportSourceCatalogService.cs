using Viper.Areas.ReportBuilder.Models;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>
/// The allow-list of report sources: the views and procedures in the [reports] schema, with the
/// columns they return and the parameters they take.
/// </summary>
public interface IReportSourceCatalogService
{
    /// <summary>Every view and procedure in [reports], by name.</summary>
    Task<IReadOnlyList<ReportSourceSummary>> GetSourcesAsync(CancellationToken ct);

    /// <summary>One source's columns and parameters, or null when [reports] has no such object.</summary>
    Task<ReportSourceDetail?> GetSourceAsync(string name, CancellationToken ct);
}

public sealed class ReportSourceCatalogService : IReportSourceCatalogService
{
    /// <summary>Procedure parameters the builder fills itself to page in SQL.</summary>
    internal const string SkipParameter = "Skip";

    internal const string TakeParameter = "Take";

    private const string ViewType = "V";

    private readonly IReportSourceMetadataReader _reader;

    public ReportSourceCatalogService(IReportSourceMetadataReader reader)
    {
        _reader = reader;
    }

    public async Task<IReadOnlyList<ReportSourceSummary>> GetSourcesAsync(CancellationToken ct)
    {
        IReadOnlyList<ReportSourceObjectRow> objects = await _reader.GetObjectsAsync(ct);
        return [.. objects.Select(ToSummary)];
    }

    public async Task<ReportSourceDetail?> GetSourceAsync(string name, CancellationToken ct)
    {
        // Only names the catalog lists are looked up, so a caller can't probe other objects.
        IReadOnlyList<ReportSourceObjectRow> objects = await _reader.GetObjectsAsync(ct);
        ReportSourceObjectRow? source = objects.FirstOrDefault(row =>
            string.Equals(row.Name, name, StringComparison.OrdinalIgnoreCase));
        if (source is null)
        {
            return null;
        }

        ReportSourceSummary summary = ToSummary(source);
        return summary.Kind == ReportSourceKind.View
            ? await DescribeViewAsync(summary, ct)
            : await DescribeProcedureAsync(summary, ct);
    }

    private async Task<ReportSourceDetail> DescribeViewAsync(ReportSourceSummary view, CancellationToken ct)
    {
        IReadOnlyList<ReportSourceColumnRow> columns = await _reader.GetViewColumnsAsync(view.Name, ct);
        return new ReportSourceDetail
        {
            Name = view.Name,
            Kind = view.Kind,
            Description = view.Description,
            Columns = ToColumns(columns),
            Parameters = [],
            PagesInSql = true,
        };
    }

    private async Task<ReportSourceDetail> DescribeProcedureAsync(ReportSourceSummary procedure, CancellationToken ct)
    {
        IReadOnlyList<ReportSourceParameter> parameters =
            [.. (await _reader.GetProcedureParametersAsync(procedure.Name, ct)).Select(ToParameter)];
        IReadOnlyList<ReportSourceColumnRow> result = await _reader.DescribeProcedureResultAsync(procedure.Name, ct);
        string? error = result.Select(row => row.ErrorMessage).FirstOrDefault(message => message is not null);
        bool pagesInSql = IsPagingParameter(parameters, SkipParameter) && IsPagingParameter(parameters, TakeParameter);

        return new ReportSourceDetail
        {
            Name = procedure.Name,
            Kind = procedure.Kind,
            Description = procedure.Description,
            Columns = error is null ? ToColumns(result) : [],
            ColumnsError = error,

            // @Skip and @Take are filled by the builder, so an admin never maps them.
            Parameters = pagesInSql
                ? [.. parameters.Where(p => !IsNamed(p, SkipParameter) && !IsNamed(p, TakeParameter))]
                : parameters,
            PagesInSql = pagesInSql,
        };
    }

    private static ReportSourceSummary ToSummary(ReportSourceObjectRow row)
    {
        return new ReportSourceSummary
        {
            Name = row.Name,
            Kind = string.Equals(row.Type, ViewType, StringComparison.OrdinalIgnoreCase)
                ? ReportSourceKind.View
                : ReportSourceKind.Procedure,
            Description = string.IsNullOrWhiteSpace(row.Description) ? null : row.Description.Trim(),
        };
    }

    private static IReadOnlyList<ReportSourceColumn> ToColumns(IEnumerable<ReportSourceColumnRow> rows)
    {
        return [.. rows
            .Where(row => !string.IsNullOrEmpty(row.Name))
            .Select(row =>
            {
                ReportValueKind kind = ReportSqlTypes.KindOf(row.SqlType);
                return new ReportSourceColumn
                {
                    Name = row.Name!,
                    SqlType = row.SqlType ?? string.Empty,
                    Kind = kind,
                    IsNullable = row.IsNullable ?? true,
                    IsSupported = ReportSqlTypes.IsSupported(kind),
                };
            })];
    }

    private static ReportSourceParameter ToParameter(ReportSourceParameterRow row)
    {
        ReportValueKind kind = ReportSqlTypes.KindOf(row.SqlType);
        return new ReportSourceParameter
        {
            Name = row.Name.TrimStart('@'),
            SqlType = row.SqlType,
            Kind = kind,
            IsSupported = !row.IsOutput && ReportSqlTypes.IsSupported(kind),
        };
    }

    private static bool IsPagingParameter(IEnumerable<ReportSourceParameter> parameters, string name)
    {
        return parameters.Any(p => IsNamed(p, name) && p.IsSupported && p.Kind == ReportValueKind.WholeNumber);
    }

    private static bool IsNamed(ReportSourceParameter parameter, string name)
    {
        return string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase);
    }
}
