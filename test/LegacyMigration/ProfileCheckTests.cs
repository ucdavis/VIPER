using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// The automatic checks built on the shared profile read: what each reports from the profile's counts,
/// and the follow-up queries that list rows by key or name what was found.
/// </summary>
[InvariantCulture]
public class ProfileCheckTests
{
    private static readonly AnalysisPlan Plan = TestSchema.Plan("""{ "dateColumns": { "[dbo].[visitor]": "addDate" } }""");

    private static ProfileRow Row(string table, long rows) => new(Plan.Schema.FindTable(table)!, rows);

    private static List<CheckResult> Run(IAutomaticCheck check, FakeDatabase database) =>
        check.Run(TestSchema.Context(Plan, database)).ToList();

    private static readonly DateTime Jan2020 = new(2020, 1, 2, 0, 0, 0, DateTimeKind.Local);
    private static readonly DateTime Jun2021 = new(2021, 6, 30, 0, 0, 0, DateTimeKind.Local);

    [Fact]
    public void LeadingOrTrailingSpaces_ListsTheRowsByKeyWithTheirDates()
    {
        var database = ProfileRow.Answering(Plan, Row(TestSchema.Visitor, 277).Text("mothraid", leading: 2))
            .Answer("LIKE ' %'", [5, 2L, Jan2020, Jun2021], [9, 2L, Jan2020, Jun2021])
            .Answer("DATALENGTH(RTRIM(");

        var result = Assert.Single(Run(ProfileProblemCheck.LeadingOrTrailingSpaces, database));

        Assert.Equal((CheckStatus.ProblemsFound, TestSchema.Visitor), (result.Status, result.Table));
        Assert.Equal(["mothraid", "varchar(8)", "2", "0"], Assert.Single(Assert.Single(result.Grids).Rows));
        Assert.Contains("mothraid, Leading spaces: rows by id (2): 5, 9; dated 2020-01 to 2021-06", result.Details);
        Assert.Equal("Keep or trim leading and trailing spaces", result.Decision?.Question);
    }

    [Fact]
    public void LeadingOrTrailingSpaces_NothingAnywhere_SaysHowMuchWasChecked()
    {
        var result = Assert.Single(Run(ProfileProblemCheck.LeadingOrTrailingSpaces, ProfileRow.Answering(Plan)));

        Assert.Equal(CheckStatus.NoProblemsFound, result.Status);
        Assert.Equal("Checked 4 tables; none found.", Assert.Single(result.Details));
    }

    [Fact]
    public void UnusualCharacters_EachKindItsOwnQueryWithFreshParameters_AndControlCodesNamed()
    {
        // FakeDatabase refuses a parameter used twice, as SQL Server does, so this also proves each query gets its own.
        var codes = Enumerable.Range(1, 31).Where(c => c is not (9 or 10 or 13)).Select(c => (object?)(c == 0x1C ? 1L : 0L)).ToArray();
        var database = ProfileRow.Answering(Plan, Row(TestSchema.ChangeLog, 1000).Text("detail", unusual: 1))
            .Answer("NCHAR(", codes)
            .Answer("LIKE @control", [7, 1L, null, null])
            .Answer("@mojibake1")
            .Answer("LIKE @replacement");

        var result = Assert.Single(Run(ProfileProblemCheck.UnusualCharacters, database));

        Assert.Contains("detail, Control characters: rows by log_ID (1): 7", result.Details);
        Assert.Contains("detail, control characters: 0x1C (1 row, likely “ cut to one byte)", result.Details);
        Assert.DoesNotContain(result.Details, d => d.Contains("Encoding damage", StringComparison.Ordinal));
    }

