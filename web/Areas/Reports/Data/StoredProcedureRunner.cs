using System.Data;
using System.Data.Common;

namespace Viper.Areas.Reports.Data;

/// <summary>
/// Runs a stored procedure and maps each row of its result.
/// </summary>
public interface IStoredProcedureRunner
{
    Task<IReadOnlyList<T>> QueryAsync<T>(
        string connectionStringName,
        string procedure,
        IReadOnlyList<StoredProcedureParameter> parameters,
        Func<IDataRecord, T> map,
        CancellationToken ct);
}

/// <summary>
/// Calls stored procedures on databases that have no EF context. Procedure names are constants
/// in the data services, and every value is a typed parameter, so no SQL is ever built from input.
/// </summary>
public sealed class StoredProcedureRunner : IStoredProcedureRunner
{
    /// <summary>
    /// Some PPS report procedures scan the whole UCPath extract, so allow longer than the default 30 seconds.
    /// </summary>
    public const int CommandTimeoutSeconds = 120;

    private readonly IDbConnectionFactory _connections;

    public StoredProcedureRunner(IDbConnectionFactory connections)
    {
        _connections = connections;
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string connectionStringName,
        string procedure,
        IReadOnlyList<StoredProcedureParameter> parameters,
        Func<IDataRecord, T> map,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(procedure);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(map);

        await using DbConnection connection = _connections.Create(connectionStringName);
        await connection.OpenAsync(ct);
        await using DbCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = procedure;
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Parameters.AddRange(parameters.Select(parameter => parameter.ToDbParameter(command)).ToArray());

        await using DbDataReader reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<T>();
        while (await reader.ReadAsync(ct))
        {
            rows.Add(map(reader));
        }

        return rows;
    }
}
