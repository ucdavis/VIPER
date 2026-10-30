using System.Globalization;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// report.txt is what people read, so its layout is part of the tool: sections in a fixed order, the most
/// severe first, design observations combined into one table, and counts laid out as tables.
/// </summary>
[InvariantCulture]
public class AnalysisReportTests
{
    private static CheckResult Result(string name, string? table, ResultKind kind = ResultKind.Problem, Severity severity = Severity.Warning,
        CheckStatus status = CheckStatus.ProblemsFound) =>
        new() { Name = name, Table = table, Kind = kind, Severity = severity, Status = status };

    private static string Report(List<CheckResult> results, List<string>? include = null, IReadOnlyList<string>? changes = null,
        List<MaskedColumn>? masked = null) =>
        AnalysisReport.ToText(
            new AnalysisRun("ClinicalScheduler", null, "Production", "server", "db", "2026-10-27 09:00:00", "2026-10-27 10:00:00", "1.0s",
                null, include, new ObjectCoverage(312, include != null ? 308 : 0, 0), new ObjectCoverage(6, 0, 0), results, masked),
            changes ?? ["(no earlier run in this folder)"]);

    [Fact]
    public void ToText_MaskedColumns_AreListedByTableBeforeTheDecisions()
    {
        var lines = Lines(Report([], masked:
        [
            new MaskedColumn("[dbo].[visitor]", "startWeekID"), new MaskedColumn("[dbo].[Status]", "Label"),
            new MaskedColumn("[dbo].[visitor]", "gradYear")
        ]));

        var heading = lines.FindIndex(l => l.StartsWith("VALUES DESCRIBED BY SHAPE (3 columns): declare under safeColumns to show", StringComparison.Ordinal));
        Assert.Equal(["  [dbo].[Status]: Label", "  [dbo].[visitor]: gradYear, startWeekID"], lines.Skip(heading + 1).Take(2));
        Assert.True(heading < lines.FindIndex(l => l.StartsWith("DECISIONS FOR THE MIGRATION", StringComparison.Ordinal)));
    }

    [Fact]
    public void ToText_PersonIdColumns_AreListedApartWithTheShapesOfTheirHiddenValues()
    {
        var lines = Lines(Report([], masked:
        [
            new MaskedColumn("[dbo].[tblAudit]", "audit_TermCode"),
            new MaskedColumn("[dbo].[tblAudit]", "audit_MothraID", IsPersonId: true, Shapes: "letters only, 6-8 chars (4 distinct values)"),
            new MaskedColumn("[dbo].[tblEffort]", "effort_MothraID", IsPersonId: true)
        ]));

        Assert.Contains("VALUES DESCRIBED BY SHAPE (1 column): declare under safeColumns to show", lines);
        Assert.Contains("  [dbo].[tblAudit]: audit_TermCode", lines);
        var heading = lines.IndexOf("PERSON ID COLUMNS WITH VALUES THAT ARE NOT IDS (2 columns)");
        Assert.StartsWith("  Values of all digits", lines[heading + 1], StringComparison.Ordinal);
        Assert.Equal(["  [dbo].[tblAudit].audit_MothraID: letters only, 6-8 chars (4 distinct values)", "  [dbo].[tblEffort].effort_MothraID"], lines.Skip(heading + 4).Take(2));
    }

    [Fact]
    public void ToText_RunsUnderTheInvariantCulture_SoCountsReadTheSameOnEveryMachine()
    {
        var result = Result("notBlank: notes", "[dbo].[visitor]");
        result.Counts.Add(new ResultCount("NULL rows", 1234));

        Assert.Same(CultureInfo.InvariantCulture, CultureInfo.CurrentCulture);
        Assert.Contains("      NULL rows: 1,234", Lines(Report([result])));
    }

    [Fact]
    public void ToText_NothingMasked_HasNoMaskedSection()
    {
        Assert.DoesNotContain("VALUES DESCRIBED BY SHAPE", Report([]), StringComparison.Ordinal);
    }

