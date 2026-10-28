using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// Every automatic check together, as a run executes them: on a clean database they find nothing and
/// fail nowhere, they never read a table left out by exclude or include, and the checks that work from
/// metadata never query at all.
/// </summary>
[InvariantCulture]
public class AutomaticRunTests
{
    /// <summary>
    /// Profiles with nothing found, and no groups for the duplicate and audit checks' grouped reads.
    /// </summary>
    private static FakeDatabase Clean(AnalysisPlan plan) =>
        ProfileRow.Answering(plan).AnswerWhen(sql => sql.Contains("HAVING COUNT_BIG(*) > 1", StringComparison.Ordinal),
            _ => [[0L, null, null, null]]);

    private static List<CheckResult> RunAll(AnalysisPlan plan, FakeDatabase database)
    {
        var context = TestSchema.Context(plan, database);
        return CheckRegistry.Automatic.SelectMany(c => c.Run(context)).ToList();
    }

    [Fact]
    public void AllAutomaticChecks_CleanDatabase_FindNoProblemsAndNeverFail()
    {
        var results = RunAll(TestSchema.Plan("{}"), Clean(TestSchema.Plan("{}")));

        Assert.DoesNotContain(results, r => r.Status is CheckStatus.FailedToRun or CheckStatus.Skipped);
        Assert.All(results.Where(r => r.Kind == ResultKind.Problem), r => Assert.Equal(CheckStatus.NoProblemsFound, r.Status));
        // Only what the schema itself shows: the person columns, and MothraIds typed varchar(8) in one table and char(8) in another.
        Assert.Equal(["Person columns", "Related columns with different types"],
            results.Where(r => r.Status == CheckStatus.ProblemsFound).Select(r => r.Name).Distinct().Order().ToList());
    }

    [Fact]
    public void AllAutomaticChecks_ExcludedTable_IsNeverRead()
    {
        var plan = TestSchema.Plan("""{ "exclude": ["[dbo].[tblPerson]"] }""");
        var database = Clean(plan);

        RunAll(plan, database);

        Assert.NotEmpty(database.Queries);
        Assert.DoesNotContain(TestSchema.Person, database.AllSql, StringComparison.Ordinal);
    }

    [Fact]
    public void AllAutomaticChecks_OutsideInclude_IsNeverRead()
    {
        var plan = TestSchema.Plan("""{ "include": ["[audit]"] }""");
        var database = Clean(plan);

        RunAll(plan, database);

        Assert.NotEmpty(database.Queries);
        Assert.DoesNotContain("[dbo].", database.AllSql, StringComparison.Ordinal);
    }

    [Fact]
    public void AllAutomaticChecks_SkipOverride_SkipsThatCheckOnThatTableOnly()
    {
        var plan = TestSchema.Plan("""
            { "automaticChecks": [{ "automaticCheck": "Placeholder text", "table": "[dbo].[visitor]", "skip": true, "reason": "Known." }] }
            """);

        var results = RunAll(plan, Clean(plan));

        var skipped = Assert.Single(results, r => r.Status == CheckStatus.Skipped);
        Assert.Equal(("Placeholder text", TestSchema.Visitor, "Skipped: Known."), (skipped.Name, skipped.Table, skipped.Message));
    }

    [Fact]
    public void MetadataChecks_NeverQueryTheDatabase()
    {
        var plan = TestSchema.Plan("{}");
        var database = new FakeDatabase();
        var context = TestSchema.Context(plan, database);
        IAutomaticCheck[] metadata =
        [
            new RepeatingColumnGroupsCheck(), new DeprecatedTypesCheck(), new MismatchedTypesCheck(), new IdentityHeadroomCheck(),
            new ObjectsThatChangeDataCheck(), new CheckConstraintsCheck(), new TablesThatLookLikeCopiesCheck(),
            new UnenforcedForeignKeyCheck(afterCleanup: false), new UnenforcedForeignKeyCheck(afterCleanup: true)
        ];

        var results = metadata.SelectMany(c => c.Run(context)).ToList();

        Assert.Empty(database.Queries);
        Assert.DoesNotContain(results, r => r.Status == CheckStatus.FailedToRun);
    }
}