    [Fact]
    public void PlaceholderText_ListsEachWordAsStoredWithItsShare()
    {
        var database = ProfileRow.Answering(Plan, Row(TestSchema.Visitor, 277).Text("mothraid", nulls: 2, blanks: 1, placeholders: 3))
            .Answer("COLLATE Latin1_General_BIN2, COUNT_BIG(*)", ["Unknown", 2L], ["N/A", 1L]);

        var result = Assert.Single(Run(new PlaceholderTextCheck(), database));

        Assert.Equal(["mothraid", "varchar(8)", "Unknown 2; N/A 1", "1.1%"], Assert.Single(Assert.Single(result.Grids).Rows));
        Assert.Contains("IN ('None', 'N/A', 'NA', 'NULL', 'Unknown', 'TBD', 'TBA', 'test', '?', '-')", database.AllSql, StringComparison.Ordinal);
    }

    [Fact]
    public void ZeroOrNegativeIds_ForeignKeyHoldingZero_IsListedWithWhyAndADecision()
    {
        var database = ProfileRow.Answering(Plan, Row(TestSchema.Visitor, 277).Number("gradYear", zeros: 4));

        var result = Assert.Single(Run(new ZeroOrNegativeIdsCheck(), database));

        Assert.Equal(["gradYear", "int", "foreign key", "4", "0"], Assert.Single(Assert.Single(result.Grids).Rows));
        Assert.Equal("Decide what 0 or a negative value means in an ID or reference column", result.Decision?.Question);
    }

    [Fact]
    public void AlwaysNumbersOrDates_LeavesPersonColumnsOut()
    {
        var database = ProfileRow.Answering(Plan,
            Row(TestSchema.Visitor, 10).Text("mothraid", numbers: 10),
            Row(TestSchema.Status, 10).Text("Label", numbers: 10));

        var results = Run(ProfileObservationCheck.AlwaysNumbersOrDates, database);

        var result = Assert.Single(results);
        Assert.Equal(TestSchema.Status, result.Table);
        Assert.Equal("Label", Assert.Single(Assert.Single(result.Grids).Rows)[0]);
    }

    [Fact]
    public void ColumnProfile_ListsColumnsAlwaysNullAndEmptyTables()
    {
        var database = ProfileRow.Answering(Plan, Row(TestSchema.Status, 18).Text("Label", nulls: 18), Row(TestSchema.ChangeLog, 0));

        var results = Run(new ColumnProfileCheck(), database);

        Assert.Contains("Always NULL (1): Label", results.Single(r => r.Table == TestSchema.Status).Details);
        Assert.Equal("Empty table.", Assert.Single(results.Single(r => r.Table == TestSchema.ChangeLog).Details));
        Assert.All(results, r => Assert.NotNull(r.ColumnProfiles));
    }

    [Fact]
    public void PersonColumns_ShapeCountsValuesThatAreNotNumbers_AndViewsAreNotRead()
    {
        var database = ProfileRow.Answering(Plan, Row(TestSchema.Visitor, 277).Text("mothraid", nulls: 2, blanks: 1, numbers: 270));

        var results = Run(new PersonColumnsCheck(), database);

        var visitor = results.Single(r => r.Table == TestSchema.Visitor).Grids[0].Rows;
        Assert.Equal("4 of 274 not numbers", visitor.Single(r => r[0] == "mothraid")[4]);
        Assert.Equal("view, not read", Assert.Single(results.Single(r => r.Table == TestSchema.Students).Grids[0].Rows)[4]);
        Assert.DoesNotContain(database.Queries, q => q.Sql.Contains(TestSchema.Students, StringComparison.Ordinal));
    }

    [Fact]
    public void TablesWithNoRecentRows_ListsOldAndEmptyTables_AndCountsUndatedOnes()
    {
        var database = ProfileRow.Answering(Plan, Row(TestSchema.Visitor, 277).Date("addDate", latest: 2012), Row(TestSchema.Status, 0));

        var result = Assert.Single(Run(new TablesWithNoRecentRowsCheck(), database));

        Assert.Equal(
            [["[dbo].[Status]", "-", "empty", "0"], ["[dbo].[visitor]", "addDate", "2012", "277"]],
            Assert.Single(result.Grids).Rows);
        Assert.Contains("Not judged, with no declared date column or no dates in it: 2 tables (declare one under dateColumns to include them).",
            result.Details);
    }
}
