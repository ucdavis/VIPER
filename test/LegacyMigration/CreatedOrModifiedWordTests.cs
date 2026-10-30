using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// Which date columns say when a row was created or changed, by their name words: the date-column suggestions
/// pick from them, and Rows differing only in audit columns sets them aside. Whole words only, so a column
/// named for something else that happens to start the same way (Module, Additional) is neither.
/// </summary>
[InvariantCulture]
public class CreatedOrModifiedWordTests
{
    [Theory]
    [InlineData("ModDate", true)]
    [InlineData("Modified_date", true)]
    [InlineData("moddate", true)]
    [InlineData("LastMod", true)]
    [InlineData("updt_dt", true)]
    [InlineData("createdOn", true)]
    [InlineData("AddTime", true)]
    [InlineData("ModuleDate", false)]
    [InlineData("AdditionalDate", false)]
    [InlineData("AddressVerified", false)]
    [InlineData("percent_start", false)]
    public void IsCreatedOrModifiedWord_MatchesWholeWordsOnly(string column, bool expected)
    {
        Assert.Equal(expected, PersonIdRules.Words(column).Exists(Suggestions.IsCreatedOrModifiedWord));
    }

    private static TableSummary Table(string name, params (string Column, string Type)[] columns) =>
        new(name, 10, true, true, 0, ["id"], [],
        [
            new ColumnSummary("id", "int", null, null, false, false, null, new IdentitySummary("1", "1", "10")),
            .. columns.Select(c => new ColumnSummary(c.Column, c.Type, null, null, true, false, null, null))
        ]);

    [Fact]
    public void Suggestions_DateColumn_IsNotGuessedFromAWordThatOnlyStartsTheSame()
    {
        var summary = new SchemaSummary
        {
            Database = "Legacy",
            Tables =
            [
                Table("[dbo].[Courses]", ("ModuleDate", "datetime"), ("Modified_date", "datetime")),
                Table("[dbo].[Sections]", ("ModuleDate", "datetime"), ("AdditionalDate", "datetime"))
            ]
        };

        var suggestions = Suggestions.Build(summary, "Legacy", "Production", "analysis.json", new Declarations());

        Assert.Contains("\"[dbo].[Courses]\": \"Modified_date\"", suggestions, StringComparison.Ordinal);
        Assert.Contains("named for modification; others: ModuleDate", suggestions, StringComparison.Ordinal);
        Assert.Contains("//   [dbo].[Sections]: ModuleDate, AdditionalDate", suggestions, StringComparison.Ordinal);
    }

    [Theory]
    // When something happened: compared, so two periods are not one record saved again.
    [InlineData("percent_start", false)]
    // When the row was saved.
    [InlineData("Modified_date", true)]
    [InlineData("LogStamp", true)]
    [InlineData("DateGenerated", true)]
    [InlineData("LastSnapshot", true)]
    public void RowsDifferingOnlyInAuditColumns_DeclaredDateColumn_IsSetAsideOnlyWhenNamedForSavingTheRow(string dateColumn, bool setAside)
    {
        var summary = TestSchema.Summary();
        // UpdatedOn is set aside by its name either way, so the check always has an audit column to run with.
        summary.Tables.Add(Table("[dbo].[tblPercent]", ("percent_Percent", "int"), ("ModuleDate", "datetime"), ("UpdatedOn", "datetime"),
            (dateColumn, "datetime")));
        var plan = TestSchema.Plan($$"""{ "dateColumns": { "[dbo].[tblPercent]": "{{dateColumn}}" } }""", summary);
        var database = new FakeDatabase().AnswerWith("SELECT COUNT_BIG(*), SUM(x.n)", _ => [[0L, null, null, null, null]]);

        _ = new AuditOnlyDifferencesCheck().Run(TestSchema.Context(plan, database)).ToList();

        var sql = database.Queries.Single(q => q.Sql.Contains("[dbo].[tblPercent]", StringComparison.Ordinal)).Sql;
        Assert.Equal(setAside, sql.Contains($"MIN(t.[{dateColumn}]) <> MAX(t.[{dateColumn}])", StringComparison.Ordinal));
        // ModuleDate is never an audit column, so it is always compared.
        Assert.DoesNotContain("MIN(t.[ModuleDate]) <> MAX", sql, StringComparison.Ordinal);
    }
}