    private static List<string> Lines(string report) => report.Split("\r\n").SelectMany(l => l.Split('\n')).ToList();

    [Fact]
    public void ToText_Sections_ComeInTheirFixedOrder()
    {
        var lines = Lines(Report(
        [
            Result("Deprecated types", "[dbo].[Service]", ResultKind.DesignObservation, Severity.Info),
            Result("Objects that change data", null, ResultKind.Context, Severity.Info, CheckStatus.NoProblemsFound),
            Result("notBlank: notes", "[dbo].[visitor]", status: CheckStatus.NoProblemsFound),
            Result("Duplicate rows", "[dbo].[Week]")
        ]));

        var headings = new[] { "DECISIONS FOR THE MIGRATION", "CHANGES SINCE THE LAST RUN", "PROBLEMS (", "NO PROBLEMS FOUND (", "CONTEXT (", "DESIGN OBSERVATIONS (", "TOTALS" }
            .Select(h => lines.FindIndex(l => l.StartsWith(h, StringComparison.Ordinal))).ToList();
        Assert.DoesNotContain(-1, headings);
        Assert.Equal(headings.Order().ToList(), headings);
    }

    [Fact]
    public void ToText_Problems_MostSevereFirst()
    {
        var report = Report(
        [
            Result("Leading or trailing spaces", "[dbo].[visitor]", severity: Severity.Warning),
            Result("Unenforced foreign keys", "[dbo].[visitor]", severity: Severity.Critical)
        ]);

        Assert.True(report.IndexOf("[CRITICAL] Unenforced foreign keys", StringComparison.Ordinal)
            < report.IndexOf("[WARNING] Leading or trailing spaces", StringComparison.Ordinal));
    }

    [Fact]
    public void ToText_PassingProblemCheck_HasNeitherSeverityNorStatus()
    {
        var lines = Lines(Report([Result("notBlank: notes", "[dbo].[visitor]", status: CheckStatus.NoProblemsFound)]));

        Assert.Contains("  notBlank: notes [dbo].[visitor]", lines);
    }

    [Fact]
    public void ToText_LabelSubject_IsInParentheses()
    {
        var lines = Lines(Report([Result("Related columns with different types", "columns named gradyear", ResultKind.DesignObservation, Severity.Info)]));

        Assert.Contains("  [INFO] Related columns with different types (columns named gradyear): found", lines);
    }

    [Fact]
    public void ToText_Decisions_OneLinePerQuestionAndCheckWithTotals()
    {
        CheckResult Flags(string table, int columns)
        {
            var result = Result("Bit columns holding NULL", table, ResultKind.DesignObservation, Severity.Info);
            result.Decision = new ResultDecision("Decide what NULL means in yes/no flags", columns, 1);
            return result;
        }

        var lines = Lines(Report([Flags("[dbo].[Rotation]", 2), Flags("[dbo].[Service]", 1)]));

        Assert.Contains("DECISIONS FOR THE MIGRATION (1)", lines);
        Assert.Contains("  Decide what NULL means in yes/no flags: columns 3, tables 2 (see Bit columns holding NULL)", lines);
    }

    [Fact]
    public void ToText_Changes_AreIndentedUnderTheirHeading()
    {
        var lines = Lines(Report([], changes: ["Last run: 2026-10-27 09:00:00.", "Unchanged results: 5"]));

        var at = lines.IndexOf("CHANGES SINCE THE LAST RUN");
        Assert.Equal(["  Last run: 2026-10-27 09:00:00.", "  Unchanged results: 5"], lines.Skip(at + 1).Take(2).ToList());
    }

