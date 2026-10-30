using Microsoft.Data.SqlClient;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// identifiesOnePerson reads a column's IDs from the legacy database and looks each up in VIPER over its own
/// connection. Each ID lands in exactly one category, and only IDs shaped like the declared type are ever
/// copied into the report: a LoginId or MailId never is, in the report or in the SQL sent to either database.
/// </summary>
[InvariantCulture]
public class IdentifiesOnePersonCheckTests
{
    /// <summary>
    /// VIPER's users.Person, answering lookups by the parameters sent: which people hold each value in one column.
    /// </summary>
    private static FakeDatabase Viper(Dictionary<string, string[]> mothraIds, Dictionary<string, string[]>? loginIds = null,
        Dictionary<string, string[]>? mailIds = null) =>
        new FakeDatabase()
            .AnswerWith("l.[MothraId] IN", p => Matches(p, mothraIds))
            .AnswerWith("l.[LoginId] IN", p => Matches(p, loginIds ?? []))
            .AnswerWith("l.[IamId] IN", _ => [])
            .AnswerWith("CHARINDEX('@', l.[MailId])", p => Matches(p, mailIds ?? []));

    // The legacy query listing rows by the IDs they hold, compared exactly.
    private const string RowsByIds = "COLLATE Latin1_General_BIN2 = @p0";

    private static List<object?[]> Matches(SqlParameter[] parameters, Dictionary<string, string[]> people) =>
        parameters.Select(p => (string)p.Value!)
            .Where(people.ContainsKey)
            .SelectMany(v => people[v].Select(person => new object?[] { v, person }))
            .ToList();

    private static (CheckResult Result, FakeDatabase Legacy, FakeDatabase Viper) Run(string table, string column, string idType,
        FakeDatabase legacy, FakeDatabase viper)
    {
        var plan = TestSchema.Plan($$"""
            { "checks": [{ "check": "identifiesOnePerson", "table": "{{table}}", "column": "{{column}}", "idType": "{{idType}}", "lookIn": "VIPER.users.Person" }] }
            """);
        var context = TestSchema.Context(plan, legacy, new() { ["VIPER"] = viper });
        return (Assert.Single(Assert.Single(plan.DeclaredChecks).Run(context)), legacy, viper);
    }

    private static List<(string, long)> Counts(CheckResult result) => result.Counts.Select(c => (c.Label, c.Value)).ToList();

    [Fact]
    public void Run_EachIdLandsInOneCategory()
    {
        var legacy = new FakeDatabase()
            .Answer(RowsByIds, [7, 3L, null, null])
            .Answer("COUNT_BIG(*)\nFROM [dbo].[visitor]",
                ["00123456", 10L], ["00999999", 2L], ["123456", 1L], ["jsmith", 3L], ["Unknown", 1L], [null, 4L]);
        var viper = Viper(new() { ["00123456"] = ["1001"] }, new() { ["jsmith"] = ["1002"] });

        var (result, _, _) = Run(TestSchema.Visitor, "mothraid", "MothraId", legacy, viper);

        Assert.Equal(CheckStatus.ProblemsFound, result.Status);
        Assert.Equal(
            [("NULL (not checked): rows", 4L), ("Found one person: IDs", 1L), ("Found one person: rows", 10L), ("Not found: IDs", 1L),
             ("Not found: rows", 2L), ("Found only after cleanup: IDs", 1L), ("Found only after cleanup: rows", 1L), ("Placeholder: IDs", 1L),
             ("Placeholder: rows", 1L), ("Found as a different ID type: IDs", 1L), ("Found as a different ID type: rows", 3L)],
            Counts(result));
        Assert.Contains("Not found: \"00999999\"", result.Details);
        Assert.Contains("Found only after cleanup: \"123456\"", result.Details);
        Assert.Contains("Placeholder: \"Unknown\" (1 row)", result.Details);
        Assert.Equal([["LoginId only", "1", "3"]], Assert.Single(result.Grids).Rows);
        Assert.Equal(["7"], result.Examples);
    }

