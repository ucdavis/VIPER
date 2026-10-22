using Viper.Areas.Personnel.Services;

namespace Viper.test.Personnel;

/// <summary>
/// The MyInfoVault procedures are read by column name. A null value reads as null, but a column
/// the procedure no longer returns fails clearly instead of showing blank data.
/// </summary>
public sealed class EisDataServiceTests
{
    [Fact]
    public void Field_ReadsAColumnIgnoringCaseAndKeepsNulls()
    {
        var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Location"] = "Davis, CA",
            ["EndDate"] = null,
        };

        Assert.Equal("Davis, CA", EisDataService.Field(row, "location"));
        Assert.Null(EisDataService.Field(row, "EndDate"));
    }

    [Fact]
    public void Field_FailsWhenTheColumnIsMissing()
    {
        var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { ["Degree"] = "DVM" };

        var error = Assert.Throws<InvalidOperationException>(() => EisDataService.Field(row, "Institution"));
        Assert.Contains("Institution", error.Message, StringComparison.Ordinal);
    }
}
