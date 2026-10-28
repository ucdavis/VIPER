using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// The validator is what keeps a customSql file from writing to a legacy database, so these cover what it
/// must accept and, mostly, what it must refuse.
/// </summary>
[InvariantCulture]
public class CustomSqlValidatorTests
{
    private const int CompatibilityLevel = 130;

    private static CustomSqlValidator.Outcome Validate(string sql) => CustomSqlValidator.Validate(sql, CompatibilityLevel);

    [Theory]
    [InlineData("SELECT percent_ID, percent_start FROM dbo.tblPercent WHERE percent_AcademicYear IS NULL")]
    [InlineData("SELECT p.percent_ID FROM [dbo].[tblPercent] AS p WITH (NOLOCK) WHERE p.percent_start IS NOT NULL;")]
    [InlineData("SELECT c.course_ID FROM tblCourses c WHERE EXISTS (SELECT 1 FROM tblEffort e WHERE e.effort_CourseID = c.course_ID)")]
    [InlineData("SELECT a FROM t1 UNION SELECT a FROM t2 EXCEPT SELECT a FROM t3")]
    [InlineData("WITH used AS (SELECT DISTINCT effort_CourseID FROM tblEffort) SELECT c.course_ID FROM tblCourses c JOIN used u ON u.effort_CourseID = c.course_ID")]
    [InlineData("-- DELETE FROM tblPercent is only a comment\nSELECT percent_ID FROM tblPercent")]
    [InlineData("SELECT percent_ID FROM tblPercent WHERE note = 'DROP TABLE tblPercent; --'")]
    // Hints that take no locks, wait for none, or only steer the plan
    [InlineData("SELECT percent_ID FROM tblPercent WITH (READUNCOMMITTED, READPAST)")]
    [InlineData("SELECT percent_ID FROM tblPercent WITH (NOWAIT)")]
    [InlineData("SELECT percent_ID FROM tblPercent WITH (INDEX(1), FORCESEEK)")]
    // Built-in functions, scalar and table-valued
    [InlineData("SELECT percent_ID FROM tblPercent WHERE LEN(LTRIM(RTRIM(note))) = 0 AND TRY_CONVERT(int, note) IS NULL")]
    [InlineData("SELECT p.percent_ID FROM tblPercent p CROSS APPLY STRING_SPLIT(p.note, ',') s WHERE s.value = ''")]
    public void Validate_ReadOnlySelect_IsAccepted(string sql)
    {
        var outcome = Validate(sql);

        Assert.Empty(outcome.Errors);
        Assert.NotEmpty(outcome.Tables);
    }

    [Fact]
    public void Validate_ReturnsTablesButNotCommonTableExpressions()
    {
        var outcome = Validate(
            "WITH used AS (SELECT effort_CourseID FROM dbo.tblEffort) " +
            "SELECT c.course_ID FROM [Effort].[dbo].[tblCourses] c JOIN used u ON u.effort_CourseID = c.course_ID");

        Assert.Equal(2, outcome.Tables.Count);
        Assert.Contains(new CustomSqlValidator.TableName(null, "dbo", "tblEffort"), outcome.Tables);
        Assert.Contains(new CustomSqlValidator.TableName("Effort", "dbo", "tblCourses"), outcome.Tables);
    }

