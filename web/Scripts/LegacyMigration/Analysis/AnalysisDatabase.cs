using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// What the checks read a database through, so tests can answer their queries with known rows.
    /// </summary>
    public interface IAnalysisDatabase : IDisposable
    {
        string Database { get; }

        string DataSource { get; }

        /// <summary>
        /// Each row as its column values, NULL as null.
        /// </summary>
        List<object?[]> QueryRows(string sql, params SqlParameter[] parameters);

        /// <summary>
        /// Hands the open reader to read, for a result too large to hold as rows.
        /// </summary>
        void Read(string sql, Action<DbDataReader> read);
    }

    /// <summary>
    /// The analysis's one connection to the legacy database. It only reads: SQL is built from names bound
    /// to the schema export, and every value goes in as a parameter. The exception is a customSql file,
    /// run as written once CustomSqlValidator has shown it to be a single read-only SELECT.
    /// </summary>
    public sealed class AnalysisDatabase : IAnalysisDatabase
    {
        // A full read of a large table can take minutes.
        private const int CommandTimeoutSeconds = 600;

        private readonly SqlConnection _conn;

        private AnalysisDatabase(SqlConnection conn)
        {
            _conn = conn;
        }

        /// <summary>
        /// Reads without locks for the whole session: full-table reads would otherwise take shared locks
        /// that can delay a live application's writes. Counts may be slightly off while the application is
        /// mid-write, which is fine for analysis.
        /// </summary>
        public static AnalysisDatabase Open(string connectionString)
        {
            var conn = new SqlConnection(connectionString);
            try
            {
                conn.Open();
                using var cmd = new SqlCommand("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED", conn);
                cmd.ExecuteNonQuery();
                return new AnalysisDatabase(conn);
            }
            catch (SqlException)
            {
                conn.Dispose();
                throw;
            }
        }

        public string Database => _conn.Database;

        public string DataSource => _conn.DataSource ?? "";

        public void Read(string sql, Action<DbDataReader> read)
        {
            using var cmd = new SqlCommand(sql, _conn);
            cmd.CommandTimeout = CommandTimeoutSeconds;
            using var reader = cmd.ExecuteReader();
            read(reader);
        }

        public List<object?[]> QueryRows(string sql, params SqlParameter[] parameters)
        {
            using var cmd = new SqlCommand(sql, _conn);
            cmd.CommandTimeout = CommandTimeoutSeconds;
            cmd.Parameters.AddRange(parameters);
            using var reader = cmd.ExecuteReader();

            var rows = new List<object?[]>();
            while (reader.Read())
            {
                rows.Add(Enumerable.Range(0, reader.FieldCount).Select(i => ValueOf(reader, i)).ToArray());
            }
            return rows;
        }

        /// <summary>
        /// A column's value, NULL as null. A decimal that does not fit .NET's decimal (decimal(38) holds up
        /// to 38 digits, .NET's 28 or 29) comes back as its text, where GetValue would throw.
        /// </summary>
        public static object? ValueOf(DbDataReader reader, int i)
        {
            if (reader.IsDBNull(i))
            {
                return null;
            }
            return reader is SqlDataReader sql && IsSqlDecimal(sql.GetDataTypeName(i))
                ? DecimalValue(sql.GetSqlDecimal(i))
                : reader.GetValue(i);
        }

        /// <summary>
        /// Whether a column holds a SqlDecimal, by its SQL Server type: money and smallmoney also report
        /// decimal as their .NET type, but hold a SqlMoney that GetSqlDecimal cannot read, and always fit
        /// .NET's decimal.
        /// </summary>
        internal static bool IsSqlDecimal(string dataTypeName) =>
            dataTypeName.Equals("decimal", StringComparison.OrdinalIgnoreCase) || dataTypeName.Equals("numeric", StringComparison.OrdinalIgnoreCase);

        internal static object DecimalValue(SqlDecimal value)
        {
            try
            {
                return value.Value;
            }
            catch (OverflowException)
            {
                return value.ToString();
            }
        }

        public void Dispose() => _conn.Dispose();
    }

    /// <summary>
    /// What a check runs with: the plan, the legacy database, and on demand, other databases to look
    /// things up in (such as VIPER for people). Lookup connections open on first use, through openLookup
    /// (for the same environment as the analysis), and close with the context.
    /// </summary>
    public sealed class AnalysisContext(AnalysisPlan plan, IAnalysisDatabase database, Func<string, IAnalysisDatabase> openLookup) : IDisposable
    {
        /// <summary>
        /// The most example keys any one result lists; its total is always shown.
        /// </summary>
        public const int ExampleLimit = 50;

        private readonly Dictionary<string, IAnalysisDatabase> _lookups = new(StringComparer.Ordinal);
        private readonly Dictionary<TableRef, TableProfile> _profiles = [];

        public AnalysisPlan Plan { get; } = plan;

        public IAnalysisDatabase Database { get; } = database;

        /// <summary>
        /// An exception's message for the report, with any value it quotes described by shape (see ResultSql.ErrorText).
        /// </summary>
        public string ErrorText(Exception ex) => ResultSql.ErrorText(ex, Plan.Schema);

        /// <summary>
        /// A read-only connection by connection name. Throws InvalidOperationException when the name has
        /// no connection string, which fails only the checks that need it.
        /// </summary>
        public IAnalysisDatabase LookupDatabase(string connectionName)
        {
            if (!_lookups.TryGetValue(connectionName, out var lookup))
            {
                lookup = openLookup(connectionName);
                Console.WriteLine($"  Opened lookup connection {connectionName}: {lookup.DataSource}/{lookup.Database}");
                _lookups[connectionName] = lookup;
            }
            return lookup;
        }

        /// <summary>
        /// A table's one profile read, shared by every check built on it. A failed read is kept too, so
        /// it is not retried by each check.
        /// </summary>
        public TableProfile Profile(TableRef table)
        {
            if (!_profiles.TryGetValue(table, out var profile))
            {
                profile = TableProfile.Read(this, table);
                _profiles[table] = profile;
            }
            return profile;
        }

        public void Dispose()
        {
            foreach (var lookup in _lookups.Values)
            {
                lookup.Dispose();
            }
        }
    }
}
