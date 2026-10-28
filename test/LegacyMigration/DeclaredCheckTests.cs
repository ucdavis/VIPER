using System.Data;
using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// The declared checks run against known rows: what each counts and lists, and what it keeps out of the
/// report. A LoginId ("jsmith") stands in for any value that must never be copied unless its column is safe;
/// values reach queries only as parameters.
/// </summary>
[InvariantCulture]
public class DeclaredCheckTests
{
    private const string LoginId = "jsmith";

    /// <summary>
    /// One declared check run against the fake database, with safeColumns entries as "table.column". Each
    /// entry has looksPersonal, so a column such as visitor.notes can stand in for any text column.
    /// </summary>
    private static List<CheckResult> Run(string check, FakeDatabase database, params string[] safe)
    {
        var entries = safe.Select(s => $$"""{ "table": "{{s[..s.LastIndexOf('.')]}}", "column": "{{s[(s.LastIndexOf('.') + 1)..]}}", "reason": "Codes.", "looksPersonal": "Test codes." }""");
        var plan = TestSchema.Plan($$"""{ "safeColumns": [{{string.Join(", ", entries)}}], "checks": [{{check}}] }""");
        return Assert.Single(plan.DeclaredChecks).Run(TestSchema.Context(plan, database)).ToList();
    }

    private static void AssertNeverShown(CheckResult result, FakeDatabase database)
    {
        Assert.DoesNotContain(LoginId, string.Join("\n", result.Details.Concat(result.Examples)
            .Concat(result.Grids.SelectMany(g => g.Rows.SelectMany(r => r)))), StringComparison.Ordinal);
        Assert.DoesNotContain(LoginId, database.AllSql, StringComparison.Ordinal);
    }

    [Fact]
    public void ValuesMustExistIn_SafeColumnValuesMissing_AreShown()
    {
        var database = new FakeDatabase().Answer("GROUP BY t.[gradYear]", [2010, 9L, 2L, 13L, null, null], [2011, 4L, 2L, 13L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "valuesMustExistIn", "table": "[dbo].[visitor]", "columns": ["gradYear"], "otherTable": "[dbo].[Status]", "otherColumns": ["GradYear"] }
            """, database, "[dbo].[visitor].gradYear"));

        Assert.Equal(CheckStatus.ProblemsFound, result.Status);
        Assert.Equal([("No match: values", 2L), ("No match: rows", 13L)], result.Counts.Select(c => (c.Label, c.Value)).ToList());
        Assert.Contains("  gradYear 2010: 9 rows", result.Details);
        Assert.Single(database.Queries);
    }

    [Fact]
    public void ValuesMustExistIn_NumbersInAColumnWithNoSafeOrigin_AreShapes()
    {
        var database = new FakeDatabase()
            .Answer("GROUP BY t.[gradYear]", [2010, 9L, 2L, 13L, null, null], [2011, 4L, 2L, 13L, null, null])
            .Answer("SELECT TOP (50) t.[id], COUNT_BIG(*) OVER ()", [5, 13L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "valuesMustExistIn", "table": "[dbo].[visitor]", "columns": ["gradYear"], "otherTable": "[dbo].[Status]", "otherColumns": ["GradYear"] }
            """, database));

