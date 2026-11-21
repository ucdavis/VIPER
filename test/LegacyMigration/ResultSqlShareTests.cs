using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

[InvariantCulture]
public class ResultSqlShareTests
{
    [Theory]
    [InlineData(8318, 78473, "10.6%")]
    [InlineData(69, 135, "51.1%")]
    [InlineData(0, 100, "0%")]
    [InlineData(100, 100, "100%")]
    [InlineData(1, 1000, "0.1%")]
    [InlineData(1, 3371, "<0.1%")]
    [InlineData(3370, 3371, ">99.9%")]
    public void Share_RoundsToOneDecimal_ButNeverToNoneOrAll(long part, long whole, string expected)
    {
        Assert.Equal(expected, ResultSql.Share(part, whole));
    }
}