    [Fact]
    public void ToText_DesignObservationOnSeveralTables_IsOneTableAcrossThem()
    {
        CheckResult Deprecated(string table, string column)
        {
            var result = Result("Deprecated types", table, ResultKind.DesignObservation, Severity.Info);
            result.Grids.Add(new ResultGrid("Columns", ["Column"], ["Type"], [[column, "text"]], 1));
            return result;
        }

        var report = Report([Deprecated("[dbo].[Service]", "Description"), Deprecated("[dbo].[Didactic]", "Notes")]);

        Assert.Single(Lines(report), l => l.Contains("[INFO] Deprecated types", StringComparison.Ordinal));
        Assert.Contains("      Tables: 2", Lines(report));
        Assert.Matches(@"Table\s+Column\s+Type", report);
        Assert.Matches(@"\[dbo\]\.\[Didactic\]\s+Notes\s+text", report);
    }

    [Fact]
    public void ToText_DesignObservationWithAnOverride_KeepsItsOwnBlock()
    {
        var overridden = Result("Deprecated types", "[dbo].[Service]", ResultKind.DesignObservation);
        overridden.Message = "Severity changed: needed before the migration.";
        overridden.Grids.Add(new ResultGrid("Columns", ["Column"], ["Type"], [["Description", "text"]], 1));
        var plain = Result("Deprecated types", "[dbo].[Didactic]", ResultKind.DesignObservation, Severity.Info);
        plain.Grids.Add(new ResultGrid("Columns", ["Column"], ["Type"], [["Notes", "text"]], 1));

        var lines = Lines(Report([overridden, plain]));

        Assert.Contains("  [WARNING] Deprecated types [dbo].[Service]: found", lines);
        Assert.Contains("      Severity changed: needed before the migration.", lines);
    }

    [Fact]
    public void ToText_CategoryMeasureCounts_AreOneTable()
    {
        var result = Result("identifiesOnePerson: Mothra_ID", "[dbo].[studentSchedule]");
        result.Counts.AddRange([
            new ResultCount("Found one person: IDs", 2530), new ResultCount("Found one person: rows", 137881),
            new ResultCount("Not found: IDs", 1), new ResultCount("Not found: rows", 4)]);

        var report = Report([result]);

        Assert.Matches(@"Category\s+IDs\s+Rows", report);
        Assert.Matches(@"Found one person\s+2,530\s+137,881", report);
    }

    [Fact]
    public void ToText_ZeroCounts_LeftOutBesideOthersButShownWhenAllAreZero()
    {
        var mixed = Result("withinRange: effort_Hours", "[dbo].[tblEffort]");
        mixed.Counts.AddRange([new ResultCount("Rows below 0", 0), new ResultCount("Rows above 2500", 3)]);
        var zero = Result("withinRange: effort_Weeks", "[dbo].[tblEffort]", status: CheckStatus.NoProblemsFound);
        zero.Counts.AddRange([new ResultCount("Rows below 0", 0), new ResultCount("Rows above 52", 0)]);

        var lines = Lines(Report([mixed, zero]));

        Assert.Contains("      Rows above 2500: 3", lines);
        Assert.DoesNotContain("      Rows below 0: 0", lines.TakeWhile(l => !l.StartsWith("NO PROBLEMS FOUND", StringComparison.Ordinal)));
        Assert.Contains("      Rows above 52: 0", lines);
    }

    [Fact]
    public void ToText_KeysOnlyGrid_IsGroupedByItsLeadingColumn()
    {
        var result = Result("sameKeysAs", "[dbo].[SVM_Phones]");
        result.Grids.Add(new ResultGrid("Only in [dbo].[SVM_Phones]", ["sectionID", "unitID"], [], [["3", "5"], ["3", "7"], ["3", "10"], ["4", "1"]], 4));

        var lines = Lines(Report([result]));

        Assert.Contains("        sectionID 3: unitID 5, 7, 10", lines);
        Assert.Contains("        sectionID 4: unitID 1", lines);
    }

    [Fact]
    public void ToText_Include_HeaderSaysHowMuchWasChecked()
    {
        var lines = Lines(Report([], include: ["[dbo].[visitor]"]));

        Assert.Contains("  Include:       [dbo].[visitor]", lines);
        Assert.Contains("  Tables:        4 of 312 checked automatically (308 not in the include list)", lines);
    }
}