        Assert.Contains("  2 values not shown (13 rows): digits only, 4 chars (2)", result.Details);
        Assert.DoesNotContain(result.Details, d => d.Contains("2010", StringComparison.Ordinal));
    }

    [Fact]
    public void ValuesMustExistIn_TextValueThatMayBeALoginId_IsAShapeWithItsRowsByKey()
    {
        var database = new FakeDatabase()
            .Answer("\n  AND EXISTS (")
            .Answer("GROUP BY t.[mothraid]", [LoginId, 2L, 2L, 3L, null, null], ["00999999", 1L, 2L, 3L, null, null])
            .Answer("SELECT TOP (50) t.[id], COUNT_BIG(*) OVER ()", [5, 2L, null, null], [9, 2L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "valuesMustExistIn", "table": "[dbo].[visitor]", "columns": ["mothraid"], "otherTable": "[dbo].[tblPerson]", "otherColumns": ["person_MothraID"] }
            """, database));

        Assert.Contains("  mothraid \"00999999\": 1 row", result.Details);
        Assert.Contains("  1 values not shown (2 rows): letters only, 6 chars", result.Details);
        Assert.Contains("  Rows by id (2): 5, 9", result.Details);
        AssertNeverShown(result, database);
        Assert.Contains(database.Queries, q => q.Parameters.Any(p => Equals(p.Value, LoginId)));
    }

    [Fact]
    public void ValuesMustExistIn_TextLookupIntoAnIdentity_ShowsOnlyValuesOfDigits()
    {
        var database = new FakeDatabase()
            .Answer("GROUP BY t.[detail]", [LoginId, 2L, 2L, 3L, null, null], ["42", 1L, 2L, 3L, null, null])
            .Answer("SELECT TOP (50) t.[log_ID], COUNT_BIG(*) OVER ()", [5, 2L, null, null], [9, 2L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "valuesMustExistIn", "table": "[audit].[ChangeLog]", "columns": ["detail"], "otherTable": "[dbo].[visitor]", "otherColumns": ["id"] }
            """, database));

        Assert.Contains("  detail \"42\": 1 row", result.Details);
        Assert.Contains("  1 values not shown (2 rows): letters only, 6 chars", result.Details);
        Assert.Contains("  Rows by log_ID (2): 5, 9", result.Details);
        AssertNeverShown(result, database);
    }

    [Fact]
    public void NotBlank_CountsNullAndBlankApart_AndListsTheRows()
    {
        var database = new FakeDatabase()
            .Answer("SELECT COUNT_BIG(CASE WHEN t.[notes] IS NULL", [1L, 2L])
            .Answer("t.[notes] IS NULL OR", [3, 3L, null, null], [4, 3L, null, null], [8, 3L, null, null]);

        var result = Assert.Single(Run("""{ "check": "notBlank", "table": "[dbo].[visitor]", "columns": ["notes"] }""", database));

        Assert.Equal([("NULL rows", 1L), ("Blank rows (empty or only spaces)", 2L)], result.Counts.Select(c => (c.Label, c.Value)).ToList());
        Assert.Equal(["3", "4", "8"], result.Examples);
    }

    [Fact]
    public void StartBeforeEnd_RowsEndingBeforeTheyStart_AreListed()
    {
        var database = new FakeDatabase().Answer("t.[endWeekID] < t.[startWeekID]", [11, 1L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "startBeforeEnd", "table": "[dbo].[visitor]", "startColumn": "startWeekID", "endColumn": "endWeekID" }
            """, database));

        Assert.Equal((CheckStatus.ProblemsFound, 1L), (result.Status, result.Counts[0].Value));
        Assert.Equal(["11"], result.Examples);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WithinRange_ValuesOutsideTheRange_AreShownOnlyForASafeColumn(bool safe)
    {
        var database = new FakeDatabase()
            .Answer("MIN(CASE WHEN", [0L, 2L, null, 99])
            .Answer("SELECT TOP (50) t.[id]", [6, 2L, null, null], [7, 2L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "withinRange", "table": "[dbo].[visitor]", "column": "startWeekID", "min": 1, "max": 52 }
            """, database, safe ? ["[dbo].[visitor].startWeekID"] : []));

        Assert.Equal([("Rows below 1", 0L), ("Rows above 52", 2L)], result.Counts.Select(c => (c.Label, c.Value)).ToList());
        Assert.Equal(safe, result.Details.Contains("Highest above the range: 99"));
        Assert.All(database.Queries, q => Assert.DoesNotContain("52", q.Sql, StringComparison.Ordinal));
        Assert.All(database.Queries, q => Assert.Equal(["@min", "@max"], q.Parameters.Select(p => p.ParameterName)));
    }

    [Fact]
    public void CanConvertTo_ValuesThatWillNotConvert_AreDescribedByShape()
    {
        var database = new FakeDatabase()
            .AnswerWhen(sql => sql.Contains("TRY_CAST", StringComparison.Ordinal) && sql.Contains("GROUP BY", StringComparison.Ordinal),
                _ => [[LoginId, 3L, 1L]])
            .Answer("TRY_CAST", [2, 3L, null, null]);

        var result = Assert.Single(Run("""{ "check": "canConvertTo", "table": "[dbo].[visitor]", "column": "mothraid", "type": "int" }""", database));

        Assert.Equal([("Rows that will not convert", 3L), ("Distinct values that will not convert", 1L)], result.Counts.Select(c => (c.Label, c.Value)).ToList());
        Assert.Contains("Values, by shape: letters only, 6 chars", result.Details);
        Assert.DoesNotContain(LoginId, string.Join("\n", result.Details), StringComparison.Ordinal);
    }

    [Fact]
    public void CanConvertTo_CompatibilityLevelBelow110_StillRuns()
    {
        var summary = TestSchema.Summary();
        var level100 = new SchemaSummary
        {
            Database = summary.Database,
            CompatibilityLevel = 100,
            HasViewDefinition = summary.HasViewDefinition,
            Tables = summary.Tables,
            Views = summary.Views,
            ForeignKeys = summary.ForeignKeys,
            PersonIdColumns = summary.PersonIdColumns
        };
        var plan = TestSchema.Plan("""{ "checks": [{ "check": "canConvertTo", "table": "[dbo].[visitor]", "column": "mothraid", "type": "int" }] }""", level100);
        var database = new FakeDatabase().Answer("TRY_CAST");

        var result = Assert.Single(Assert.Single(plan.DeclaredChecks).Run(TestSchema.Context(plan, database)));

        Assert.Equal(CheckStatus.NoProblemsFound, result.Status);
        Assert.Contains(database.Queries, q => q.Sql.Contains("TRY_CAST", StringComparison.Ordinal));
    }

    [Fact]
    public void MatchesPattern_FailingValuesByShape_WithThePatternsAsParameters()
    {
        var database = new FakeDatabase()
            .Answer("GROUP BY", ["jsmith@ucdavis", 2L, 1L])
            .Answer("SELECT TOP (50) t.[GradYear]", [2019, 2L, null, null], [2020, 2L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "matchesPattern", "table": "[dbo].[Status]", "column": "Label", "patterns": ["%_@_%._%"] }
            """, database));

        Assert.Contains("Values, by shape: contains @, with no dot after the @, 14 chars", result.Details);
        Assert.All(database.Queries, q => Assert.DoesNotContain("%_@_%._%", q.Sql, StringComparison.Ordinal));
        Assert.All(database.Queries, q => Assert.Equal("%_@_%._%", Assert.Single(q.Parameters).Value));
    }

    [Fact]
    public void DoesNotMatchPattern_CountsRowsPerPattern()
    {
        var database = new FakeDatabase()
            .Answer("GROUP BY", ["Test", 2L, 2L], ["demo", 1L, 2L])
            .Answer("SUM(CASE WHEN", [2L, 1L])
            .Answer("SELECT TOP (50) t.[id]", [3, 3L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "doesNotMatchPattern", "table": "[dbo].[visitor]", "column": "notes", "patterns": ["%test%", "%demo%"] }
            """, database));

        Assert.Contains(("Rows matching %test%", 2L), result.Counts.Select(c => (c.Label, c.Value)));
        Assert.Contains(("Rows matching %demo%", 1L), result.Counts.Select(c => (c.Label, c.Value)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MatchesOtherTable_DifferingValuePairs_AreReadOnlyForSafeColumns(bool safe)
    {
        var database = new FakeDatabase()
            .Answer("JOIN [dbo].[Status] AS p ON", ["Fall", "Spring", 3L])
            .AnswerWhen(sql => sql.Contains("NOT ((CASE WHEN", StringComparison.Ordinal) && sql.Contains("AND NOT ((CASE WHEN", StringComparison.Ordinal)
                    && !sql.Contains(") AND ((CASE WHEN", StringComparison.Ordinal),
                _ => [[1, 3L, null, null]])
            .Answer("SELECT TOP (50)");

        var result = Assert.Single(Run("""
            { "check": "matchesOtherTable", "table": "[dbo].[visitor]", "column": "notes", "otherTable": "[dbo].[Status]",
              "joinOn": [{ "column": "gradYear", "otherColumn": "GradYear" }], "otherColumn": "Label" }
            """, database, safe ? ["[dbo].[visitor].notes", "[dbo].[Status].Label"] : ["[dbo].[Status].Label"]));

        Assert.Equal(3L, result.Counts.Single(c => c.Label == "Rows that disagree").Value);
        Assert.Equal(safe, database.Queries.Exists(q => q.Sql.Contains("JOIN [dbo].[Status] AS p ON", StringComparison.Ordinal)));
        Assert.Equal(safe, result.Details.Contains("  \"Fall\" vs \"Spring\": 3 rows"));
    }

    [Fact]
    public void ListValues_ValuesNotAllowed_AreFlaggedAndCaseVariantsKeptApart()
    {
        var database = new FakeDatabase().Answer("GROUP BY", ["Open", 10L, null, null], ["open", 2L, null, null], ["Shut", 1L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "listValues", "table": "[dbo].[Status]", "column": "Label", "allowedValues": ["Open", "Closed"] }
            """, database));

        Assert.Equal(3L, result.Counts.Single(c => c.Label == "Rows with a value not in allowedValues").Value);
        Assert.Equal(["\"Open\": 10", "\"open\": 2  (not allowed)", "\"Shut\": 1  (not allowed)"], result.Details);
        Assert.Contains("COLLATE Latin1_General_BIN2, DATALENGTH(", database.Queries[0].Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ListValues_CharPadding_IsNotPartOfTheValue()
    {
        var database = new FakeDatabase().Answer("GROUP BY", ["AB   ", 5L, null, null], ["CD   ", 1L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "listValues", "table": "[dbo].[Status]", "column": "Abbrev", "allowedValues": ["AB"] }
            """, database));

        Assert.Equal(1L, result.Counts.Single(c => c.Label == "Rows with a value not in allowedValues").Value);
        Assert.Equal(["\"AB\": 5", "\"CD\": 1  (not allowed)"], result.Details);
    }

    /// <summary>
    /// The test schema with [dbo].[Flags], a bit column and a decimal column, for listValues on columns that are not text.
    /// </summary>
    private static AnalysisPlan FlagsPlan(string check)
    {
        var summary = TestSchema.Summary();
        summary.Tables.Add(new TableSummary("[dbo].[Flags]", 10, false, false, 0, [], [],
        [
            new ColumnSummary("active", "bit", null, null, true, false, null, null),
            new ColumnSummary("weight", "decimal(5,2)", null, null, true, false, null, null)
        ]));
        return TestSchema.Plan($$"""{ "checks": [{{check}}] }""", summary);
    }

    [Theory]
    [InlineData("""["0", "1"]""")]
    [InlineData("""["false", "TRUE"]""")]
    public void ListValues_BitColumn_ComparesAndShowsAsZeroAndOne(string allowed)
    {
        var plan = FlagsPlan($$"""{ "check": "listValues", "table": "[dbo].[Flags]", "column": "active", "allowedValues": {{allowed}} }""");
        var database = new FakeDatabase().Answer("GROUP BY", [true, 5L, null, null], [false, 2L, null, null], [null, 1L, null, null]);

        var result = Assert.Single(Assert.Single(plan.DeclaredChecks).Run(TestSchema.Context(plan, database)));

        Assert.Equal(CheckStatus.NoProblemsFound, result.Status);
        Assert.Equal(["1: 5", "0: 2", "<null>: 1"], result.Details);
    }

    [Fact]
    public void ListValues_DecimalColumn_ComparesByValueAndDropsTrailingZeros()
    {
        var plan = FlagsPlan("""{ "check": "listValues", "table": "[dbo].[Flags]", "column": "weight", "allowedValues": ["1", "2.5"] }""");
        var database = new FakeDatabase().Answer("GROUP BY", [1.00m, 4L, null, null], [2.50m, 3L, null, null], [3.00m, 1L, null, null]);

        var result = Assert.Single(Assert.Single(plan.DeclaredChecks).Run(TestSchema.Context(plan, database)));

        Assert.Equal(1L, result.Counts.Single(c => c.Label == "Rows with a value not in allowedValues").Value);
        Assert.Equal(["1: 4", "2.5: 3", "3: 1  (not allowed)"], result.Details);
    }

    [Fact]
    public void ListValues_NumberTooLargeForDotNet_IsNotAllowedRatherThanAFault()
    {
        // A decimal(38) value past .NET's decimal arrives as its text (AnalysisDatabase.ValueOf).
        const string TooLarge = "12345678901234567890123456789012345678";
        var plan = FlagsPlan("""{ "check": "listValues", "table": "[dbo].[Flags]", "column": "weight", "allowedValues": ["1"] }""");
        var database = new FakeDatabase().Answer("GROUP BY", [1.00m, 4L, null, null], [TooLarge, 1L, null, null]);

        var result = Assert.Single(Assert.Single(plan.DeclaredChecks).Run(TestSchema.Context(plan, database)));

        Assert.Equal(1L, result.Counts.Single(c => c.Label == "Rows with a value not in allowedValues").Value);
        Assert.Contains(result.Details, d => d.Contains(TooLarge, StringComparison.Ordinal) && d.EndsWith("(not allowed)", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("active", """["yes"]""", "give 0, 1, true, or false")]
    [InlineData("weight", """["1", "one"]""", "\"allowedValues\" has \"one\", but weight is decimal(5,2); give numbers")]
    public void ListValues_AllowedValueThatCannotMatchANumberOrBitColumn_IsAnError(string column, string allowed, string expected)
    {
        var ex = Assert.Throws<DeclarationException>(() =>
            FlagsPlan($$"""{ "check": "listValues", "table": "[dbo].[Flags]", "column": "{{column}}", "allowedValues": {{allowed}} }"""));

        Assert.Contains(ex.Errors, e => e.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void ListValues_NcharPaddingWithIgnoreCase_MatchesTheAllowedValue()
    {
        var summary = TestSchema.Summary();
        summary.Tables.Add(new TableSummary("[dbo].[Kinds]", 10, false, false, 0, [], [],
            [new ColumnSummary("kind", "nchar(4)", null, 4, true, false, null, null)]));
        var plan = TestSchema.Plan("""{ "checks": [{ "check": "listValues", "table": "[dbo].[Kinds]", "column": "kind", "allowedValues": ["ab"], "ignoreCase": true }] }""", summary);
        var database = new FakeDatabase().Answer("GROUP BY", ["AB  ", 3L, null, null], ["CD  ", 1L, null, null]);

        var result = Assert.Single(Assert.Single(plan.DeclaredChecks).Run(TestSchema.Context(plan, database)));

        Assert.Equal(["\"AB\": 3", "\"CD\": 1  (not allowed)"], result.Details);
    }

    [Fact]
    public void ListValues_TypedTrailingSpaceInVarchar_IsNotAllowed()
    {
        var database = new FakeDatabase().Answer("GROUP BY", ["Open", 3L, null, null], ["Open ", 2L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "listValues", "table": "[dbo].[Status]", "column": "Label", "allowedValues": ["Open"] }
            """, database));

        Assert.Equal(["\"Open\": 3", "\"Open \": 2  (not allowed)"], result.Details);
    }

    [Fact]
    public void ValuesMustExistIn_CharValue_IsShownWithoutPadding()
    {
        var database = new FakeDatabase()
            .Answer("\n  AND EXISTS (")
            .Answer("GROUP BY t.[Abbrev]", ["AB   ", 2L, 1L, 2L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "valuesMustExistIn", "table": "[dbo].[Status]", "columns": ["Abbrev"], "otherTable": "[dbo].[Status]", "otherColumns": ["Label"] }
            """, database, "[dbo].[Status].Abbrev"));

        Assert.Contains("  Abbrev \"AB\": 2 rows", result.Details);
    }

    [Fact]
    public void OneValuePerKey_NeverReadsTheValues_AndMasksAPersonKey()
    {
        var database = new FakeDatabase()
            .Answer("DENSE_RANK()", [LoginId, 1L, 1L, 0, null, null, 5], [LoginId, 1L, 1L, 0, null, null, 6], [LoginId, 1L, 2L, 0, null, null, 7])
            .Answer("HAVING", [LoginId, 2L, 1L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "oneValuePerKey", "table": "[dbo].[visitor]", "keyColumns": ["mothraid"], "columns": ["gradYear"] }
            """, database));

        Assert.Equal(
            ["Keys with more than one gradYear value (1):", "  mothraid <letters only, 6 chars>: 3 rows, 2 different gradYear values",
             "    2 rows: the most common value", "    1 row: a different value, id 7"],
            result.Details);
        Assert.Contains("ELSE 0 END, NULL, NULL", database.Queries[1].Sql, StringComparison.Ordinal);
        AssertNeverShown(result, database);
    }

    [Fact]
    public void OneValuePerKey_SafeColumn_ShowsItsValues()
    {
        var database = new FakeDatabase()
            .Answer("DENSE_RANK()", ["00123456", 1L, 1L, 0, 2020, null, 5], ["00123456", 1L, 1L, 0, 2020, null, 6], ["00123456", 1L, 2L, 0, 2021, null, 7])
            .Answer("HAVING", ["00123456", 2L, 1L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "oneValuePerKey", "table": "[dbo].[visitor]", "keyColumns": ["mothraid"], "columns": ["gradYear"] }
            """, database, "[dbo].[visitor].gradYear"));

        Assert.Contains("    1 row: 2021, id 7", result.Details);
        Assert.Contains("ELSE 0 END, t.[gradYear], NULL", database.Queries[1].Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void OneValuePerKey_KeysDifferingOnlyInCase_StayOneKeyAsSqlGroupedThem()
    {
        // SQL found one key (a case-insensitive column), and numbered both spellings' rows as key 1.
        var database = new FakeDatabase()
            .Answer("DENSE_RANK()", ["ABCDEF", 1L, 1L, 0, 2020, null, 5], ["abcdef", 1L, 2L, 0, 2021, null, 6])
            .Answer("HAVING", ["ABCDEF", 2L, 1L, null, null]);

        var result = Assert.Single(Run("""
            { "check": "oneValuePerKey", "table": "[dbo].[visitor]", "keyColumns": ["mothraid"], "columns": ["gradYear"] }
            """, database, "[dbo].[visitor].gradYear"));

        Assert.Contains("  mothraid <letters only, 6 chars>: 2 rows, 2 different gradYear values", result.Details);
        Assert.Single(result.Details, d => d.StartsWith("  mothraid", StringComparison.Ordinal));
        Assert.Contains("DENSE_RANK() OVER (ORDER BY t.[mothraid])", database.Queries[1].Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void SameKeysAs_PersonKeyOnlyOnOneSide_IsShownOnlyAsAShape()
    {
        var database = new FakeDatabase()
            .Answer("FROM [dbo].[visitor] AS a", [LoginId, 1L, null, null], ["00123456", 1L, null, null])
            .Answer("FROM [dbo].[tblPerson] AS a");

        var result = Assert.Single(Run("""
            { "check": "sameKeysAs", "table": "[dbo].[visitor]", "columns": ["mothraid"], "otherTable": "[dbo].[tblPerson]", "otherColumns": ["person_MothraID"] }
            """, database));

        Assert.Equal([["<letters only, 6 chars>"], ["00123456"]], Assert.Single(result.Grids).Rows);
        AssertNeverShown(result, database);
    }

    private static CustomSqlCheck CustomSql(string? occurredAt = "addDate", bool traced = true)
    {
        var visitor = TestSchema.Plan("{}").Schema.FindTable(TestSchema.Visitor)!;
        return new()
        {
            Name = "Visitors without a stay",
            Severity = Severity.Warning,
            Tables = [visitor],
            Sql = "SELECT mothraid, addDate, notes FROM dbo.visitor",
            KeyColumns = ["mothraid"],
            KeySources = [traced ? visitor.FindColumn("mothraid") : null],
            OccurredAtColumn = occurredAt
        };
    }

    private static DataTable CustomRows(Type dateType)
    {
        var table = new DataTable();
        table.Columns.Add("mothraid", typeof(string));
        table.Columns.Add("addDate", dateType);
        table.Columns.Add("notes", typeof(string));
        table.Rows.Add(LoginId, dateType == typeof(DateTime) ? new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Local) : "2020", "free text");
        table.Rows.Add("00123456", dateType == typeof(DateTime) ? new DateTime(2024, 5, 6, 0, 0, 0, DateTimeKind.Local) : "2024", "more text");
        return table;
    }

    [Fact]
    public void CustomSql_ReadsOnlyKeysAndDates_AndMasksKeysThatAreNotSafe()
    {
        var database = new FakeDatabase().AnswerReader("FROM dbo.visitor", () => CustomRows(typeof(DateTime)));
        var plan = TestSchema.Plan("{}");

        var result = Assert.Single(CustomSql().Run(TestSchema.Context(plan, database)));

        Assert.Equal(CheckStatus.ProblemsFound, result.Status);
        Assert.Equal(["<letters only, 6 chars>", "00123456"], result.Examples);
        Assert.Equal(("2020-01", "2024-05"), (result.Earliest, result.Latest));
        Assert.DoesNotContain(result.Details.Concat(result.Examples), d => d.Contains("text", StringComparison.Ordinal));
    }

    [Fact]
    public void CustomSql_KeyNotTracedToAColumn_IsAShapeAndSaysWhy()
    {
        var database = new FakeDatabase().AnswerReader("FROM dbo.visitor", () => CustomRows(typeof(DateTime)));

        var result = Assert.Single(CustomSql(traced: false).Run(TestSchema.Context(TestSchema.Plan("{}"), database)));

        Assert.Equal(["<letters only, 6 chars>", "<digits only, 8 chars>"], result.Examples);
        Assert.StartsWith("mothraid is described by shape", Assert.Single(result.Details), StringComparison.Ordinal);
    }

    [Fact]
    public void CustomSql_DateColumnThatIsNotADate_FailsToRunAndSaysWhy()
    {
        var database = new FakeDatabase().AnswerReader("FROM dbo.visitor", () => CustomRows(typeof(string)));

        var result = Assert.Single(CustomSql().Run(TestSchema.Context(TestSchema.Plan("{}"), database)));

        Assert.Equal(CheckStatus.FailedToRun, result.Status);
        Assert.Equal("Failed to run: addDate is not a date or time, so it cannot date the rows.", result.Message);
    }

    [Fact]
    public void CustomSql_KeyColumnTheQueryDoesNotReturn_FailsToRunAndSaysWhich()
    {
        var database = new FakeDatabase().AnswerReader("FROM dbo.visitor", () => CustomRows(typeof(DateTime)));
        var check = new CustomSqlCheck
        {
            Name = "x",
            Severity = Severity.Warning,
            Tables = CustomSql().Tables,
            Sql = CustomSql().Sql,
            KeyColumns = ["id"],
            KeySources = [null]
        };

        var result = Assert.Single(check.Run(TestSchema.Context(TestSchema.Plan("{}"), database)));

        Assert.Equal("Failed to run: the query returns no column named id.", result.Message);
    }
}
