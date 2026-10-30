using System.Text.Json;
using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// The suggestions are guesses from the schema export alone, for a developer to keep or delete: scope for a
/// new database, backups to exclude, date columns, relationships named like a primary key, and person ID
/// columns. What the declarations already hold is left out, and what is written must bind as written.
/// </summary>
[InvariantCulture]
public class SuggestionsTests
{
    private static ColumnSummary Column(string name, string type) => new(name, type, null, null, true, false, null, null);

    private static TableSummary Table(string name, string key, params ColumnSummary[] columns) =>
        new(name, 100, true, true, 0, [key], [], [.. columns]);

    /// <summary>
    /// Courses keyed by course_ID; effort rows and audit log rows that name a course_ID, an effort MothraId,
    /// and two date columns; a dated backup of courses; and a view.
    /// </summary>
    private static SchemaSummary Summary()
    {
        List<TableSummary> tables =
        [
            Table("[dbo].[tblCourses]", "course_ID", Column("course_ID", "int"), Column("course_Name", "varchar(50)")),
            Table("[dbo].[tblEffort]", "effort_ID", Column("effort_ID", "int"), Column("course_ID", "int"), Column("effort_MothraID", "char(8)"),
                Column("effort_Created", "datetime"), Column("effort_Modified", "datetime")),
            Table("[dbo].[tblCourses_backup_2017_08_25]", "course_ID", Column("course_ID", "int")),
            Table("[audit].[tblLog]", "log_ID", Column("log_ID", "int"), Column("course_ID", "int"))
        ];
        List<ViewSummary> views = [new("[dbo].[vCourses]", [Column("course_ID", "int")])];
        var withoutPeople = new SchemaSummary { Tables = tables, Views = views };
        return new SchemaSummary
        {
            Database = "Effort",
            CompatibilityLevel = 130,
            Tables = tables,
            Views = views,
            PersonIdColumns = PersonIdRules.Find(withoutPeople)
        };
    }

    private static Declarations Declared(string json) => JsonSerializer.Deserialize<Declarations>(json, Declarations.ReadOptions)!;

    private static string Build(string declarations = "{}") =>
        Suggestions.Build(Summary(), "Effort", "Production", "Areas/Effort/analysis.json", Declared(declarations));

    [Fact]
    public void Build_NewDatabase_ListsEverySchemaAndObjectAsACommentedInclude()
    {
        var suggestions = Build();

        Assert.Contains("  // \"include\": [", suggestions, StringComparison.Ordinal);
        Assert.Contains("  //   \"[audit]\", // 1 table, 0 views", suggestions, StringComparison.Ordinal);
        Assert.Contains("  //   \"[dbo]\", // 3 tables, 1 view", suggestions, StringComparison.Ordinal);
        Assert.Contains("  //   \"[dbo].[tblEffort]\", // 100 rows", suggestions, StringComparison.Ordinal);
        Assert.Contains("  //   \"[dbo].[vCourses]\", // view", suggestions, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_GuessesExcludesDateColumnsRelationshipsAndPersonIds()
    {
        var suggestions = Build();

        Assert.Contains("    \"[dbo].[tblCourses_backup_2017_08_25]\" // name word \"backup\" and a date", suggestions, StringComparison.Ordinal);
        Assert.Contains("\"[dbo].[tblEffort]\": \"effort_Created\"", suggestions, StringComparison.Ordinal);
        Assert.Contains("named for creation; others: effort_Modified", suggestions, StringComparison.Ordinal);
        Assert.Contains("\"description\": \"Guessed: course_ID is named like the primary key of [dbo].[tblCourses]. Confirm the relationship before keeping.\"",
            suggestions, StringComparison.Ordinal);
        Assert.Contains("\"table\": \"[audit].[tblLog]\"", suggestions, StringComparison.Ordinal);
        Assert.Contains("\"column\": \"effort_MothraID\"", suggestions, StringComparison.Ordinal);
        Assert.Contains("\"lookIn\": \"VIPER.users.Person\"", suggestions, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_Output_ReadsAndBindsAsWritten()
    {
        var folder = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Join(folder.FullName, "analysis.suggestions.json");
            File.WriteAllText(path, Build());

            var plan = AnalysisPlan.Bind(Summary(), Declarations.Load(path));

            Assert.Equal(3, plan.DeclaredChecks.Count);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public void Build_WithAnInclude_GuessesOnlyForIncludedTablesAndListsNoInventory()
    {
        var suggestions = Build("""{ "include": ["[dbo]"] }""");

        Assert.DoesNotContain("// \"include\": [", suggestions, StringComparison.Ordinal);
        Assert.DoesNotContain("[audit].[tblLog]", suggestions, StringComparison.Ordinal);
        Assert.Contains("\"table\": \"[dbo].[tblEffort]\"", suggestions, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WhatIsDeclared_IsNotSuggestedAgain()
    {
        var suggestions = Build("""
            { "exclude": ["[dbo].[tblCourses_backup_2017_08_25]"],
              "dateColumns": { "[dbo].[tblEffort]": "effort_Modified" },
              "checks": [
                { "check": "valuesMustExistIn", "table": "[dbo].[tblEffort]", "columns": ["course_ID"], "otherTable": "[dbo].[tblCourses]", "otherColumns": ["course_ID"] },
                { "check": "identifiesOnePerson", "table": "[dbo].[tblEffort]", "column": "effort_MothraID", "idType": "MothraId", "lookIn": "VIPER.users.Person" }
              ] }
            """);

        Assert.DoesNotContain("// \"include\": [", suggestions, StringComparison.Ordinal);
        Assert.DoesNotContain("tblCourses_backup", suggestions, StringComparison.Ordinal);
        Assert.DoesNotContain("effort_Created", suggestions, StringComparison.Ordinal);
        Assert.DoesNotContain("\"table\": \"[dbo].[tblEffort]\"", suggestions, StringComparison.Ordinal);
        // The audit table's course_ID is not declared, so it is still guessed.
        Assert.Contains("\"table\": \"[audit].[tblLog]\"", suggestions, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_GenericOrAmbiguousKeyName_IsNotGuessedAsARelationship()
    {
        var summary = new SchemaSummary
        {
            Database = "Legacy",
            Tables =
            [
                Table("[dbo].[Terms]", "ID", Column("ID", "int")),
                Table("[dbo].[Units]", "unit_code", Column("unit_code", "varchar(10)")),
                Table("[hr].[Units]", "unit_code", Column("unit_code", "varchar(10)")),
                Table("[dbo].[Staff]", "staff_ID", Column("staff_ID", "int"), Column("ID", "int"), Column("unit_code", "varchar(10)"))
            ]
        };

        var suggestions = Suggestions.Build(summary, "Legacy", "Production", "analysis.json", new Declarations());

        Assert.DoesNotContain("valuesMustExistIn", suggestions, StringComparison.Ordinal);
    }
}
