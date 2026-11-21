using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// Objects that change data names its blind spots, among them the stored objects SQL Server could not resolve,
/// in words a reader without SQL Server's error catalog can act on.
/// </summary>
[InvariantCulture]
public class ObjectsThatChangeDataCheckTests
{
    private const string BindingError =
        "The dependencies reported for entity \"[dbo].[get_ElectiveBlocks]\" might not include references to all columns. " +
        "This is either because the entity references an object that does not exist or because of an error in one or more " +
        "statements in the entity. (error 2020)";

    [Fact]
    public void Run_ObjectsSqlServerCouldNotResolve_AreListedWithWhyInPlainWords()
    {
        var summary = TestSchema.Summary();
        summary.ModuleDependencies.Add(new ModuleDependency("[dbo].[get_ElectiveBlocks]", "P", [], false, BindingError));
        summary.ModuleDependencies.Add(new ModuleDependency("[dbo].[usp_other]", "P", [], false, "Invalid object name 'x'. (error 208)"));
        var plan = TestSchema.Plan("{}", summary);

        var result = Assert.Single(new ObjectsThatChangeDataCheck().Run(TestSchema.Context(plan, new FakeDatabase())));

        var grid = Assert.Single(result.Grids, g => g.Title.StartsWith("SQL Server could not resolve", StringComparison.Ordinal));
        Assert.Equal("SQL Server could not resolve their references, so what they read and write is unknown", grid.Title);
        Assert.Equal(
            [["[dbo].[get_ElectiveBlocks]", "It references something that does not exist now, a temporary table, or another database or server (error 2020)"],
             ["[dbo].[usp_other]", "Invalid object name 'x'. (error 208)"]],
            grid.Rows);
    }

    [Fact]
    public void Run_DatabaseTriggerWritingToATable_IsListedAsAWriter()
    {
        var summary = TestSchema.Summary();
        summary.ModuleDependencies.Add(new ModuleDependency("[trg_LogSchemaChanges]", "DATABASE_DDL_TRIGGER",
            [new ModuleReference("[dbo].[Status]", false, true, [])], false, null));
        var plan = TestSchema.Plan("{}", summary);

        var result = Assert.Single(new ObjectsThatChangeDataCheck().Run(TestSchema.Context(plan, new FakeDatabase())));

        var grid = Assert.Single(result.Grids, g => g.Title == "Tables written by stored objects");
        Assert.Equal([["[dbo].[Status]", "[trg_LogSchemaChanges]", "1"]], grid.Rows);
    }

    [Fact]
    public void Run_ObjectWithNoDefinitionToRead_IsNamedAsABlindSpot()
    {
        const string Reason = "definition not visible; the login may lack VIEW DEFINITION";
        var summary = TestSchema.Summary();
        summary.ModuleDependencies.Add(new ModuleDependency("[dbo].[usp_hidden]", "P", [new ModuleReference("[dbo].[Status]", true, false, [])], false, null));
        summary.MissingDefinitions.Add(new MissingDefinition("[dbo].[usp_hidden]", "SQL_STORED_PROCEDURE", Reason));
        var plan = TestSchema.Plan("{}", summary);

        var result = Assert.Single(new ObjectsThatChangeDataCheck().Run(TestSchema.Context(plan, new FakeDatabase())));

        var grid = Assert.Single(result.Grids, g => g.Title.StartsWith("No definition to read", StringComparison.Ordinal));
        Assert.Equal([["[dbo].[usp_hidden]", Reason]], grid.Rows);
    }

    [Theory]
    [InlineData(true, "This database stores no views, procedures, functions, or triggers.")]
    [InlineData(false, "No views, procedures, functions, or triggers are visible to this login, which lacks VIEW DEFINITION; " +
                       "the database may still store some.")]
    public void Run_NoObjects_SaysNoneOnlyWhenTheLoginCouldSeeThem(bool hasViewDefinition, string expected)
    {
        var plan = TestSchema.Plan("{}", WithViewDefinition(TestSchema.Summary(), hasViewDefinition));

        var result = Assert.Single(new ObjectsThatChangeDataCheck().Run(TestSchema.Context(plan, new FakeDatabase())));

        Assert.Equal(expected, result.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Run_WithoutViewDefinition_SaysTheListIsIncomplete(bool hasViewDefinition)
    {
        var summary = WithViewDefinition(TestSchema.Summary(), hasViewDefinition);
        summary.ModuleDependencies.Add(new ModuleDependency("[dbo].[usp_write]", "P", [new ModuleReference("[dbo].[Status]", false, true, [])], false, null));
        var plan = TestSchema.Plan("{}", summary);

        var result = Assert.Single(new ObjectsThatChangeDataCheck().Run(TestSchema.Context(plan, new FakeDatabase())));

        Assert.Equal(!hasViewDefinition, result.Details.Exists(d => d.StartsWith("Incomplete: the login lacks VIEW DEFINITION", StringComparison.Ordinal)));
    }

    private static SchemaSummary WithViewDefinition(SchemaSummary summary, bool hasViewDefinition) => new()
    {
        Database = summary.Database,
        CompatibilityLevel = summary.CompatibilityLevel,
        HasViewDefinition = hasViewDefinition,
        Tables = summary.Tables,
        Views = summary.Views,
        ForeignKeys = summary.ForeignKeys,
        PersonIdColumns = summary.PersonIdColumns
    };
}
