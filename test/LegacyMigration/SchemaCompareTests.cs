using System.Text.Json;
using System.Text.RegularExpressions;
using Viper.Scripts.LegacyMigration;

namespace Viper.test.LegacyMigration;

/// <summary>
/// The schema comparison reads two exports' files only. Each object both have is identical, different,
/// different only in whitespace, or a possible permission gap (one side could not see its definition);
/// the rest are only in one environment. The report lists each group and totals them.
/// </summary>
[InvariantCulture]
public sealed class SchemaCompareTests : IDisposable
{
    private readonly DirectoryInfo _connection = Directory.CreateTempSubdirectory("schema-compare-tests-");

    public void Dispose() => _connection.Delete(recursive: true);

    private void Export(string environment, SchemaSummary summary, params (string Path, string Text)[] files)
    {
        var dir = Path.Join(_connection.FullName, environment);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Join(dir, "summary.json"), JsonSerializer.Serialize(summary, SchemaExport.JsonOptions));
        foreach (var (path, text) in files)
        {
            var full = Path.Join(dir, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, text);
        }
    }

    private string Report() => new SchemaCompare("ClinicalScheduler", "Test", "Production", _connection.FullName).Report();

    private static List<string> Lines(string report) => report.Split("\r\n").ToList();

    /// <summary>
    /// One object of each kind, Test seeing every definition and Production lacking VIEW DEFINITION.
    /// </summary>
    private void ExportBoth()
    {
        Export("Test",
            new SchemaSummary { Server = "test-sql", Database = "CS", CompatibilityLevel = 130, HasViewDefinition = true, UnexportedObjects = ["[dbo].[asm] (assembly)"] },
            ("Tables/dbo.Same.sql", "CREATE TABLE dbo.Same (id int)"),
            ("Tables/dbo.Changed.sql", "CREATE TABLE dbo.Changed (id int)"),
            ("Views/dbo.Spaced.sql", "CREATE VIEW dbo.Spaced AS\r\nSELECT 1 AS x"),
            ("Procedures/dbo.Hidden.sql", "CREATE PROCEDURE dbo.Hidden AS SELECT 1"),
            ("Tables/dbo.OnlyTest.sql", "CREATE TABLE dbo.OnlyTest (id int)"));
        Export("Production",
            new SchemaSummary { Server = "prod-sql", Database = "CS", CompatibilityLevel = 120, HasViewDefinition = false },
            ("Tables/dbo.Same.sql", "CREATE TABLE dbo.Same (id int)"),
            ("Tables/dbo.Changed.sql", "CREATE TABLE dbo.Changed (id bigint)"),
            ("Views/dbo.Spaced.sql", "CREATE VIEW dbo.Spaced AS\r\n    SELECT 1 AS x   "),
            ("Procedures/dbo.Hidden.sql", $"{SchemaExport.MissingDefinitionMarker} no VIEW DEFINITION permission\r\n"),
            ("Tables/dbo.OnlyProduction.sql", "CREATE TABLE dbo.OnlyProduction (id int)"));
    }

    [Fact]
    public void Report_SortsEachObjectIntoOneGroup()
    {
        ExportBoth();

        var lines = Lines(Report());

        Assert.Equal(["DEFINITIONS THAT DIFFER (1)", "  Tables/dbo.Changed"], lines.SkipWhile(l => !l.StartsWith("DEFINITIONS", StringComparison.Ordinal)).Take(2));
        Assert.Equal(["WHITESPACE-ONLY DIFFERENCES (1)", "  Views/dbo.Spaced"], lines.SkipWhile(l => !l.StartsWith("WHITESPACE", StringComparison.Ordinal)).Take(2));
        Assert.Equal(["OBJECTS ONLY IN TEST (1)", "  Tables/dbo.OnlyTest"], lines.SkipWhile(l => !l.StartsWith("OBJECTS ONLY IN TEST", StringComparison.Ordinal)).Take(2));
        Assert.Equal(["OBJECTS ONLY IN PRODUCTION (1)", "  Tables/dbo.OnlyProduction"],
            lines.SkipWhile(l => !l.StartsWith("OBJECTS ONLY IN PRODUCTION", StringComparison.Ordinal)).Take(2));
    }

    [Fact]
    public void Report_HiddenDefinitionAndMissingViewDefinition_ArePermissionGapsNotDifferences()
    {
        ExportBoth();

        var lines = Lines(Report());

        Assert.Contains("POSSIBLE PERMISSION GAPS (2)", lines);
        Assert.Contains(lines, l => l.StartsWith("  The Production login lacks VIEW DEFINITION.", StringComparison.Ordinal));
        Assert.Contains("  Procedures/dbo.Hidden: definition not visible in Production (no VIEW DEFINITION permission)", lines);
    }

    [Fact]
    public void Report_MarksEnvironmentDifferencesAndListsUnscriptedTypesOnlyInOne()
    {
        ExportBoth();

        var report = Report();

        Assert.Matches(new Regex(@"\* Compatibility level\s+130\s+120"), report);
        Assert.Matches(new Regex(@"  {2}Database\s+CS\s+CS"), report);
        Assert.Contains("UNSCRIPTED OBJECT TYPES (1)\r\n  [dbo].[asm] (assembly): only in Test", report, StringComparison.Ordinal);
        Assert.DoesNotContain("EXCLUDED TOOL OBJECTS", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Report_TotalsEveryGroupAndGivesDiffCommands()
    {
        ExportBoth();

        var report = Report();

        foreach (var (label, count) in new[] { ("Identical", 1), ("Different", 1), ("Whitespace-only", 1), ("Possible permission gaps", 1), ("Only in Test", 1), ("Only in Production", 1) })
        {
            Assert.Matches(new Regex($@"  {Regex.Escape(label)}:\s+{count}\r\n"), report);
        }
        Assert.Contains("git diff --no-index --stat -- ", report, StringComparison.Ordinal);
        Assert.Contains("Changed.sql\"", report, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("SELECT 'A  B' AS x", "SELECT 'A B' AS x")]
    [InlineData("SELECT 1 AS [a  b]", "SELECT 1 AS [a b]")]
    [InlineData("SELECT 1 /* a  b */ AS x", "SELECT 1 /* a b */ AS x")]
    [InlineData("SELECT 1 AS x -- note\r\nFROM dbo.tbl", "SELECT 1 AS x -- note FROM dbo.tbl")]
    public void Report_WhitespaceInsideStringsNamesOrComments_IsADifference(string test, string production)
    {
        Export("Test", new SchemaSummary(), ("Views/dbo.Literal.sql", $"CREATE VIEW dbo.Literal AS\r\n{test}"));
        Export("Production", new SchemaSummary(), ("Views/dbo.Literal.sql", $"CREATE VIEW dbo.Literal AS\r\n    {production}"));

        var lines = Lines(Report());

        Assert.Equal(["DEFINITIONS THAT DIFFER (1)", "  Views/dbo.Literal"], lines.SkipWhile(l => !l.StartsWith("DEFINITIONS", StringComparison.Ordinal)).Take(2));
        Assert.Contains("WHITESPACE-ONLY DIFFERENCES (0)", lines);
    }

    [Fact]
    public void Normalize_CollapsesWhitespaceBetweenTokensOnly()
    {
        Assert.Equal("SELECT 'A  B' AS x", SchemaCompare.Normalize("  SELECT\r\n\t'A  B'   AS x  \r\n"));
    }

    [Fact]
    public void Normalize_KeepsTheLineBreakThatEndsALineComment()
    {
        Assert.Equal("SELECT 1 -- note\nFROM dbo.tbl", SchemaCompare.Normalize("SELECT 1   -- note\r\n\r\n    FROM dbo.tbl\r\n"));
    }

    [Fact]
    public void Report_ViewsNamedAlikeButForCase_AreLeftOutOfTheColumnCheck()
    {
        static SchemaSummary Views(string type) => new()
        {
            Views =
            [
                new ViewSummary("[dbo].[V]", [new ColumnSummary("id", type, null, null, true, false, null, null)]),
                new ViewSummary("[dbo].[v]", [new ColumnSummary("id", "int", null, null, true, false, null, null)])
            ]
        };
        Export("Test", Views("int"), ("Views/dbo.V.sql", "CREATE VIEW dbo.V AS SELECT 1 AS id"));
        Export("Production", Views("bigint"), ("Views/dbo.V.sql", "CREATE VIEW dbo.V AS SELECT 1 AS id"));

        var report = Report();

        Assert.Contains("VIEW COLUMNS THAT DIFFER (0)", report, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"  Identical:\s+1\r\n"), report);
    }

    private static SchemaSummary WithView(params (string Name, string Type)[] columns) => new()
    {
        Views = [new ViewSummary("[dbo].[vAll]", [.. columns.Select(c => new ColumnSummary(c.Name, c.Type, null, null, true, false, null, null))])]
    };

    [Theory]
    [InlineData("only in Production: added; types differ: name (varchar(10) in Test, varchar(20) in Production)", "name", "varchar(20)", "added")]
    [InlineData("column order differs", "id", "int", null)]
    public void Report_SameViewDefinitionWithDifferentColumns_IsNotIdentical(string expected, string secondName, string secondType, string? third)
    {
        const string view = "CREATE VIEW dbo.vAll AS SELECT * FROM dbo.tbl";
        Export("Test", WithView(("id", "int"), ("name", "varchar(10)")), ("Views/dbo.vAll.sql", view));
        (string, string)[] production = third != null
            ? [("id", "int"), (secondName, secondType), (third, "int")]
            : [("name", "varchar(10)"), (secondName, secondType)];
        Export("Production", WithView(production), ("Views/dbo.vAll.sql", view));

        var report = Report();

        Assert.Contains($"VIEW COLUMNS THAT DIFFER (1)\r\n  Views/dbo.vAll: {expected}\r\n", report, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"  Same definition, different view columns:\s+1\r\n"), report);
        Assert.Matches(new Regex(@"  Identical:\s+0\r\n"), report);
    }

    [Theory]
    [InlineData("[dbo].[vAll]", "Views/dbo.vAll")]
    [InlineData("[a.b].[c]", "Views/a%2Eb.c")]
    [InlineData("[dbo].[odd]]name]", "Views/dbo.odd]name")]
    public void ViewFileKey_MatchesTheExportsFileName(string quoted, string expected)
    {
        Assert.Equal(expected, SchemaCompare.ViewFileKey(quoted));
    }

    [Fact]
    public void Report_EnvironmentNotExported_SaysToRunTheExportFirst()
    {
        Export("Test", new SchemaSummary());

        var ex = Assert.Throws<InvalidOperationException>(Report);

        Assert.Contains("No export found for Production", ex.Message, StringComparison.Ordinal);
        Assert.Contains("RunSchemaExport.bat", ex.Message, StringComparison.Ordinal);
    }
}