    [Theory]
    // Statements that write or run other code
    [InlineData("INSERT INTO tblPercent (percent_ID) VALUES (1)")]
    [InlineData("UPDATE tblPercent SET percent_AcademicYear = NULL")]
    [InlineData("DELETE FROM tblPercent")]
    [InlineData("MERGE tblPercent AS t USING tblPercent AS s ON t.percent_ID = s.percent_ID WHEN MATCHED THEN DELETE;")]
    [InlineData("TRUNCATE TABLE tblPercent")]
    [InlineData("DROP TABLE tblPercent")]
    [InlineData("EXEC dbo.usp_clean_run 1")]
    [InlineData("EXEC('DELETE FROM tblPercent')")]
    [InlineData("WAITFOR DELAY '00:10:00'")]
    // More than one statement, however it is hidden
    [InlineData("SELECT 1 FROM tblPercent; DELETE FROM tblPercent")]
    [InlineData("SELECT 1 FROM tblPercent\nGO\nSELECT 2 FROM tblPercent")]
    [InlineData("SELECT 1 FROM tblPercent --\n; DELETE FROM tblPercent")]
    [InlineData("DECLARE @year int = 2024; SELECT percent_ID FROM tblPercent WHERE percent_AcademicYear = @year")]
    [InlineData("")]
    // A SELECT that creates a table
    [InlineData("SELECT percent_ID INTO dbo.copy FROM tblPercent")]
    [InlineData("SELECT percent_ID INTO #copy FROM tblPercent")]
    // Temporary tables and variables, which need another statement to exist
    [InlineData("SELECT * FROM #schedsInRun")]
    [InlineData("SELECT percent_ID FROM tblPercent WHERE percent_ID = @id")]
    // Other servers and files
    [InlineData("SELECT * FROM OPENQUERY(BANNER, 'SELECT 1 FROM dual')")]
    [InlineData("SELECT * FROM OPENROWSET('SQLNCLI', 'Server=x;Trusted_Connection=yes;', 'SELECT 1')")]
    [InlineData("SELECT * FROM OPENROWSET(BULK 'C:\\data.txt', SINGLE_CLOB) AS f")]
    [InlineData("SELECT * FROM OPENDATASOURCE('SQLNCLI', 'Data Source=x;Integrated Security=SSPI').db.dbo.t")]
    [InlineData("SELECT * FROM LinkedServer.Effort.dbo.tblPercent")]
    // Changes to the database from inside a SELECT
    [InlineData("SELECT NEXT VALUE FOR dbo.seq FROM tblPercent")]
    // User-defined functions, whose code cannot be checked, in this database or another
    [InlineData("SELECT percent_ID FROM tblPercent WHERE dbo.fn_isValid(percent_ID) = 0")]
    [InlineData("SELECT percent_ID FROM tblPercent WHERE OtherDb.dbo.fn_isValid(percent_ID) = 0")]
    [InlineData("SELECT f.id FROM dbo.tvf_rows(1) AS f")]
    [InlineData("SELECT f.id FROM OtherDb.dbo.tvf_rows(1) AS f")]
    [InlineData("SELECT p.percent_ID FROM tblPercent p CROSS APPLY dbo.tvf_rows(p.percent_ID) f")]
    [InlineData("SELECT percent_ID FROM tblPercent t WHERE t.doc.value('(/a)[1]', 'int') = 1")]
    // Wherever in the query the call is
    [InlineData("SELECT dbo.fn_label(percent_ID) FROM tblPercent")]
    [InlineData("WITH x AS (SELECT dbo.fn_label(percent_ID) AS l FROM tblPercent) SELECT l FROM x")]
    [InlineData("SELECT percent_ID FROM tblPercent WHERE EXISTS (SELECT 1 FROM tblCourses WHERE dbo.fn_isValid(course_ID) = 1)")]
    [InlineData("SELECT p.percent_ID FROM tblPercent p JOIN tblCourses c ON dbo.fn_match(p.percent_ID, c.course_ID) = 1")]
    [InlineData("SELECT f.id FROM tvf_rows(1) AS f")]
    // Hints that take locks or wait for them, even with an allowed hint beside them
    [InlineData("SELECT percent_ID FROM tblPercent WITH (UPDLOCK)")]
    [InlineData("SELECT percent_ID FROM tblPercent WITH (TABLOCKX)")]
    [InlineData("SELECT percent_ID FROM tblPercent WITH (HOLDLOCK)")]
    [InlineData("SELECT percent_ID FROM tblPercent WITH (READCOMMITTED)")]
    [InlineData("SELECT percent_ID FROM tblPercent WITH (READCOMMITTEDLOCK)")]
    [InlineData("SELECT percent_ID FROM tblPercent WITH (PAGLOCK)")]
    [InlineData("SELECT percent_ID FROM tblPercent WITH (NOLOCK, TABLOCK)")]
    // Not valid T-SQL
    [InlineData("SELECT FROM WHERE")]
    public void Validate_AnythingButAReadOnlySelect_IsRefused(string sql)
    {
        var outcome = Validate(sql);

        Assert.NotEmpty(outcome.Errors);
        Assert.Empty(outcome.Tables);
    }

