using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// The comparison reads the last run back from results.json and reports what moved since, so a run that
/// cannot be read back the way it was written would silently compare against nothing.
/// </summary>
[InvariantCulture]
public class RunComparisonTests
{
    private static RunSnapshot Run(params ResultSnapshot[] results) => new() { RanAt = "2026-10-27 10:00:00", Results = [.. results] };

    private static ResultSnapshot Result(string name, string? table, CheckStatus status, ResultKind kind = ResultKind.Problem,
        List<ResultCount>? counts = null, List<string>? examples = null, long? exampleTotal = null, string? latest = null,
        List<GridSnapshot>? grids = null) => new()
        {
            Name = name,
            Table = table,
            Kind = kind,
            Severity = Severity.Warning,
            Status = status,
            Counts = counts ?? [],
            Examples = examples ?? [],
            ExampleTotal = exampleTotal ?? examples?.Count ?? 0,
            Latest = latest,
            Grids = grids ?? []
        };

    private static GridSnapshot Grid(string title, List<string> keyColumns, List<List<string>> rows) =>
        new() { Title = title, KeyColumns = keyColumns, Rows = rows, Total = rows.Count };

    private static ResultSnapshot Observation(GridSnapshot grid) =>
        Result("Date and time columns at midnight", "[dbo].[ServiceRequest]", CheckStatus.ProblemsFound, ResultKind.DesignObservation,
            grids: [grid]);

    [Fact]
    public void Lines_GridRowWhoseValuesMoved_IsOneChangedRowByKey()
    {
        var lines = RunComparison.Lines(
            Run(Observation(Grid("Columns", ["Column"], [["Create_Date", "135"], ["Modified_Date", "2,357"], ["Old_Date", "4"]]))),
            Run(Observation(Grid("Columns", ["Column"], [["Create_Date", "136"], ["Modified_Date", "2,357"], ["New_Date", "1"]]))));

        Assert.Contains("      Columns added (1): New_Date", lines);
        Assert.Contains("      Columns removed (1): Old_Date", lines);
        Assert.Contains("      Columns changed (1): Create_Date", lines);
        Assert.DoesNotContain(lines, l => l.Contains("Modified_Date", StringComparison.Ordinal));
    }

    [Fact]
    public void Lines_KeyColumnBlankOnSomeRows_HeadsABlockAndIsNotPartOfTheKey()
    {
        var lines = RunComparison.Lines(
            Run(Observation(Grid("Groups", ["Group", "Table"], [["9 shared columns", "[dbo].[A]", "12,194"], ["", "[dbo].[B]", "2,052"]]))),
            Run(Observation(Grid("Groups", ["Group", "Table"], [["9 shared columns", "[dbo].[A]", "12,200"], ["", "[dbo].[B]", "2,052"]]))));

        Assert.Contains("      Groups changed (1): [dbo].[A]", lines);
    }

    [Fact]
    public void Snapshot_ReadsBackWhatResultsJsonHolds()
    {
        var result = new CheckResult
        {
            Name = "matchesPattern: emailTo",
            Kind = ResultKind.Problem,
            Severity = Severity.Warning,
            Table = "[dbo].[externalEmailSent]",
            Status = CheckStatus.ProblemsFound,
            Latest = "2025-12-02",
            ExampleTotal = 2
        };
        result.Counts.Add(new ResultCount("Rows that match no pattern", 12));
        result.Examples.AddRange(["3368", "3470"]);
        result.Grids.Add(new ResultGrid("Columns", ["Column"], ["Type"], [["role", "varchar(15)"]], 1));
        var run = new AnalysisRun("ClinicalScheduler", null, "Production", "server", "db", "2026-10-27", "2026-10-27 10:00:00", "1.0s",
            null, null, new ObjectCoverage(1, 0, 0), new ObjectCoverage(0, 0, 0), [result]);

        var snapshot = RunComparison.Snapshot(run);

        var read = Assert.Single(snapshot.Results);
        Assert.Equal("2026-10-27 10:00:00", snapshot.RanAt);
        Assert.Equal(("matchesPattern: emailTo", "[dbo].[externalEmailSent]"), (read.Name, read.Table));
        Assert.Equal((ResultKind.Problem, Severity.Warning, CheckStatus.ProblemsFound), (read.Kind, read.Severity, read.Status));
        Assert.Equal(12, Assert.Single(read.Counts).Value);
        Assert.Equal(["3368", "3470"], read.Examples);
        Assert.Equal("2025-12-02", read.Latest);
        Assert.Equal(["role", "varchar(15)"], Assert.Single(Assert.Single(read.Grids).Rows));
    }

