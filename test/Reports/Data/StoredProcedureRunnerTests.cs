using System.Data;
using System.Data.Common;
using Viper.Areas.Reports.Data;

namespace Viper.test.Reports;

public sealed class StoredProcedureRunnerTests
{
    private static DataTable People()
    {
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add("Ada");
        table.Rows.Add("Bo");
        return table;
    }

    [Fact]
    public async Task QueryAsync_CallsTheProcedureWithTypedParameters()
    {
        var connections = new FakeDbConnectionFactory(People());
        var runner = new StoredProcedureRunner(connections);

        await runner.QueryAsync(
            "PPS",
            "usp_get_people",
            [StoredProcedureParameter.Text("type", "F", 1), StoredProcedureParameter.Date("from", null)],
            record => record.GetString(0),
            TestContext.Current.CancellationToken);

        FakeDbCommand command = Assert.IsType<FakeDbCommand>(connections.Connection.LastCommand);
        Assert.Equal("PPS", connections.RequestedName);
        Assert.Equal(CommandType.StoredProcedure, command.CommandType);
        Assert.Equal("usp_get_people", command.CommandText);
        Assert.Equal(StoredProcedureRunner.CommandTimeoutSeconds, command.CommandTimeout);

        DbParameter[] parameters = [.. command.Parameters.Cast<DbParameter>()];
        Assert.Equal(["@type", "@from"], parameters.Select(parameter => parameter.ParameterName));
        Assert.Equal("F", parameters[0].Value);
        Assert.Equal(DBNull.Value, parameters[1].Value);
    }

    [Fact]
    public async Task QueryAsync_MapsEveryRowAndClosesUp()
    {
        var connections = new FakeDbConnectionFactory(People());
        var runner = new StoredProcedureRunner(connections);

        IReadOnlyList<string> names = await runner.QueryAsync(
            "PPS", "usp_get_people", [], record => record.GetString(0), TestContext.Current.CancellationToken);

        Assert.Equal(["Ada", "Bo"], names);
        Assert.True(connections.Connection.WasDisposed);
        Assert.True(connections.Connection.LastCommand is { WasDisposed: true });
    }

    [Fact]
    public async Task QueryAsync_OpensTheConnectionFirst()
    {
        var connections = new FakeDbConnectionFactory(People());
        var runner = new StoredProcedureRunner(connections);
        ConnectionState stateWhileReading = ConnectionState.Closed;

        await runner.QueryAsync(
            "PPS",
            "usp_get_people",
            [],
            _ => stateWhileReading = connections.Connection.State,
            TestContext.Current.CancellationToken);

        Assert.Equal(ConnectionState.Open, stateWhileReading);
    }

    [Fact]
    public async Task QueryAsync_BadArguments_Throw()
    {
        var runner = new StoredProcedureRunner(new FakeDbConnectionFactory(People()));
        CancellationToken ct = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<ArgumentException>(() => runner.QueryAsync("PPS", " ", [], record => 1, ct));
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.QueryAsync("PPS", "usp_x", null!, record => 1, ct));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => runner.QueryAsync<int>("PPS", "usp_x", [], null!, ct));
    }
}
