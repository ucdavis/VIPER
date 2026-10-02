using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Viper.Areas.Reports.Data;

namespace Viper.test.Reports;

/// <summary>
/// Hands out one <see cref="FakeDbConnection"/> and records the name it was asked for.
/// </summary>
internal sealed class FakeDbConnectionFactory : IDbConnectionFactory
{
    public FakeDbConnectionFactory(DataTable result)
    {
        Connection = new FakeDbConnection(result);
    }

    public FakeDbConnection Connection { get; }

    public string? RequestedName { get; private set; }

    public DbConnection Create(string connectionStringName)
    {
        RequestedName = connectionStringName;
        return Connection;
    }
}

/// <summary>
/// A connection whose commands return a fixed table, so the stored procedure runner can be
/// tested without SQL Server. It records whether it was opened and disposed, and the last command.
/// </summary>
internal sealed class FakeDbConnection : DbConnection
{
    private readonly DataTable _result;
    private ConnectionState _state = ConnectionState.Closed;

    public FakeDbConnection(DataTable result)
    {
        _result = result;
    }

    public FakeDbCommand? LastCommand { get; private set; }

    public bool WasDisposed { get; private set; }

    [AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;

    public override string Database => "Fake";

    public override string DataSource => "Fake";

    public override string ServerVersion => "0";

    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName)
    {
        throw new NotSupportedException();
    }

    public override void Close()
    {
        _state = ConnectionState.Closed;
    }

    public override void Open()
    {
        _state = ConnectionState.Open;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        throw new NotSupportedException();
    }

    protected override DbCommand CreateDbCommand()
    {
        LastCommand = new FakeDbCommand(this, _result);
        return LastCommand;
    }

    protected override void Dispose(bool disposing)
    {
        WasDisposed = true;
        base.Dispose(disposing);
    }
}

/// <summary>
/// A command that returns its table's rows. Parameters use a real SqlCommand's collection, so
/// they behave exactly as they would against SQL Server.
/// </summary>
internal sealed class FakeDbCommand : DbCommand
{
    private readonly DataTable _result;
    private readonly SqlCommand _parameters = new();

    public FakeDbCommand(DbConnection connection, DataTable result)
    {
        DbConnection = connection;
        _result = result;
    }

    public bool WasDisposed { get; private set; }

    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;

    public override int CommandTimeout { get; set; }

    public override CommandType CommandType { get; set; }

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; }

    protected override DbParameterCollection DbParameterCollection => _parameters.Parameters;

    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel()
    {
        throw new NotSupportedException();
    }

    public override int ExecuteNonQuery()
    {
        throw new NotSupportedException();
    }

    public override object? ExecuteScalar()
    {
        throw new NotSupportedException();
    }

    public override void Prepare()
    {
        throw new NotSupportedException();
    }

    protected override DbParameter CreateDbParameter()
    {
        return new SqlParameter();
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return _result.CreateDataReader();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _parameters.Dispose();
        }

        WasDisposed = true;
        base.Dispose(disposing);
    }
}