    [Fact]
    public void Run_ValueFoundAsALoginId_IsNeverShownOrWrittenIntoSql()
    {
        var legacy = new FakeDatabase()
            .Answer(RowsByIds, [7, 3L, null, null])
            .Answer("COUNT_BIG(*)\nFROM [dbo].[visitor]", ["jsmith", 3L]);
        var viper = Viper([], new() { ["jsmith"] = ["1002"] });

        var (result, legacyDb, viperDb) = Run(TestSchema.Visitor, "mothraid", "MothraId", legacy, viper);

        var report = string.Join("\n", result.Details.Concat(result.Examples).Concat(result.Grids.SelectMany(g => g.Rows.SelectMany(r => r))));
        Assert.DoesNotContain("jsmith", report, StringComparison.Ordinal);
        Assert.DoesNotContain("jsmith", legacyDb.AllSql + viperDb.AllSql, StringComparison.Ordinal);
        Assert.Contains(viperDb.Queries, q => q.Sql.Contains("l.[LoginId] IN", StringComparison.Ordinal)
            && q.Parameters.Any(p => Equals(p.Value, "jsmith")));
    }

    [Fact]
    public void Run_DeclaredAsLoginId_NotFoundValuesAreDescribedByShapeOnly()
    {
        var legacy = new FakeDatabase()
            .Answer(RowsByIds, [41, 2L, null, null], [42, 2L, null, null])
            .Answer("COUNT_BIG(*)\nFROM [audit].[ChangeLog]", ["jsmith", 5L], ["xdoe", 2L]);
        var viper = Viper([], new() { ["jsmith"] = ["1002"] });

        var (result, _, _) = Run(TestSchema.ChangeLog, "ModBy", "LoginId", legacy, viper);

        Assert.Contains("Not found: 1 not shown, LoginIds are never shown: letters only, 4 chars", result.Details);
        Assert.Contains("  Rows by log_ID (2): 41, 42", result.Details);
        Assert.DoesNotContain(result.Details, d => d.Contains("xdoe", StringComparison.Ordinal) || d.Contains("jsmith", StringComparison.Ordinal));
    }

    [Fact]
    public void Run_IdHeldBySeveralPeople_IsFoundSeveralAndNeverPicksOne()
    {
        var legacy = new FakeDatabase().Answer("COUNT_BIG(*)\nFROM [dbo].[visitor]", ["00123456", 4L]);
        var viper = Viper(new() { ["00123456"] = ["1001", "1003"] });

        var (result, _, _) = Run(TestSchema.Visitor, "mothraid", "MothraId", legacy, viper);

        Assert.Equal([("Found one person: IDs", 0L), ("Found one person: rows", 0L), ("Found several people: IDs", 1L), ("Found several people: rows", 4L)],
            Counts(result));
        Assert.Contains("Found several people: \"00123456\"", result.Details);
    }

    [Fact]
    public void Run_RowsOfAnId_AreFoundByValueAndLength_SoATrailingSpaceVariantIsLeftOut()
    {
        var legacy = new FakeDatabase()
            .Answer(RowsByIds, [7, 3L, null, null])
            // Not shaped like MothraIds, so their rows are listed by key rather than the IDs shown.
            .Answer("COUNT_BIG(*)\nFROM [dbo].[visitor]", ["12a", 2L], ["12a ", 1L]);

        var (_, legacyDb, _) = Run(TestSchema.Visitor, "mothraid", "MothraId", legacy, Viper([]));

        var lookup = legacyDb.Queries.Single(q => q.Sql.Contains(RowsByIds, StringComparison.Ordinal));
        Assert.Contains("(t.[mothraid] COLLATE Latin1_General_BIN2 = @p0 AND DATALENGTH(t.[mothraid]) = DATALENGTH(@p0))", lookup.Sql, StringComparison.Ordinal);
        Assert.All(lookup.Parameters, p => Assert.Equal(System.Data.SqlDbType.VarChar, p.SqlDbType));
    }

