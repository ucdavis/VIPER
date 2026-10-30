using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// ErrorText decides what of an exception's message reaches the report. SQL Server and .NET quote the
/// value that failed in some messages, so quoted text must come out as a shape unless it is a name from the
/// export, and a check that throws must fail alone with its message masked the same way.
/// </summary>
[InvariantCulture]
public class ResultSqlErrorTextTests
{
    private static string ErrorText(string message) =>
        ResultSql.ErrorText(new InvalidOperationException(message), TestSchema.Plan("{}").Schema);

    [Theory]
    [InlineData("Conversion failed when converting the varchar value 'jsmith' to data type int.",
        "Conversion failed when converting the varchar value <letters only, 6 chars> to data type int.")]
    [InlineData("The input string 'jsmith@ucdavis.edu' was not in a correct format.",
        "The input string <an email address, 18 chars> was not in a correct format.")]
    [InlineData("Converting the varchar value 'O'Brien' to data type int.",
        "Converting the varchar value <letters and other characters, 7 chars> to data type int.")]
    [InlineData("Invalid object name 'dbo.secret'.", "Invalid object name <letters and other characters, 10 chars>.")]
    [InlineData("Unable to cast object of type 'System.jsmith' to type 'System.Int32'.",
        "Unable to cast object of type <letters and other characters, 13 chars> to type 'System.Int32'.")]
    [InlineData("Value 'jsmith, System.Private.CoreLib' is not valid.", "Value <letters, spaces and other characters, 30 chars> is not valid.")]
    public void ErrorText_QuotedValue_IsDescribedByShape(string message, string expected)
    {
        Assert.Equal(expected, ErrorText(message));
    }

    [Theory]
    [InlineData("Invalid column name 'mothraid'.")]
    [InlineData("Invalid object name 'dbo.visitor'.")]
    [InlineData("Invalid object name '[dbo].[visitor]'.")]
    [InlineData("The multi-part identifier \"t.gradYear\" could not be bound.")]
    [InlineData("The check can't run.")]
    [InlineData("Unable to cast object of type 'System.String' to type 'System.Int32'.")]
    [InlineData("Unable to cast object of type 'System.Nullable`1[System.DateTime]' to type 'System.Decimal'.")]
    [InlineData("Object of type 'System.Collections.Generic.List`1[System.String]' cannot be converted.")]
    public void ErrorText_NamesFromTheExportAndApostrophesInWords_AreKept(string message)
    {
        Assert.Equal(message, ErrorText(message));
    }

    [Fact]
    public void ErrorText_UnclosedQuote_WithholdsTheRest()
    {
        Assert.Equal("The input string <rest of message withheld>", ErrorText("The input string 'jsmith was not in a correct format."));
    }

    [Fact]
    public void RunOne_CheckThatThrowsAnyException_FailsAloneWithItsValueMasked()
    {
        var plan = TestSchema.Plan("{}");
        var context = TestSchema.Context(plan, new FakeDatabase());

        var result = Assert.Single(DataAnalysis.RunOne(context,
            () => throw new FormatException("The input string 'jsmith' was not in a correct format."),
            "x", null, Severity.Warning, TestSchema.Visitor));

        Assert.Equal(CheckStatus.FailedToRun, result.Status);
        Assert.StartsWith("Failed to run: FormatException: The input string <letters only, 6 chars> was not in a correct format. (a fault in the check, at ",
            result.Message, StringComparison.Ordinal);
    }
}
