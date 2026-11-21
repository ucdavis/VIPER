using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// A database for check tests: answers each query with the rows registered for the first SQL fragment it
/// contains, and records every query with its parameters so tests can check what was asked. A query with
/// no answer fails the test, naming the SQL, rather than returning nothing and passing by accident. A
/// parameter passed to two queries fails too, as SQL Server refuses a parameter already used by another command.
/// </summary>
internal sealed class FakeDatabase : IAnalysisDatabase
{
    private readonly List<(Func<string, bool> Matches, Func<SqlParameter[], List<object?[]>> Rows)> _answers = [];
    private readonly List<(string Fragment, Func<DataTable> Table)> _readers = [];
    private readonly HashSet<SqlParameter> _usedParameters = [];

    public string Database => "Legacy";

    public string DataSource => "fake";

    public List<(string Sql, SqlParameter[] Parameters)> Queries { get; } = [];

    /// <summary>
    /// Rows for every query containing fragment; earlier registrations win, so register the specific ones first.
    /// </summary>
    public FakeDatabase Answer(string fragment, params object?[][] rows) => AnswerWith(fragment, _ => [.. rows]);

    /// <summary>
    /// Rows worked out from the query's parameters, such as a lookup answering only the values it was sent.
    /// </summary>
    public FakeDatabase AnswerWith(string fragment, Func<SqlParameter[], List<object?[]>> rows) =>
        AnswerWhen(sql => sql.Contains(fragment, StringComparison.Ordinal), rows);

    public FakeDatabase AnswerWhen(Func<string, bool> matches, Func<SqlParameter[], List<object?[]>> rows)
    {
        _answers.Add((matches, rows));
        return this;
    }

    /// <summary>
    /// The shared profile read of one table: the only query that ends at its FROM, with no WHERE.
    /// </summary>
    public FakeDatabase AnswerProfile(string table, ProfileRow row) =>
        AnswerWhen(sql => sql.StartsWith("SELECT COUNT_BIG(*),\n", StringComparison.Ordinal) && sql.EndsWith($"FROM {table} AS t", StringComparison.Ordinal),
            _ => [row.Build()]);

    /// <summary>
    /// A result read through Read, as customSql does, with its columns' names and types.
    /// </summary>
    public FakeDatabase AnswerReader(string fragment, Func<DataTable> table)
    {
        _readers.Add((fragment, table));
        return this;
    }

    public List<object?[]> QueryRows(string sql, params SqlParameter[] parameters)
    {
        foreach (var parameter in parameters)
        {
            if (!_usedParameters.Add(parameter))
            {
                throw new InvalidOperationException($"Parameter {parameter.ParameterName} was already used by another query:\n{sql}");
            }
        }
        Queries.Add((sql, parameters));
        var answer = _answers.Find(a => a.Matches(sql));
        return answer.Rows != null ? answer.Rows(parameters) : throw new InvalidOperationException($"No answer registered for:\n{sql}");
    }

    public void Read(string sql, Action<DbDataReader> read)
    {
        Queries.Add((sql, []));
        var answer = _readers.Find(a => sql.Contains(a.Fragment, StringComparison.Ordinal));
        if (answer.Table == null)
        {
            throw new InvalidOperationException($"No reader registered for:\n{sql}");
        }
        using var table = answer.Table();
        using var reader = table.CreateDataReader();
        read(reader);
    }

    /// <summary>
    /// Every query's SQL, for asserting on what was asked.
    /// </summary>
    public string AllSql => string.Join("\n----\n", Queries.Select(q => q.Sql));

    public void Dispose()
    {
    }
}