    [Fact]
    public void Run_CleanedIdHeldBySeveralPeople_IsItsOwnCategory()
    {
        var legacy = new FakeDatabase()
            .Answer(RowsByIds, [7, 3L, null, null])
            .Answer("COUNT_BIG(*)\nFROM [dbo].[visitor]", ["123456", 3L]);
        var viper = Viper(new() { ["00123456"] = ["1001", "1003"] });

        var (result, _, _) = Run(TestSchema.Visitor, "mothraid", "MothraId", legacy, viper);

        Assert.Contains(("Found only after cleanup, several people: IDs", 1L), Counts(result));
        Assert.DoesNotContain(Counts(result), c => c.Item1 == "Found only after cleanup: IDs");
        Assert.Contains("Found only after cleanup, several people: \"123456\"", result.Details);
    }

    [Fact]
    public void Run_CleanedIdFoundUnderTwoSpellingsOfOnePerson_IsOnePerson()
    {
        var legacy = new FakeDatabase()
            .Answer(RowsByIds, [41, 2L, null, null])
            .Answer("COUNT_BIG(*)\nFROM [audit].[ChangeLog]", ["JDOE ", 2L]);
        // The lookup table holds the same person's LoginId twice, in two cases.
        var viper = new FakeDatabase().AnswerWith("l.[LoginId] IN", _ => [["jdoe", "1002"], ["JDOE", "1002"]]);

        var (result, _, _) = Run(TestSchema.ChangeLog, "ModBy", "LoginId", legacy, viper);

        Assert.Contains(("Found only after cleanup: IDs", 1L), Counts(result));
    }

    [Fact]
    public void Run_MailIdsStoredAsFullAddresses_MatchWithTheirDomain()
    {
        var legacy = new FakeDatabase()
            .Answer(RowsByIds, [41, 1L, null, null])
            .Answer("COUNT_BIG(*)\nFROM [audit].[ChangeLog]", ["jdoe@ucdavis.edu", 3L], ["asmith", 2L], ["jdoe@gmail.com", 1L]);
        var viper = Viper([], mailIds: new() { ["jdoe@ucdavis.edu"] = ["1002"], ["asmith@ucdavis.edu"] = ["1003"] });

        var (result, _, viperDb) = Run(TestSchema.ChangeLog, "ModBy", "MailId", legacy, viper);

        Assert.Contains(("Found one person: IDs", 2L), Counts(result));
        Assert.Contains(("Not found: IDs", 1L), Counts(result));
        Assert.DoesNotContain(Counts(result), c => c.Item1.StartsWith("Found only after cleanup", StringComparison.Ordinal));
        Assert.Contains(result.Details, d => d.StartsWith("2 IDs are stored as a full address, matched with its domain", StringComparison.Ordinal));
        Assert.Contains(viperDb.Queries, q => q.Sql.Contains("ELSE l.[MailId] + '@ucdavis.edu' END", StringComparison.Ordinal));
    }

    [Fact]
    public void Run_PaddedColumn_IsReadWithoutItsPaddingAndSaysSo()
    {
        var legacy = new FakeDatabase().Answer("COUNT_BIG(*)\nFROM [dbo].[tblPerson]", ["00123456", 4L]);
        var viper = Viper(new() { ["00123456"] = ["1001"] });

        var (result, legacyDb, _) = Run(TestSchema.Person, "person_MothraID", "MothraId", legacy, viper);

        Assert.Equal(CheckStatus.NoProblemsFound, result.Status);
        Assert.StartsWith("SELECT RTRIM(t.[person_MothraID]) COLLATE Latin1_General_BIN2", legacyDb.Queries[0].Sql, StringComparison.Ordinal);
        Assert.Contains(result.Details, d => d.StartsWith("char(8) pads values", StringComparison.Ordinal));
    }

    [Fact]
    public void Run_AllNull_SaysThereWereNoIdsToCheck()
    {
        var legacy = new FakeDatabase().Answer("COUNT_BIG(*)\nFROM [dbo].[visitor]", [null, 6L]);

        var (result, _, viperDb) = Run(TestSchema.Visitor, "mothraid", "MothraId", legacy, Viper([]));

        Assert.Equal(CheckStatus.NoProblemsFound, result.Status);
        Assert.Contains("No IDs to check: every row is NULL.", result.Details);
        Assert.Empty(viperDb.Queries);
    }
}