    [Fact]
    public void Lines_NewAndGoneFindings_AreListedWithWhatTheyWere()
    {
        var lines = RunComparison.Lines(
            Run(Result("Duplicate rows", "[dbo].[Week]", CheckStatus.ProblemsFound),
                Result("notBlank: role", "[dbo].[InstructorSchedule]", CheckStatus.NoProblemsFound)),
            Run(Result("notBlank: role", "[dbo].[InstructorSchedule]", CheckStatus.ProblemsFound),
                Result("matchesPattern: emailTo", "[dbo].[externalEmailSent]", CheckStatus.ProblemsFound)));

        Assert.Contains("Found now, not in the last run (2):", lines);
        Assert.Contains("  [WARNING] notBlank: role [dbo].[InstructorSchedule] (was no problems found)", lines);
        Assert.Contains("  [WARNING] matchesPattern: emailTo [dbo].[externalEmailSent] (not in the last run)", lines);
        Assert.Contains("Found in the last run, not now (1):", lines);
        Assert.Contains("  [WARNING] Duplicate rows [dbo].[Week] (not in this run)", lines);
    }

    [Fact]
    public void Lines_FoundBothTimes_ListsWhatMoved()
    {
        var lines = RunComparison.Lines(
            Run(Result("matchesPattern: emailTo", "[dbo].[externalEmailSent]", CheckStatus.ProblemsFound,
                counts: [new ResultCount("Rows that match no pattern", 12)], examples: ["3368", "3470"], latest: "2025-12-02")),
            Run(Result("matchesPattern: emailTo", "[dbo].[externalEmailSent]", CheckStatus.ProblemsFound,
                counts: [new ResultCount("Rows that match no pattern", 14)], examples: ["3368", "3470", "7400"], latest: "2026-10-20")));

        Assert.Contains("Found both times, changed (1):", lines);
        Assert.Contains("      Rows that match no pattern: 12 to 14", lines);
        Assert.Contains("      Latest affected row: 2025-12-02 to 2026-10-20", lines);
        Assert.Contains("      Keys not listed before (1): 7400", lines);
        Assert.Contains("Unchanged results: 0", lines);
    }

    [Fact]
    public void Lines_KeysBeyondTheListedOnes_AreNotCalledNew()
    {
        // The last run listed 2 of its 60 keys, so 7400 may have been among the 58 not listed.
        var lines = RunComparison.Lines(
            Run(Result("Duplicate rows", "[dbo].[Week]", CheckStatus.ProblemsFound, examples: ["1", "2"], exampleTotal: 60)),
            Run(Result("Duplicate rows", "[dbo].[Week]", CheckStatus.ProblemsFound, examples: ["1", "7400"], exampleTotal: 60)));

        Assert.DoesNotContain(lines, l => l.Contains("Keys", StringComparison.Ordinal));
        Assert.Contains("Unchanged results: 1", lines);
    }

    [Fact]
    public void Lines_ContextAndUnchangedResults_AreCountedNotListed()
    {
        var lines = RunComparison.Lines(
            Run(Result("Column profile", "[dbo].[Week]", CheckStatus.ProblemsFound, ResultKind.Context, [new ResultCount("Rows", 1000)]),
                Result("Deprecated types", "[dbo].[Service]", CheckStatus.ProblemsFound, ResultKind.DesignObservation)),
            Run(Result("Column profile", "[dbo].[Week]", CheckStatus.ProblemsFound, ResultKind.Context, [new ResultCount("Rows", 1012)]),
                Result("Deprecated types", "[dbo].[Service]", CheckStatus.ProblemsFound, ResultKind.DesignObservation)));

        Assert.Equal(2, lines.Count);
        Assert.Equal("Unchanged results: 2", lines[1]);
    }

    [Fact]
    public void Lines_StatusChangeWithoutFindings_IsAnOtherStatusChange()
    {
        var lines = RunComparison.Lines(
            Run(Result("canConvertTo: effort_Role as int", "[dbo].[tblEffort]", CheckStatus.NoProblemsFound)),
            Run(Result("canConvertTo: effort_Role as int", "[dbo].[tblEffort]", CheckStatus.FailedToRun)));

        Assert.Contains("  [WARNING] canConvertTo: effort_Role as int [dbo].[tblEffort] (was no problems found, now failed to run)", lines);
    }
}
