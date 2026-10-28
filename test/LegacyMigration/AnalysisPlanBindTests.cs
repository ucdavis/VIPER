using System.Text.Json;
using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// Binding holds analysis.json to the schema export before anything connects: a name that is not in the
/// export can never reach a query, and every problem is reported in one pass.
/// </summary>
[InvariantCulture]
public class AnalysisPlanBindTests
{
    [Fact]
    public void Bind_NoDeclarations_ChecksEverything()
    {
        var plan = TestSchema.Plan("{}");

        Assert.All(plan.Schema.Tables, t => Assert.True(t.GetsAutomaticChecks));
        Assert.Empty(plan.DeclaredChecks);
    }

    [Fact]
    public void Read_UnknownField_IsRefused()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Declarations>("""{ "exlude": [] }""", Declarations.ReadOptions));
    }

    [Fact]
    public void Read_CommentsAndTrailingCommas_AreAllowed()
    {
        var plan = TestSchema.Plan("""
            // A comment
            { "exclude": ["[dbo].[Status]",], }
            """);

        Assert.True(plan.Schema.IsExcluded(TestSchema.Status));
    }

    [Fact]
    public void Bind_SeveralProblems_AreAllReportedTogether()
    {
        var errors = TestSchema.Errors("""
            { "exclude": ["[dbo].[missing]"], "dateColumns": { "[dbo].[visitor]": "notes" }, "checks": [{ "check": "nope" }] }
            """);

        Assert.Equal(3, errors.Count);
    }

    [Fact]
    public void Bind_NameInTheWrongCase_SuggestsTheRightOne()
    {
        var errors = TestSchema.Errors("""{ "exclude": ["[dbo].[Visitor]"] }""");

        Assert.Contains("Did you mean [dbo].[visitor]?", Assert.Single(errors), StringComparison.Ordinal);
    }

    [Fact]
    public void Bind_ColumnInTheWrongCase_SuggestsTheRightOne()
    {
        var errors = TestSchema.CheckErrors("""{ "check": "notBlank", "table": "[dbo].[visitor]", "columns": ["MothraID"] }""");

        Assert.Contains("Did you mean mothraid?", Assert.Single(errors), StringComparison.Ordinal);
    }

    [Fact]
    public void Bind_IncludeSchema_LimitsTheAutomaticChecksToIt()
    {
        var plan = TestSchema.Plan("""{ "include": ["[audit]"] }""");

        Assert.Equal([TestSchema.ChangeLog], plan.Schema.Tables.Where(t => t.GetsAutomaticChecks).Select(t => t.Name).ToList());
    }

    [Theory]
    [InlineData("""{ "include": [] }""", "leave \"include\" out")]
    [InlineData("""{ "include": ["[nope]"] }""", "is not a schema")]
    [InlineData("""{ "include": ["[dbo].[visitor]", "[dbo].[visitor]"] }""", "already listed")]
    public void Bind_BadInclude_IsAnError(string json, string expected)
    {
        Assert.Contains(TestSchema.Errors(json), e => e.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void Bind_DateColumnThatIsNotADate_IsAnError()
    {
        var errors = TestSchema.Errors("""{ "dateColumns": { "[dbo].[visitor]": "notes" } }""");

        Assert.Contains("not a date column", Assert.Single(errors), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{ "automaticCheck": "Nope", "table": "[dbo].[visitor]", "skip": true, "reason": "r" }""", "is not an automatic check")]
    [InlineData("""{ "automaticCheck": "Deprecated types", "table": "[dbo].[visitor]", "skip": true, "severity": "info", "reason": "r" }""", "exactly one of")]
    [InlineData("""{ "automaticCheck": "Deprecated types", "table": "[dbo].[visitor]", "skip": true, "reason": " " }""", "\"reason\" must say why")]
    public void Bind_BadAutomaticCheckOverride_IsAnError(string entry, string expected)
    {
        Assert.Contains(TestSchema.Errors($$"""{ "automaticChecks": [{{entry}}] }"""), e => e.Contains(expected, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Related columns with different types")]
    [InlineData("Tables that look like copies")]
    [InlineData("Tables with no recent rows")]
    [InlineData("Objects that change data")]
    [InlineData("Rules the schema enforces")]
    public void Bind_OverrideOfACheckAcrossTables_IsAnError(string check)
    {
        var errors = TestSchema.Errors($$"""
            { "automaticChecks": [{ "automaticCheck": "{{check}}", "table": "[dbo].[visitor]", "skip": true, "reason": "r" }] }
            """);

        Assert.Equal($"automaticChecks[0]: \"{check}\" reports one result across tables, so it cannot be skipped or changed for one table.",
            Assert.Single(errors));
    }

    [Fact]
    public void Registry_ChecksAcrossTables_AreTheFiveTheReferenceNames()
    {
        Assert.Equal(
            ["Objects that change data", "Related columns with different types", "Rules the schema enforces", "Tables that look like copies",
             "Tables with no recent rows"],
            CheckRegistry.Automatic.Where(c => !c.AcceptsTableOverrides).Select(c => c.Name).Order(StringComparer.Ordinal).ToList());
    }

    [Fact]
    public void Bind_OverrideOutsideInclude_IsAnError()
    {
        var errors = TestSchema.Errors("""
            { "include": ["[audit]"],
              "automaticChecks": [{ "automaticCheck": "Deprecated types", "table": "[dbo].[visitor]", "skip": true, "reason": "r" }] }
            """);

        Assert.Contains("outside \"include\"", Assert.Single(errors), StringComparison.Ordinal);
    }

    [Fact]
    public void Bind_Override_IsFoundForItsCheckAndTable()
    {
        var plan = TestSchema.Plan("""
            { "automaticChecks": [{ "automaticCheck": "Deprecated types", "table": "[dbo].[visitor]", "severity": "warning", "reason": "r" }] }
            """);

        Assert.Equal(Severity.Warning, plan.FindOverride("Deprecated types", TestSchema.Visitor)?.Severity);
        Assert.Null(plan.FindOverride("Deprecated types", TestSchema.Status));
    }

    [Theory]
    [InlineData("""{ "table": "[dbo].[visitor]", "column": "startWeekID", "reason": "" }""", "\"reason\" must say why")]
    [InlineData("""{ "table": "[dbo].[visitor]", "column": "nope", "reason": "r" }""", "nope is not a column")]
    [InlineData("""{ "table": "[dbo].[tblPerson]", "column": "person_LastName", "reason": "r" }""", "its name has \"last\"")]
    [InlineData("""{ "table": "[dbo].[tblPerson]", "column": "person_MothraID", "reason": "r" }""", "flagged as a person column")]
    [InlineData("""{ "table": "[audit].[ChangeLog]", "column": "ModBy", "reason": "r" }""", "flagged as a person column")]
    public void Bind_BadSafeColumn_IsAnError(string entry, string expected)
    {
        Assert.Contains(TestSchema.Errors($$"""{ "safeColumns": [{{entry}}] }"""), e => e.Contains(expected, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("GradeMode", "grade")]
    [InlineData("testScores", "scores")]
    [InlineData("cum_GPA", "gpa")]
    [InlineData("AccountBalance", "balance")]
    [InlineData("Age", "age")]
    [InlineData("staffNotes", "notes")]
    [InlineData("EthnicityCode", "ethnicity")]
    [InlineData("disabilityFlag", "disability")]
    [InlineData("healthInsurance", "health")]
    [InlineData("lastModified", "last")]
    [InlineData("pageNumber", null)]
    [InlineData("upgradeDate", null)]
    [InlineData("GradYear", null)]
    [InlineData("SalaryAmount", null)]
    [InlineData("PayRate", null)]
    public void PersonalWord_MatchesWholeWordsAndParts(string columnName, string? expected)
    {
        Assert.Equal(expected, SchemaBinder.PersonalWord(columnName));
    }

    [Fact]
    public void Bind_SafeColumnListedTwice_IsAnError()
    {
        const string entry = """{ "table": "[dbo].[visitor]", "column": "startWeekID", "reason": "r" }""";

        Assert.Contains(TestSchema.Errors($$"""{ "safeColumns": [{{entry}}, {{entry}}] }"""), e => e.Contains("already listed", StringComparison.Ordinal));
    }

    [Fact]
    public void Bind_PersonalLookingSafeColumnWithLooksPersonal_IsShown()
    {
        var plan = TestSchema.Plan("""
            { "safeColumns": [{ "table": "[dbo].[tblPerson]", "column": "person_LastName", "reason": "Placeholder text only.",
                                "looksPersonal": "Holds a fixed placeholder, never a name." }] }
            """);

        Assert.Equal(ValueVisibility.Shown, plan.Schema.FindTable(TestSchema.Person)!.FindColumn("person_LastName")!.Visibility);
    }

    [Theory]
    [InlineData("""{ "check": "listValues", "table": "[dbo].[tblPerson]", "column": "person_LastName" }""", true)]
    [InlineData("""{ "check": "listValues", "table": "[dbo].[tblPerson]", "column": "person_LastName", "looksPersonal": "Placeholders only." }""", false)]
    [InlineData("""{ "check": "listValues", "table": "[dbo].[Status]", "column": "Label" }""", false)]
    public void Bind_ListValuesOnAPersonalLookingColumn_NeedsLooksPersonal(string check, bool isError)
    {
        Assert.Equal(isError, TestSchema.CheckErrors(check).Any(e => e.Contains("may hold personal values", StringComparison.Ordinal)));
    }

    [Fact]
    public void Bind_SafeColumnsAndDeclaredLookups_ShowTheirColumnsWithAReason()
    {
        var plan = TestSchema.Plan("""
            { "safeColumns": [{ "table": "[dbo].[visitor]", "column": "endWeekID", "reason": "Week numbers, not personal." }],
              "checks": [{ "check": "valuesMustExistIn", "table": "[dbo].[visitor]", "columns": ["startWeekID"],
                           "otherTable": "[dbo].[visitor]", "otherColumns": ["id"] },
                         { "check": "valuesMustExistIn", "table": "[dbo].[visitor]", "columns": ["gradYear"],
                           "otherTable": "[dbo].[Status]", "otherColumns": ["GradYear"] }] }
            """);
        var visitor = plan.Schema.FindTable(TestSchema.Visitor)!;

        Assert.Equal("Week numbers, not personal.", visitor.FindColumn("endWeekID")!.ShownReason);
        Assert.Contains("declared lookup", visitor.FindColumn("startWeekID")!.ShownReason, StringComparison.Ordinal);
        // A lookup into a column the database did not make passes nothing on.
        Assert.Equal(ValueVisibility.Masked, visitor.FindColumn("gradYear")!.Visibility);
    }

    [Fact]
    public void Bind_DeclaredLookupFromATextColumn_ShowsOnlyItsDigits()
    {
        var plan = TestSchema.Plan("""
            { "checks": [{ "check": "valuesMustExistIn", "table": "[audit].[ChangeLog]", "columns": ["detail"],
                           "otherTable": "[dbo].[visitor]", "otherColumns": ["id"] }] }
            """);
        var detail = plan.Schema.FindTable(TestSchema.ChangeLog)!.FindColumn("detail")!;

        Assert.Equal(ValueVisibility.DigitsOnly, detail.Visibility);
        Assert.Contains("declared lookup", detail.ShownReason, StringComparison.Ordinal);
        Assert.False(detail.IsPersonId);
    }

    [Fact]
    public void Bind_SchemaOrigins_SetEachColumnsVisibility()
    {
        var plan = TestSchema.Plan("{}", ChainSchema());
        ValueVisibility Of(string table, string column) => plan.Schema.FindTable(table)!.FindColumn(column)!.Visibility;

        Assert.Equal(ValueVisibility.Shown, Of("[dbo].[A]", "id"));
        Assert.Equal(ValueVisibility.Shown, Of("[dbo].[A]", "version"));
        Assert.Equal(ValueVisibility.Shown, Of("[dbo].[B]", "aId"));
        Assert.Equal(ValueVisibility.Shown, Of("[dbo].[C]", "bId"));
        Assert.Equal(ValueVisibility.Masked, Of("[dbo].[C]", "code"));
        Assert.Equal(ValueVisibility.DigitsOnly, Of("[dbo].[C]", "mothraId"));
    }

    /// <summary>
    /// A has an identity and a rowversion; B is keyed by a foreign key to A's identity, and C by a foreign key
    /// to B's, so C.bId is shown through the chain.
    /// </summary>
    private static SchemaSummary ChainSchema()
    {
        static ColumnSummary Column(string name, string type, bool identity = false) =>
            new(name, type, null, null, false, false, null, identity ? new IdentitySummary("1", "1", "10") : null);
        List<TableSummary> tables =
        [
            new("[dbo].[A]", 10, true, true, 0, ["id"], [], [Column("id", "int", identity: true), Column("version", "timestamp")]),
            new("[dbo].[B]", 10, true, true, 0, ["aId"], [], [Column("aId", "int")]),
            new("[dbo].[C]", 10, false, false, 0, [], [], [Column("bId", "int"), Column("code", "varchar(10)"), Column("mothraId", "char(8)")])
        ];
        List<ForeignKeySummary> foreignKeys =
        [
            new("FK_B_A", "[dbo].[B]", ["aId"], "[dbo].[A]", ["id"], false, false, false, "NO_ACTION", "NO_ACTION"),
            new("FK_C_B", "[dbo].[C]", ["bId"], "[dbo].[B]", ["aId"], true, false, false, "NO_ACTION", "NO_ACTION")
        ];
        var withoutPeople = new SchemaSummary { Tables = tables, ForeignKeys = foreignKeys };
        return new SchemaSummary
        {
            Database = "Legacy",
            CompatibilityLevel = 130,
            Tables = tables,
            ForeignKeys = foreignKeys,
            PersonIdColumns = PersonIdRules.Find(withoutPeople)
        };
    }

    [Theory]
    [InlineData("""{ "table": "[dbo].[visitor]" }""", "needs a \"check\" field")]
    [InlineData("""{ "check": "nope" }""", "is not a known check")]
    [InlineData("""{ "check": "notBlank", "table": "[dbo].[visitor]", "columns": ["notes"], "colums": [] }""", "colums")]
    public void Bind_MalformedCheck_IsAnError(string check, string expected)
    {
        Assert.Contains(TestSchema.CheckErrors(check), e => e.Contains(expected, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "check": "notBlank", "table": "[dbo].[visitor]", "columns": ["notes", "notes"] }""", "more than once")]
    [InlineData("""{ "check": "notBlank", "table": "[dbo].[visitor]", "columns": [] }""", "needs at least one column")]
    [InlineData("""{ "check": "valuesMustExistIn", "table": "[dbo].[visitor]", "columns": ["gradYear", "startWeekID"], "otherTable": "[dbo].[Status]", "otherColumns": ["GradYear"] }""", "need the same number")]
    [InlineData("""{ "check": "sameKeysAs", "table": "[dbo].[visitor]", "columns": ["gradYear"], "otherTable": "[dbo].[Status]", "otherColumns": [] }""", "needs at least one column")]
    [InlineData("""{ "check": "startBeforeEnd", "table": "[dbo].[visitor]", "startColumn": "startWeekID", "endColumn": "addDate" }""", "addDate is datetime")]
    [InlineData("""{ "check": "withinRange", "table": "[dbo].[visitor]", "column": "notes", "min": 0 }""", "needs a number or date column")]
    [InlineData("""{ "check": "withinRange", "table": "[dbo].[visitor]", "column": "startWeekID" }""", "give \"min\", \"max\", or both")]
    [InlineData("""{ "check": "withinRange", "table": "[dbo].[visitor]", "column": "startWeekID", "min": 10, "max": 1 }""", "greater than \"max\"")]
    [InlineData("""{ "check": "withinRange", "table": "[dbo].[visitor]", "column": "addDate", "min": "01/02/2020" }""", "written as \"2000-01-01\"")]
    [InlineData("""{ "check": "canConvertTo", "table": "[dbo].[visitor]", "column": "notes", "type": "decimal" }""", "is not supported")]
    [InlineData("""{ "check": "canConvertTo", "table": "[dbo].[visitor]", "column": "notes", "type": "decimal(5, 6)" }""", "scale 0 to the precision")]
    [InlineData("""{ "check": "canConvertTo", "table": "[dbo].[visitor]", "column": "gradYear", "type": "int" }""", "checks text columns")]
    [InlineData("""{ "check": "listValues", "table": "[dbo].[visitor]", "column": "addDate" }""", "needs a text, number, or bit column")]
    [InlineData("""{ "check": "matchesOtherTable", "table": "[dbo].[visitor]", "column": "gradYear", "otherTable": "[dbo].[Status]", "joinOn": [], "otherColumn": "GradYear" }""", "needs at least one")]
    [InlineData("""{ "check": "oneValuePerKey", "table": "[dbo].[visitor]", "keyColumns": ["mothraid"], "columns": ["mothraid"] }""", "both \"keyColumns\" and \"columns\"")]
    [InlineData("""{ "check": "identifiesOnePerson", "table": "[dbo].[visitor]", "column": "mothraid", "idType": "Mothra", "lookIn": "VIPER.users.Person" }""", "\"idType\" \"Mothra\" is not known")]
    [InlineData("""{ "check": "identifiesOnePerson", "table": "[dbo].[visitor]", "column": "mothraid", "idType": "MothraId", "lookIn": "users.Person" }""", "\"lookIn\" \"users.Person\" is not known")]
    [InlineData("""{ "check": "identifiesOnePerson", "table": "[dbo].[visitor]", "column": "mothraid", "idType": "Pidm", "lookIn": "VIPER.users.Person" }""", "has no Pidm column")]
    [InlineData("""{ "check": "identifiesOnePerson", "table": "[dbo].[visitor]", "column": "addDate", "idType": "MothraId", "lookIn": "VIPER.users.Person" }""", "person IDs are text or numbers")]
    public void Bind_DeclaredCheckBrokenForItsKind_IsAnError(string check, string expected)
    {
        Assert.Contains(TestSchema.CheckErrors(check), e => e.Contains(expected, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "check": "canConvertTo", "table": "[dbo].[visitor]", "column": "notes", "type": null }""")]
    [InlineData("""{ "check": "matchesPattern", "table": "[dbo].[visitor]", "column": "notes", "patterns": null }""")]
    [InlineData("""{ "check": "identifiesOnePerson", "table": "[dbo].[visitor]", "column": "mothraid", "idType": null, "lookIn": "VIPER.users.Person" }""")]
    [InlineData("""{ "check": "identifiesOnePerson", "table": "[dbo].[visitor]", "column": "mothraid", "idType": "MothraId", "lookIn": null }""")]
    [InlineData("""{ "check": "notBlank", "table": "[dbo].[visitor]", "columns": null }""")]
    public void Bind_NullInARequiredField_IsAnError(string check)
    {
        Assert.Contains(TestSchema.CheckErrors(check), e => e.StartsWith("checks[0]", StringComparison.Ordinal) && e.Contains("null", StringComparison.Ordinal));
    }

    [Fact]
    public void Bind_NullJoinPair_IsAnError()
    {
        var errors = TestSchema.CheckErrors(
            """{ "check": "matchesOtherTable", "table": "[dbo].[visitor]", "column": "gradYear", "otherTable": "[dbo].[Status]", "joinOn": [null], "otherColumn": "GradYear" }""");

        Assert.Contains(errors, e => e.Contains("\"joinOn\" has a null entry", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "automaticChecks": [null] }""", "automaticChecks[0]: is null")]
    [InlineData("""{ "safeColumns": [null] }""", "safeColumns[0]: is null")]
    public void Bind_NullListEntry_IsAnError(string json, string expected)
    {
        Assert.Contains(TestSchema.Errors(json), e => e.StartsWith(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void Bind_NullAllowedValueForANumberColumn_IsAnError()
    {
        var errors = TestSchema.CheckErrors(
            """{ "check": "listValues", "table": "[dbo].[visitor]", "column": "gradYear", "allowedValues": ["2020", null] }""");

        Assert.Contains(errors, e => e.Contains("\"allowedValues\" has null", StringComparison.Ordinal));
    }

    [Fact]
    public void Bind_NullAllowedValueForATextColumn_IsDropped()
    {
        var plan = TestSchema.Plan(
            """{ "checks": [{ "check": "listValues", "table": "[dbo].[Status]", "column": "Label", "allowedValues": ["Open", null] }] }""");

        Assert.Equal(["Open"], Assert.IsType<ListValuesCheck>(Assert.Single(plan.DeclaredChecks)).AllowedValues);
    }

    [Theory]
    [InlineData("exclude")]
    [InlineData("dateColumns")]
    [InlineData("automaticChecks")]
    [InlineData("safeColumns")]
    [InlineData("checks")]
    public void Load_NullTopLevelList_IsADeclarationError(string field)
    {
        var folder = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Join(folder.FullName, "analysis.json");
            File.WriteAllText(path, $$"""{ "{{field}}": null }""");

            var ex = Assert.Throws<DeclarationException>(() => Declarations.Load(path));

            Assert.Contains(field, Assert.Single(ex.Errors), StringComparison.Ordinal);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public void Bind_EachDeclaredCheckKind_BindsWhenWritten()
    {
        var plan = TestSchema.Plan("""
            { "checks": [
                { "check": "valuesMustExistIn", "table": "[dbo].[visitor]", "columns": ["gradYear"], "otherTable": "[dbo].[Status]", "otherColumns": ["GradYear"] },
                { "check": "noDuplicates", "table": "[dbo].[visitor]", "columns": ["mothraid", "startWeekID"], "ignoreSurroundingSpaces": true },
                { "check": "notBlank", "table": "[dbo].[visitor]", "columns": ["mothraid", "notes"] },
                { "check": "startBeforeEnd", "table": "[dbo].[visitor]", "startColumn": "startWeekID", "endColumn": "endWeekID" },
                { "check": "withinRange", "table": "[dbo].[visitor]", "column": "addDate", "min": "2000-01-01" },
                { "check": "canConvertTo", "table": "[dbo].[visitor]", "column": "mothraid", "type": "decimal(10, 2)" },
                { "check": "matchesOtherTable", "table": "[dbo].[visitor]", "column": "gradYear", "otherTable": "[dbo].[Status]",
                  "joinOn": [{ "column": "gradYear", "otherColumn": "GradYear" }], "otherColumn": "GradYear" },
                { "check": "listValues", "table": "[dbo].[Status]", "column": "Label", "allowedValues": ["Open", "Closed"], "ignoreCase": true },
                { "check": "oneValuePerKey", "table": "[dbo].[visitor]", "keyColumns": ["mothraid"], "columns": ["gradYear"] },
                { "check": "sameKeysAs", "table": "[dbo].[visitor]", "columns": ["gradYear"], "otherTable": "[dbo].[Status]", "otherColumns": ["GradYear"] },
                { "check": "identifiesOnePerson", "table": "[dbo].[vStudents]", "column": "mothraId", "idType": "MothraId", "lookIn": "VIPER.users.Person" },
                { "check": "matchesPattern", "table": "[dbo].[Status]", "column": "Label", "patterns": ["[A-Z]%"] },
                { "check": "doesNotMatchPattern", "table": "[dbo].[visitor]", "column": "notes", "patterns": ["%test%"] }
            ] }
            """);

        Assert.Equal(13, plan.DeclaredChecks.Count);
        Assert.All(plan.DeclaredChecks, c => Assert.Equal(Severity.Warning, c.Severity));
    }

    [Fact]
    public void Bind_ListValuesWithoutAllowedValues_DefaultsToInfo()
    {
        var plan = TestSchema.Plan("""{ "checks": [{ "check": "listValues", "table": "[dbo].[Status]", "column": "Label" }] }""");

        Assert.Equal(Severity.Info, Assert.Single(plan.DeclaredChecks).Severity);
    }

    [Fact]
    public void Bind_DeclaredSeverity_ReplacesTheDefault()
    {
        var plan = TestSchema.Plan("""
            { "checks": [{ "check": "notBlank", "table": "[dbo].[visitor]", "columns": ["notes"], "severity": "critical" }] }
            """);

        Assert.Equal(Severity.Critical, Assert.Single(plan.DeclaredChecks).Severity);
    }
}