    [Fact]
    public void Validate_TableVariable_IsRefusedOnce()
    {
        var outcome = Validate("SELECT * FROM @rows");

        Assert.Single(outcome.Errors);
    }

    [Fact]
    public void Validate_UserDefinedFunction_IsRefusedByName()
    {
        var outcome = Validate("SELECT percent_ID FROM tblPercent WHERE OtherDb.dbo.fn_isValid(percent_ID) = 0");

        Assert.Equal("line 1: OtherDb.dbo.fn_isValid is a user-defined function or a method, whose code cannot be checked. Write what it does into the query.",
            Assert.Single(outcome.Errors));
    }

    [Fact]
    public void Validate_PlainColumnReferences_AreTracedToTheirTable()
    {
        var outcome = Validate("SELECT p.percent_ID AS id, p.note, LEN(p.note) AS n, c.course_ID FROM dbo.tblPercent AS p JOIN tblCourses c ON c.course_ID = p.percent_ID");

        var sources = Assert.IsAssignableFrom<IReadOnlyDictionary<string, CustomSqlValidator.ColumnSource>>(outcome.Sources);
        Assert.Equal(new CustomSqlValidator.ColumnSource(new(null, "dbo", "tblPercent"), "percent_ID"), sources["ID"]);
        Assert.Equal(new CustomSqlValidator.ColumnSource(new(null, "dbo", "tblPercent"), "note"), sources["note"]);
        Assert.Equal(new CustomSqlValidator.ColumnSource(new(null, null, "tblCourses"), "course_ID"), sources["course_ID"]);
        Assert.False(sources.ContainsKey("n"));
    }

    [Fact]
    public void Validate_UnqualifiedColumnOfTheOnlyTable_IsTraced()
    {
        Assert.Equal("percent_ID", Validate("SELECT percent_ID FROM tblPercent").Sources!["percent_ID"].Column);
    }

    [Theory]
    // Two tables, so an unqualified column could be either
    [InlineData("SELECT percent_ID FROM tblPercent p JOIN tblCourses c ON c.course_ID = p.percent_ID")]
    // A common table expression or a derived table, whose column may be computed
    [InlineData("WITH x AS (SELECT LEN(note) AS percent_ID FROM tblPercent) SELECT x.percent_ID FROM x")]
    [InlineData("SELECT x.percent_ID FROM (SELECT LEN(note) AS percent_ID FROM tblPercent) AS x")]
    // More than one SELECT
    [InlineData("SELECT percent_ID FROM tblPercent UNION SELECT course_ID FROM tblCourses")]
    // A name two output columns share, or an alias used twice
    [InlineData("SELECT p.percent_ID, p.note AS percent_ID FROM tblPercent p")]
    [InlineData("SELECT p.percent_ID FROM tblPercent p CROSS JOIN tblCourses p")]
    // An expression, even a simple one
    [InlineData("SELECT ISNULL(p.percent_ID, 0) AS percent_ID FROM tblPercent p")]
    [InlineData("SELECT CAST(p.note AS int) AS percent_ID FROM tblPercent p")]
    public void Validate_ColumnThatMayNotBeTheTables_IsNotTraced(string sql)
    {
        var outcome = Validate(sql);

        Assert.Empty(outcome.Errors);
        Assert.False(outcome.Sources?.ContainsKey("percent_ID") ?? false);
    }

    [Fact]
    public void Validate_Error_GivesTheLine()
    {
        var outcome = Validate("SELECT percent_ID\nFROM tblPercent WITH (XLOCK)");

        Assert.StartsWith("line 2:", Assert.Single(outcome.Errors));
    }
}
