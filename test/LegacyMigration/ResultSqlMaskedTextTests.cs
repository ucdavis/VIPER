using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// A value that is not shown is described by its shape, enough to tell a LoginId from a name or a typo
/// without showing it. ShapeText describes every value but NULL, numbers and digits included.
/// </summary>
[InvariantCulture]
public class ResultSqlMaskedTextTests
{
    [Theory]
    [InlineData(null, "NULL")]
    [InlineData(46957, "<digits only, 5 chars>")]
    [InlineData("00123456", "<digits only, 8 chars>")]
    [InlineData("jsmith", "<letters only, 6 chars>")]
    [InlineData("jsmith  ", "<letters only, 6 chars>")]
    [InlineData("ab12cd", "<letters and digits, 6 chars>")]
    [InlineData("Smith, Jane", "<letters, spaces and other characters, 11 chars>")]
    [InlineData("jsmith@ucdavis.edu", "<an email address, 18 chars>")]
    public void ShapeText_DescribesEveryValueButNull(object? value, string expected)
    {
        Assert.Equal(expected, ResultSql.ShapeText(value));
    }

    [Fact]
    public void ShapeSummary_WithAUnit_NamesEveryCount()
    {
        Assert.Equal("letters only, 6 chars (2 distinct values); digits only, 3 chars (1 distinct value)",
            ResultSql.ShapeSummary(["jsmith", "jdoeaa", "123"], "distinct value"));
        Assert.Equal("letters only, 6 chars (1 distinct value)", ResultSql.ShapeSummary(["jsmith"], "distinct value"));
    }

    [Theory]
    [InlineData("jsmith@ucdavis.edu", "an email address")]
    [InlineData("jsmith@ucdavis", "contains @, with no dot after the @")]
    [InlineData("@ucdavis.edu", "contains @, with nothing before the @")]
    [InlineData("jsmith@", "contains @, with nothing after the @")]
    [InlineData("jsmith@.edu", "contains @, with a dot right after the @ or at the end")]
    [InlineData("jsmith@ucdavis.edu.", "contains @, with a dot right after the @ or at the end")]
    [InlineData("a@b@ucdavis.edu", "contains @, with more than one @")]
    [InlineData("Jane Smith <jsmith@ucdavis.edu>", "contains @, with spaces")]
    [InlineData("a@ucdavis.edu; b@ucdavis.edu", "contains @, with more than one @, spaces and commas or semicolons")]
    public void ShapeSummary_ValueWithAnAt_SaysWhetherItIsAnEmailAddress(string value, string expected)
    {
        Assert.Equal($"{expected}, {value.Length} chars", ResultSql.ShapeSummary([value]));
    }
}
