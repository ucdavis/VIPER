using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// The automatic checks that find rows by their own grouped or joined reads: unenforced foreign keys,
/// duplicate rows with their declared keys, and rows differing only in audit columns. Rows are reported
/// by key, and a person column's values only as shapes when they may be LoginIds.
/// </summary>
[InvariantCulture]
public class KeyedCheckTests
{
    [Fact]
    public void UnenforcedForeignKeys_OrphanRows_AreListedByTheChildKey()
    {
        var plan = TestSchema.Plan("{}", TestSchema.WithUntrustedForeignKey());
        var database = new FakeDatabase()
            .Answer("SELECT COUNT_BIG(*) FROM (SELECT DISTINCT", [2L])
            .Answer("NOT EXISTS (SELECT 1 FROM [dbo].[Status]", [12, 3L, null, null], [15, 3L, null, null], [20, 3L, null, null]);

        var results = new UnenforcedForeignKeyCheck(afterCleanup: false).Run(TestSchema.Context(plan, database)).ToList();

        var result = Assert.Single(results);
        Assert.Equal((CheckStatus.ProblemsFound, Severity.Critical, TestSchema.Visitor), (result.Status, result.Severity, result.Table));
        Assert.Equal([("Rows with no parent", 3L), ("Distinct values", 2L)], result.Counts.Select(c => (c.Label, c.Value)).ToList());
        Assert.Equal(["12", "15", "20"], result.Examples);
        Assert.Contains("t.[gradYear] IS NOT NULL", database.Queries[0].Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void UnenforcedForeignKeys_NoOrphans_ListsTheKeyAsSafeToReTrust()
    {
        var plan = TestSchema.Plan("{}", TestSchema.WithUntrustedForeignKey());
        var database = new FakeDatabase().Answer("NOT EXISTS (SELECT 1 FROM [dbo].[Status]");

        var result = Assert.Single(new UnenforcedForeignKeyCheck(afterCleanup: false).Run(TestSchema.Context(plan, database)));

        Assert.Equal(CheckStatus.NoProblemsFound, result.Status);
        Assert.Contains("No missing parents (1): FK_visitor_Status", result.Details);
    }

    [Fact]
    public void UnenforcedForeignKeys_OneKeyFails_TheOthersKeepTheirResults()
    {
        var summary = TestSchema.WithUntrustedForeignKey();
        summary = new SchemaSummary
        {
            Database = summary.Database,
            CompatibilityLevel = summary.CompatibilityLevel,
            Tables = summary.Tables,
            Views = summary.Views,
            PersonIdColumns = summary.PersonIdColumns,
            ForeignKeys = [summary.ForeignKeys[0], summary.ForeignKeys[0] with { Name = "FK_visitor_StatusEnd", Columns = ["endWeekID"] }]
        };
        var plan = TestSchema.Plan("{}", summary);
        var database = new FakeDatabase()
            .AnswerWith("t.[gradYear] IS NOT NULL", _ => throw SqlError())
            .Answer("NOT EXISTS (SELECT 1 FROM [dbo].[Status]");

        var results = new UnenforcedForeignKeyCheck(afterCleanup: false).Run(TestSchema.Context(plan, database)).ToList();

        var failed = Assert.Single(results, r => r.Status == CheckStatus.FailedToRun);
        Assert.StartsWith("FK_visitor_Status (untrusted)", failed.Description, StringComparison.Ordinal);
        Assert.StartsWith("Failed to run:", failed.Message, StringComparison.Ordinal);
        var summaryResult = Assert.Single(results, r => r.Status == CheckStatus.NoProblemsFound);
        Assert.Contains("No missing parents (1): FK_visitor_StatusEnd", summaryResult.Details);
    }

    // SqlException has no public constructor; its message and number are all the checks read.
    private static SqlException SqlError() =>
        (SqlException)RuntimeHelpers.GetUninitializedObject(typeof(SqlException));

    /// <summary>
    /// The test schema with one untrusted foreign key on text, visitor.mothraid to tblPerson.person_MothraID.
    /// </summary>
    private static AnalysisPlan TextKeyPlan()
    {
        var summary = TestSchema.Summary();
        return TestSchema.Plan("{}", new SchemaSummary
        {
            Database = summary.Database,
            CompatibilityLevel = summary.CompatibilityLevel,
            Tables = summary.Tables,
            Views = summary.Views,
            PersonIdColumns = summary.PersonIdColumns,
            ForeignKeys = [new("FK_visitor_Person", TestSchema.Visitor, ["mothraid"], TestSchema.Person, ["person_MothraID"], true, false, false, "NO_ACTION", "NO_ACTION")]
        });
    }

    [Theory]
    [InlineData(1, "Missing parents only until cleanup (1): FK_visitor_Person.")]
    [InlineData(0, "No missing parents (1): FK_visitor_Person")]
    public void UnenforcedForeignKeys_TextKey_LeavesCleanupOnlyRowsToTheOtherCheckButNotOnTheReTrustList(int cleanupOnly, string expected)
    {
        var database = new FakeDatabase()
            .Answer("SELECT CASE WHEN EXISTS", [cleanupOnly])
            .Answer("NOT EXISTS (SELECT 1 FROM [dbo].[tblPerson]");

        var result = Assert.Single(new UnenforcedForeignKeyCheck(afterCleanup: false).Run(TestSchema.Context(TextKeyPlan(), database)));

        Assert.Equal(CheckStatus.NoProblemsFound, result.Status);
        Assert.Contains(result.Details, d => d.StartsWith(expected, StringComparison.Ordinal));
        Assert.Contains("\n  AND NOT EXISTS (SELECT 1 FROM [dbo].[tblPerson] AS p WHERE LTRIM(RTRIM(", database.Queries[0].Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void UnenforcedForeignKeysAfterCleanup_TextKey_CountsOnlyRowsThatMatchAfterCleanup()
    {
        var database = new FakeDatabase()
            .Answer("SELECT COUNT_BIG(*) FROM (SELECT DISTINCT", [1L])
            .Answer("NOT EXISTS (SELECT 1 FROM [dbo].[tblPerson]", [7, 2L, null, null], [9, 2L, null, null]);

        var result = Assert.Single(new UnenforcedForeignKeyCheck(afterCleanup: true).Run(TestSchema.Context(TextKeyPlan(), database)));

        Assert.Equal([("Rows that match only after cleanup", 2L), ("Distinct values", 1L)], result.Counts.Select(c => (c.Label, c.Value)).ToList());
        Assert.Contains("\n  AND EXISTS (SELECT 1 FROM [dbo].[tblPerson] AS p WHERE LTRIM(RTRIM(", database.Queries[0].Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void UnenforcedForeignKeysAfterCleanup_NumberKey_IsNotCheckedAndSaysWhy()
    {
        var plan = TestSchema.Plan("{}", TestSchema.WithUntrustedForeignKey());
        var database = new FakeDatabase();

        var result = Assert.Single(new UnenforcedForeignKeyCheck(afterCleanup: true).Run(TestSchema.Context(plan, database)));

        Assert.Empty(database.Queries);
        Assert.Contains("none of the 1 untrusted or disabled foreign keys is on text columns", Assert.Single(result.Details), StringComparison.Ordinal);
    }

    /// <summary>
    /// visitor with a declared key of (mothraid, startWeekID): one exact-copy group and one group sharing
    /// the key that differs in notes, both holding a LoginId where the MothraId belongs.
    /// </summary>
    private static (List<CheckResult> Results, FakeDatabase Database) DuplicatesOnVisitor()
    {
        var plan = TestSchema.Plan("""
            { "checks": [{ "check": "noDuplicates", "table": "[dbo].[visitor]", "columns": ["mothraid", "startWeekID"] }] }
            """);
        // The declared part's grouped read: the key's compared values, then DATALENGTH, rows, dates, and one flag
        // per compared column (mothraid, gradYear, startWeekID, endWeekID, addUser, addDate, notes) saying it differs.
        var database = new FakeDatabase()
            .Answer("COUNT(DISTINCT", ["jsmith", 10, 6, 2L, null, null, 0, 0, 0, 0, 0, 0, 1])
            .Answer("(SELECT CASE WHEN", [0, 3], [0, 8])
            .Answer("SELECT x.g, x.k", [4, 4, "jsmith", 11], [4, 9, "jsmith", 11])
            .AnswerWith("SELECT COUNT_BIG(*), SUM(x.n)", _ => [[1L, 2L, null, null, 0L, 0L]]);

        var results = new DuplicateRowsCheck(ignoringSurrogateKey: true).Run(TestSchema.Context(plan, database)).ToList();
        return (results, database);
    }

    [Fact]
    public void DuplicateRows_CopiesDifferingOnlyInCaseOrTrailingSpaces_AreCountedApartAndMarked()
    {
        var plan = TestSchema.Plan("{}");
        // visitor's compared text columns are mothraid, addUser, and notes; the second group differs in notes only by case.
        var database = new FakeDatabase()
            .AnswerWhen(sql => sql.StartsWith("SELECT COUNT_BIG(*), SUM(x.n)", StringComparison.Ordinal) && sql.Contains(TestSchema.Visitor, StringComparison.Ordinal),
                _ => [[2L, 5L, null, null, 1L, 3L]])
            .AnswerWith("SELECT COUNT_BIG(*), SUM(x.n)", _ => [[0L, null, null, null, null, null]])
            .Answer("SELECT TOP (10) MIN(t.[id])", [4, 0, 0, 0], [12, 0, 0, 1])
            .Answer("SELECT x.g, x.k", [4, 4], [4, 9], [12, 12], [12, 13], [12, 14]);

        var result = new DuplicateRowsCheck(ignoringSurrogateKey: true).Run(TestSchema.Context(plan, database)).Single(r => r.Table == TestSchema.Visitor);

        Assert.Equal(
            [("Exact copies: groups", 1L), ("Exact copies: rows", 2L),
             ("Copies differing only in case or trailing spaces: groups", 1L), ("Copies differing only in case or trailing spaces: rows", 3L)],
            result.Counts.Select(c => (c.Label, c.Value)).ToList());
        Assert.Contains("Copies, by id (2; exact unless marked):", result.Details);
        Assert.Contains("  4, 9", result.Details);
        Assert.Contains("  12, 13, 14; differ only in case or trailing spaces, in notes", result.Details);
        var totals = database.Queries.First(q => q.Sql.Contains(TestSchema.Visitor, StringComparison.Ordinal)).Sql;
        Assert.Contains("MIN(t.[notes] COLLATE Latin1_General_BIN2) <> MAX(t.[notes] COLLATE Latin1_General_BIN2) OR MIN(DATALENGTH(t.[notes]))", totals, StringComparison.Ordinal);
    }

    [Fact]
    public void NoDuplicates_ColumnDifferingOnlyInCase_IsNamedAndItsGroupIsACopy()
    {
        var plan = TestSchema.Plan("""
            { "checks": [{ "check": "noDuplicates", "table": "[dbo].[visitor]", "columns": ["mothraid", "startWeekID"] }] }
            """);
        // Flags per compared column (mothraid, gradYear, startWeekID, endWeekID, addUser, addDate, notes): 1 differs, 2 by case or spaces only.
        var database = new FakeDatabase()
            .Answer("COUNT(DISTINCT", ["00123456", 8, 6, 2L, null, null, 0, 0, 0, 0, 1, 0, 2], ["00999999", 8, 7, 2L, null, null, 0, 0, 0, 0, 0, 0, 2])
            .Answer("(SELECT CASE WHEN", [0, 3], [0, 8])
            .Answer("SELECT TOP (10) MIN(t.[id])", [4, 0, 0, 1])
            .Answer("SELECT x.g, x.k", [4, 4, "00999999", 7], [4, 9, "00999999", 7])
            .AnswerWhen(sql => sql.StartsWith("SELECT COUNT_BIG(*), SUM(x.n)", StringComparison.Ordinal) && sql.Contains(TestSchema.Visitor, StringComparison.Ordinal),
                _ => [[1L, 2L, null, null, 1L, 2L]])
            .AnswerWith("SELECT COUNT_BIG(*), SUM(x.n)", _ => [[0L, null, null, null, null, null]]);

        var result = new DuplicateRowsCheck(ignoringSurrogateKey: true).Run(TestSchema.Context(plan, database)).Single(r => r.Table == TestSchema.Visitor);

        Assert.Contains("Same (mothraid, startWeekID), different elsewhere (1 of the 2 groups sharing it; the other 1 is a copy, above):", result.Details);
        Assert.Contains("    addUser and notes (case or trailing spaces only): 1", result.Details);
    }

    [Fact]
    public void DuplicateRows_ExactCopiesAndDeclaredKey_AreOneResultForTheTable()
    {
        var (results, _) = DuplicatesOnVisitor();

        var result = results.Single(r => r.Table == TestSchema.Visitor);
        Assert.Equal(CheckStatus.ProblemsFound, result.Status);
        Assert.Equal(
            [("Exact copies: groups", 1L), ("Exact copies: rows", 2L), ("Same (mothraid, startWeekID), different elsewhere: groups", 1L),
             ("Same (mothraid, startWeekID), different elsewhere: rows", 2L)],
            result.Counts.Select(c => (c.Label, c.Value)).ToList());
        Assert.Contains("    notes only: 1", result.Details);
    }

    [Fact]
    public void DuplicateRows_LoginIdInADeclaredPersonColumn_IsShownOnlyAsAShape()
    {
        var (results, database) = DuplicatesOnVisitor();

        var details = results.Single(r => r.Table == TestSchema.Visitor).Details;
        // startWeekID has no safe origin, so its numbers are shapes too.
        Assert.Contains("  4, 9 (mothraid <letters only, 6 chars>, startWeekID <digits only, 2 chars>)", details);
        Assert.Contains("    mothraid <letters only, 6 chars>, startWeekID <digits only, 2 chars>: 2 rows; id 3, 8; differ in notes", details);
        Assert.DoesNotContain(details, d => d.Contains("jsmith", StringComparison.Ordinal));
        // The group's values reach the row lookup as parameters, never as SQL text.
        Assert.DoesNotContain("jsmith", database.AllSql, StringComparison.Ordinal);
        Assert.Contains(database.Queries, q => q.Parameters.Any(p => Equals(p.Value, "jsmith")));
    }

    [Fact]
    public void RowsDifferingOnlyInAuditColumns_IgnoresTheAuditColumnsAndListsGroupsByKey()
    {
        var plan = TestSchema.Plan("{}");
        var database = new FakeDatabase()
            .AnswerWhen(sql => sql.StartsWith("SELECT COUNT_BIG(*), SUM(x.n)", StringComparison.Ordinal) && sql.Contains(TestSchema.Visitor, StringComparison.Ordinal),
                _ => [[1L, 2L, null, null, 0L]])
            .AnswerWith("SELECT COUNT_BIG(*), SUM(x.n)", _ => [[0L, null, null, null, null]])
            .Answer("SELECT x.g, x.k", [5, 5], [5, 7]);

        var results = new AuditOnlyDifferencesCheck().Run(TestSchema.Context(plan, database)).ToList();

        var result = Assert.Single(results);
        Assert.Equal((TestSchema.Visitor, Severity.Warning), (result.Table, result.Severity));
        Assert.Contains("Audit columns ignored: addUser, addDate", result.Details);
        Assert.Contains("  5, 7", result.Details);
        Assert.DoesNotContain(database.Queries, q => q.Sql.StartsWith("SELECT TOP (10) MIN(", StringComparison.Ordinal));
        var grouped = database.Queries.First(q => q.Sql.Contains(TestSchema.Visitor, StringComparison.Ordinal)).Sql;
        Assert.DoesNotContain("GROUP BY t.[mothraid], t.[gradYear], t.[startWeekID], t.[endWeekID], t.[addUser]", grouped, StringComparison.Ordinal);
        Assert.Contains("MIN(t.[addUser]) <> MAX(t.[addUser])", grouped, StringComparison.Ordinal);
    }

    [Fact]
    public void RowsDifferingOnlyInAuditColumns_LogTable_IsInfoWithANote()
    {
        var plan = TestSchema.Plan("{}");
        var database = new FakeDatabase()
            .AnswerWhen(sql => sql.StartsWith("SELECT COUNT_BIG(*), SUM(x.n)", StringComparison.Ordinal) && sql.Contains(TestSchema.ChangeLog, StringComparison.Ordinal),
                _ => [[1L, 2L, null, null, 0L]])
            .AnswerWith("SELECT COUNT_BIG(*), SUM(x.n)", _ => [[0L, null, null, null, null]])
            .Answer("SELECT x.g, x.k", [1, 1], [1, 2]);

        var result = Assert.Single(new AuditOnlyDifferencesCheck().Run(TestSchema.Context(plan, database)));

        Assert.Equal((TestSchema.ChangeLog, Severity.Info), (result.Table, result.Severity));
        Assert.Equal("Info, not warning: named like a log, so these are likely separate events.", result.Details[0]);
    }

    [Fact]
    public void RowsDifferingOnlyInAuditColumns_OtherColumnsDifferingInCase_AreCountedAndMarked()
    {
        var plan = TestSchema.Plan("{}");
        // visitor's core text columns, once addUser and addDate are set aside, are mothraid and notes.
        var database = new FakeDatabase()
            .AnswerWhen(sql => sql.StartsWith("SELECT COUNT_BIG(*), SUM(x.n)", StringComparison.Ordinal) && sql.Contains(TestSchema.Visitor, StringComparison.Ordinal),
                _ => [[1L, 2L, null, null, 1L]])
            .AnswerWith("SELECT COUNT_BIG(*), SUM(x.n)", _ => [[0L, null, null, null, null]])
            // The listing's own query holds the loose query's opening in its IN list, so it is answered first.
            .Answer("SELECT x.g, x.k", [5, 5], [5, 7])
            .Answer("SELECT TOP (10) MIN(t.[id])", [5, 0, 1]);

        var result = Assert.Single(new AuditOnlyDifferencesCheck().Run(TestSchema.Context(plan, database)));

        Assert.Contains(("Groups whose other columns also differ in case or trailing spaces", 1L), result.Counts.Select(c => (c.Label, c.Value)));
        Assert.Contains("  5, 7; differ only in case or trailing spaces, in notes", result.Details);
    }
}
