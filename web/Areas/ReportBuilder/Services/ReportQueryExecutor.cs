using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>The rows a report query returned.</summary>
public sealed record ReportQueryResult
{
    /// <summary>Each row's values by column name, ignoring case; database nulls are null.</summary>
    public required IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; }

    /// <summary>The count query's answer, when the command had one.</summary>
    public int? TotalRows { get; init; }

    /// <summary>True when the query had more rows than the limit; the extra rows aren't read.</summary>
    public required bool Truncated { get; init; }
}

/// <summary>Runs the SQL a <see cref="ReportSqlBuilder"/> wrote.</summary>
public interface IReportQueryExecutor
{
    /// <summary>
    /// Reads at most <paramref name="maxRows"/> rows. Each statement gets
    /// <paramref name="timeout"/>; a timeout or database error is thrown as a <see cref="DbException"/>.
    /// </summary>
    Task<ReportQueryResult> QueryAsync(ReportSqlCommand command, int maxRows, TimeSpan timeout, CancellationToken ct);
}

/// <summary>
/// Runs report queries on <c>ConnectionStrings:VIPERReports</c> when it's set (ideally a login
/// limited to reading [report]), otherwise on <c>VIPER</c>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Needs a live SQL Server; the SQL it runs is tested in ReportSqlBuilderTests.")]
public sealed class ReportQueryExecutor : IReportQueryExecutor
{
    internal const string ReportsConnectionName = "VIPERReports";
    internal const string FallbackConnectionName = "VIPER";

    // Long enough for any parameter value the binder allows, and one size for every string so
    // SQL Server reuses the cached plan instead of compiling one per value length.
    private const int StringParameterSize = 4000;

    private readonly string _connectionString;

    public ReportQueryExecutor(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString(ReportsConnectionName)
            ?? configuration.GetConnectionString(FallbackConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{FallbackConnectionName}' not configured");
    }

    public async Task<ReportQueryResult> QueryAsync(ReportSqlCommand command, int maxRows, TimeSpan timeout, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        int? total = null;
        if (command.CountText is not null)
        {
            await using SqlCommand count = Create(connection, command.CountText, command.Parameters, timeout);
            total = Convert.ToInt32(await count.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
        }

        await using SqlCommand select = Create(connection, command.Text, command.Parameters, timeout);
        await using SqlDataReader reader = await select.ExecuteReaderAsync(CommandBehavior.SingleResult, ct);
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        bool truncated = false;
        while (await reader.ReadAsync(ct))
        {
            if (rows.Count >= maxRows)
            {
                truncated = true;
                break;
            }

            var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return new ReportQueryResult { Rows = rows, TotalRows = total, Truncated = truncated };
    }

    private static SqlCommand Create(SqlConnection connection, string text, IReadOnlyDictionary<string, object> parameters, TimeSpan timeout)
    {
        // Text is built by ReportSqlBuilder from catalog-checked, bracket-quoted names; values are parameters.
#pragma warning disable CA2100
        var command = new SqlCommand(text, connection)
        {
            CommandTimeout = (int)Math.Ceiling(timeout.TotalSeconds),
        };
#pragma warning restore CA2100
        foreach ((string name, object value) in parameters)
        {
            SqlParameter parameter = command.Parameters.AddWithValue(name, value);
            if (value is string)
            {
                parameter.Size = StringParameterSize;
            }
        }

        return command;
    }
}
