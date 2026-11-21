using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// Binding is where a customSql query is held to this database's schema export and its file to the
/// declarations folder; the validator alone allows another database's name in front of a table.
/// </summary>
[InvariantCulture]
public sealed class CustomSqlDeclarationTests : IDisposable
{
    private const string Where = "checks[0] (customSql)";

    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("customsql-tests-");

    public void Dispose() => _folder.Delete(recursive: true);

    private static TableSummary Table(string name, params string[] columns) => new(
        name, 10, true, true, 0, [columns[0]], [],
        columns.Select(c => new ColumnSummary(c, "int", null, null, false, false, null, null)).ToList());

    private SchemaBinder Binder()
    {
        var summary = new SchemaSummary
        {
            Database = "Effort",
            CompatibilityLevel = 130,
            Tables =
            [
                Table("[dbo].[tblPercent]", "percent_ID", "percent_AcademicYear"),
                Table("[dbo].[tblCourses]", "course_id", "course_CRN"),
                Table("[audit].[tblLog]", "log_ID")
            ]
        };
        return new SchemaBinder(new SchemaIndex(summary, new HashSet<string>()))
        {
            DeclarationsFolder = _folder.FullName,
            Database = summary.Database,
            CompatibilityLevel = summary.CompatibilityLevel
        };
    }

    private CustomSqlDeclaration Declaration(string sql, string file = "query.sql", List<string>? keyColumns = null, string? table = null)
    {
        File.WriteAllText(Path.Join(_folder.FullName, "query.sql"), sql);
        return new CustomSqlDeclaration
        {
            Check = "customSql",
            Name = "test",
            File = file,
            KeyColumns = keyColumns ?? ["percent_ID"],
            Table = table
        };
    }

    [Fact]
    public void Bind_ValidQuery_ReadsItsTablesOnce()
    {
        var binder = Binder();

        var check = Declaration(
            "SELECT p.percent_ID FROM TBLPERCENT p JOIN [Effort].[dbo].[tblCourses] c ON c.course_id = p.percent_ID " +
            "WHERE EXISTS (SELECT 1 FROM dbo.tblPercent x JOIN audit.tblLog l ON l.log_ID = x.percent_ID)")
            .Bind(binder, Where);

        Assert.Empty(binder.Errors);
        var bound = Assert.IsType<CustomSqlCheck>(check);
        Assert.Equal(["[audit].[tblLog]", "[dbo].[tblCourses]", "[dbo].[tblPercent]"], bound.Tables.Select(t => t.Name).Order().ToList());
    }

    [Fact]
    public void Bind_KeyColumns_AreTracedToTheirTableColumnWhereTheyCanBe()
    {
        var binder = Binder();

        var check = Declaration("SELECT p.Percent_ID AS id, LEN(p.percent_AcademicYear) AS n FROM TBLPERCENT p", keyColumns: ["ID", "n"])
            .Bind(binder, Where);

        var bound = Assert.IsType<CustomSqlCheck>(check);
        Assert.Equal(["[dbo].[tblPercent].[percent_ID]", null], bound.KeySources.Select(c => c?.ToString()));
    }

    [Theory]
    [InlineData("SELECT percent_ID FROM SIS.dbo.tb_CareerSelection", "another database")]
    [InlineData("SELECT percent_ID FROM dbo.tblMissing", "not a table or view in the schema export")]
    [InlineData("SELECT percent_ID FROM audit.tblPercent", "not a table or view in the schema export")]
    [InlineData("DELETE FROM tblPercent", "query.sql, line 1:")]
    [InlineData("SELECT 1 AS percent_ID", "reads no table or view")]
    public void Bind_QueryOutsideThisDatabaseOrInvalid_IsRefused(string sql, string expected)
    {
        var binder = Binder();

        var check = Declaration(sql).Bind(binder, Where);

        Assert.Null(check);
        Assert.Contains(binder.Errors, e => e.StartsWith(Where, StringComparison.Ordinal) && e.Contains(expected, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(@"..\query.sql")]
    [InlineData("../query.sql")]
    [InlineData(@"sub\query.sql")]
    [InlineData("query.txt")]
    [InlineData("")]
    public void Bind_FileNotASqlFileBesideTheDeclarations_IsRefused(string file)
    {
        File.WriteAllText(Path.Join(_folder.FullName, "query.txt"), "SELECT percent_ID FROM tblPercent");
        var binder = Binder();

        var check = Declaration("SELECT percent_ID FROM tblPercent", file).Bind(binder, Where);

        Assert.Null(check);
        Assert.Contains(binder.Errors, e => e.Contains("\"file\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Bind_MissingFile_IsRefused()
    {
        var binder = Binder();

        var check = Declaration("SELECT percent_ID FROM tblPercent", "missing.sql").Bind(binder, Where);

        Assert.Null(check);
        Assert.Contains(binder.Errors, e => e.Contains("missing.sql could not be read", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(new string[0], "at least one column")]
    [InlineData(new[] { "percent_ID", "PERCENT_ID" }, "more than once")]
    public void Bind_KeyColumnsEmptyOrRepeated_IsRefused(string[] keyColumns, string expected)
    {
        var binder = Binder();

        var check = Declaration("SELECT percent_ID FROM tblPercent", keyColumns: [.. keyColumns]).Bind(binder, Where);

        Assert.Null(check);
        Assert.Contains(binder.Errors, e => e.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void Bind_TableField_IsRefused()
    {
        var binder = Binder();

        var check = Declaration("SELECT percent_ID FROM tblPercent", table: "[dbo].[tblPercent]").Bind(binder, Where);

        Assert.Null(check);
        Assert.Contains(binder.Errors, e => e.Contains("\"table\" is not used", StringComparison.Ordinal));
    }
}
