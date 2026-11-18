using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>SQL Server type names map to the kinds of value a report can show.</summary>
public sealed class ReportSqlTypesTests
{
    [Theory]
    [InlineData("nvarchar", ReportValueKind.Text)]
    [InlineData("nvarchar(50)", ReportValueKind.Text)]
    [InlineData("VARCHAR(max)", ReportValueKind.Text)]
    [InlineData("int", ReportValueKind.WholeNumber)]
    [InlineData("decimal(9,2)", ReportValueKind.FractionalNumber)]
    [InlineData("money", ReportValueKind.FractionalNumber)]
    [InlineData("date", ReportValueKind.Date)]
    [InlineData("datetime2(7)", ReportValueKind.DateTime)]
    [InlineData("time", ReportValueKind.Time)]
    [InlineData("bit", ReportValueKind.Boolean)]
    [InlineData("uniqueidentifier", ReportValueKind.UniqueId)]
    [InlineData("varbinary(max)", ReportValueKind.Binary)]
    [InlineData("sql_variant", ReportValueKind.Other)]
    [InlineData(" ", ReportValueKind.Other)]
    [InlineData(null, ReportValueKind.Other)]
    public void KindOf_IgnoresLengthAndCase(string? sqlType, ReportValueKind expected)
    {
        Assert.Equal(expected, ReportSqlTypes.KindOf(sqlType));
    }

    [Theory]
    [InlineData(ReportValueKind.Text, true)]
    [InlineData(ReportValueKind.UniqueId, true)]
    [InlineData(ReportValueKind.Binary, false)]
    [InlineData(ReportValueKind.Other, false)]
    public void IsSupported_RejectsBinaryAndUnknownValues(ReportValueKind kind, bool expected)
    {
        Assert.Equal(expected, ReportSqlTypes.IsSupported(kind));
    }
}
