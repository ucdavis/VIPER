using System.Data;
using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// Values read from a row and sent back to find that row. Sent untyped, a DateTime goes as datetime,
/// which rounds to 3 ms and fails before 1753, so a date or datetime2 value would miss its own rows.
/// </summary>
[InvariantCulture]
public class ResultSqlParameterTests
{
    private static readonly string[] Types =
        ["date", "datetime", "smalldatetime", "datetime2(3)", "datetime2(7)", "time(0)", "datetimeoffset(2)", "varchar(20)"];

    // Finer than datetime's 3 ms.
    private static readonly DateTime Precise = new DateTime(2024, 5, 1, 8, 30, 0, 123, DateTimeKind.Local).AddTicks(4567);

    private static readonly DateTime Year1 = new(1, 1, 1, 0, 0, 0, DateTimeKind.Local);

    private static ColumnRef Column(string type)
    {
        var columns = Types.Select((t, i) => new ColumnSummary($"c{i}", t, null, null, true, false, null, null)).ToList();
        var summary = new SchemaSummary { Tables = [new TableSummary("[dbo].[dates]", 10, true, true, 0, ["c0"], [], columns)] };
        return new SchemaIndex(summary, new HashSet<string>()).FindTable("[dbo].[dates]")!.Columns[Array.IndexOf(Types, type)];
    }

    private static object ValueFor(string type) => type switch
    {
        _ when type.StartsWith("time", StringComparison.Ordinal) => new TimeSpan(0, 8, 30, 0, 123),
        _ when type.StartsWith("datetimeoffset", StringComparison.Ordinal) => new DateTimeOffset(2024, 5, 1, 8, 30, 0, TimeSpan.FromHours(-7)),
        _ => Precise
    };

    [Theory]
    [InlineData("date", SqlDbType.Date, 0)]
    [InlineData("datetime2(3)", SqlDbType.DateTime2, 3)]
    [InlineData("datetime2(7)", SqlDbType.DateTime2, 7)]
    [InlineData("time(0)", SqlDbType.Time, 0)]
    [InlineData("datetimeoffset(2)", SqlDbType.DateTimeOffset, 2)]
    public void Parameter_DateOrTimeColumn_IsTypedAsTheColumn(string type, SqlDbType expected, byte scale)
    {
        var value = ValueFor(type);

        var parameter = ResultSql.Parameter("@p", value, Column(type));

        Assert.Equal(expected, parameter.SqlDbType);
        Assert.Equal(scale, parameter.Scale);
        Assert.Equal(value, parameter.Value);
    }

    // A datetime value reads as whole milliseconds, so it matches its row only when sent back as datetime.
    [Theory]
    [InlineData("datetime")]
    [InlineData("smalldatetime")]
    public void Parameter_DatetimeColumn_StaysDatetime(string type)
    {
        var parameter = ResultSql.Parameter("@p", new DateTime(2024, 5, 1, 8, 30, 0, 3, DateTimeKind.Local), Column(type));

        Assert.Equal(SqlDbType.DateTime, parameter.SqlDbType);
    }

    [Fact]
    public void Parameter_DateBefore1753_KeepsItsValue()
    {
        var parameter = ResultSql.Parameter("@p", Year1, Column("date"));

        Assert.Equal(SqlDbType.Date, parameter.SqlDbType);
        Assert.Equal(Year1, parameter.Value);
    }

    [Fact]
    public void Parameter_Null_IsDbNull()
    {
        Assert.Equal(DBNull.Value, ResultSql.Parameter("@p", null, Column("datetime2(7)")).Value);
    }

    [Fact]
    public void MatchAny_TypesTheLeadingExpressionsByColumn_AndTheRestByValue()
    {
        var (sql, parameters) = ResultSql.MatchAny(
            ["t.[c3]", "t.[c7]", "DATALENGTH(t.[c7])"], [Column("datetime2(3)"), Column("varchar(20)")], [[Precise, "a", 1]], "g");

        Assert.Equal("(t.[c3] = @g0_0 AND t.[c7] = @g0_1 AND DATALENGTH(t.[c7]) = @g0_2)", sql);
        Assert.Equal([SqlDbType.DateTime2, SqlDbType.NVarChar, SqlDbType.Int], parameters.Select(p => p.SqlDbType));
        Assert.Equal(3, parameters[0].Scale);
    }
}
