using System.Data;
using System.Data.SqlTypes;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// How the analysis reads each value from a query. A decimal(38) value can be larger than .NET's decimal,
/// which GetValue would throw on and cost the run every check that reads the table.
/// </summary>
[InvariantCulture]
public class AnalysisDatabaseValueTests
{
    [Fact]
    public void DecimalValue_FitsDotNetDecimal_IsADecimal()
    {
        Assert.Equal(12345.67m, AnalysisDatabase.DecimalValue(SqlDecimal.Parse("12345.67")));
    }

    [Fact]
    public void DecimalValue_TooLargeForDotNetDecimal_IsItsText()
    {
        const string digits = "12345678901234567890123456789012345678";

        Assert.Equal(digits, AnalysisDatabase.DecimalValue(SqlDecimal.Parse(digits)));
    }

    [Theory]
    [InlineData("decimal", true)]
    [InlineData("numeric", true)]
    [InlineData("money", false)]
    [InlineData("smallmoney", false)]
    [InlineData("int", false)]
    public void IsSqlDecimal_OnlyDecimalAndNumeric_AreReadAsSqlDecimal(string dataTypeName, bool expected)
    {
        Assert.Equal(expected, AnalysisDatabase.IsSqlDecimal(dataTypeName));
    }

    [Fact]
    public void ValueOf_OtherReader_GivesItsValueAndNullForNull()
    {
        using var table = new DataTable();
        table.Columns.Add("a", typeof(decimal));
        table.Rows.Add(1.5m);
        table.Rows.Add(DBNull.Value);
        using var reader = table.CreateDataReader();

        var values = new List<object?>();
        while (reader.Read())
        {
            values.Add(AnalysisDatabase.ValueOf(reader, 0));
        }

        Assert.Equal([1.5m, null], values);
    }
}
