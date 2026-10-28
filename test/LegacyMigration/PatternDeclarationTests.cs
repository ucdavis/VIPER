using System.Text.Json;
using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// matchesPattern and doesNotMatchPattern bind to one text column; their patterns go to SQL Server as
/// parameters, so binding only checks that there are some.
/// </summary>
[InvariantCulture]
public class PatternDeclarationTests
{
    private const string Where = "checks[0]";

    private static SchemaBinder Binder()
    {
        var summary = new SchemaSummary
        {
            Database = "Effort",
            CompatibilityLevel = 130,
            Tables =
            [
                new TableSummary("[dbo].[tblPercent]", 10, true, true, 0, ["percent_ID"], [],
                [
                    new ColumnSummary("percent_ID", "int", null, null, false, false, null, null),
                    new ColumnSummary("percent_AcademicYear", "char(9)", null, 9, true, false, null, null)
                ])
            ]
        };
        return new SchemaBinder(new SchemaIndex(summary, new HashSet<string>()))
        {
            DeclarationsFolder = Path.GetTempPath(),
            Database = summary.Database,
            CompatibilityLevel = summary.CompatibilityLevel
        };
    }

    private static DeclaredCheck? Bind(string check, string json, SchemaBinder binder)
    {
        var type = CheckRegistry.Declared[check];
        var declaration = (CheckDeclaration)JsonSerializer.Deserialize(json, type, Declarations.ReadOptions)!;
        return declaration.Bind(binder, Where);
    }

    [Theory]
    [InlineData("matchesPattern", true)]
    [InlineData("doesNotMatchPattern", false)]
    public void Bind_TextColumn_BindsWithItsPatterns(string check, bool mustMatch)
    {
        var binder = Binder();

        var bound = Bind(check, $$"""
            { "check": "{{check}}", "table": "[dbo].[tblPercent]", "column": "percent_AcademicYear",
              "patterns": ["[12][0-9][0-9][0-9]-[12][0-9][0-9][0-9]", "%test%"] }
            """, binder);

        Assert.Empty(binder.Errors);
        var pattern = Assert.IsType<MatchesPatternCheck>(bound);
        Assert.Equal(mustMatch, pattern.MustMatch);
        Assert.Equal(["[12][0-9][0-9][0-9]-[12][0-9][0-9][0-9]", "%test%"], pattern.Patterns);
        Assert.Equal($"{check}: percent_AcademicYear", pattern.Name);
    }

    [Fact]
    public void Bind_NumberColumn_IsRefused()
    {
        var binder = Binder();

        var bound = Bind("matchesPattern", """
            { "check": "matchesPattern", "table": "[dbo].[tblPercent]", "column": "percent_ID", "patterns": ["[0-9]%"] }
            """, binder);

        Assert.Null(bound);
        Assert.Contains(binder.Errors, e => e.Contains("checks text columns", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[\"%a%\", \"\"]")]
    public void Bind_NoPatternsOrAnEmptyOne_IsRefused(string patterns)
    {
        var binder = Binder();

        var bound = Bind("doesNotMatchPattern", $$"""
            { "check": "doesNotMatchPattern", "table": "[dbo].[tblPercent]", "column": "percent_AcademicYear", "patterns": {{patterns}} }
            """, binder);

        Assert.Null(bound);
        Assert.Contains(binder.Errors, e => e.Contains("\"patterns\"", StringComparison.Ordinal));
    }
}
