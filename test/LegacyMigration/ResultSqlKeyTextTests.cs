using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// Row keys and shown values are where table data reaches the report. A value is shown only when its
/// column's origin makes it safe (ColumnRef.Visibility); anything else comes out as a shape, whatever it
/// looks like, and its column is noted for the report's list of masked columns.
/// </summary>
[InvariantCulture]
public class ResultSqlKeyTextTests
{
    private static ColumnRef Column(string table, string column, string json = "{}") =>
        TestSchema.Plan(json).Schema.FindTableOrView(table)!.FindColumn(column)!;

    private static ColumnRef MothraId() => Column(TestSchema.Person, "person_MothraID");

    private static ColumnRef TermCode() => Column(TestSchema.Person, "person_TermCode");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CanShow_NullAndBlank_AreShownInAnyColumn(object? value)
    {
        Assert.True(ResultSql.CanShow(TermCode(), value));
    }

    [Theory]
    [InlineData(202409)]
    [InlineData("123456789")]
    public void CanShow_ColumnWithNoSafeOrigin_MasksEvenNumbers(object value)
    {
        // An SSN is all digits too, so how a value looks never makes it safe.
        Assert.False(ResultSql.CanShow(TermCode(), value));
    }

    [Theory]
    [InlineData("00123456", true)]
    [InlineData(" 00123456 ", true)]
    [InlineData(46957, true)]
    [InlineData("jsmith", false)]
    [InlineData("12a", false)]
    public void CanShow_PersonIdColumn_ShowsOnlyDigits(object value, bool expected)
    {
        Assert.Equal(expected, ResultSql.CanShow(MothraId(), value));
    }

    [Fact]
    public void CanShow_IdentityColumn_IsShown()
    {
        Assert.True(ResultSql.CanShow(Column(TestSchema.Visitor, "id"), 12));
    }

    [Fact]
    public void Columns_PersonColumnFlaggedByTheRules_IsAPersonColumn()
    {
        Assert.True(MothraId().IsPersonColumn);
        Assert.False(TermCode().IsPersonColumn);
    }

    [Fact]
    public void KeyText_MasksEachColumnByItsOwnVisibility()
    {
        Assert.Equal("(<letters only, 6 chars>, <digits only, 6 chars>)", ResultSql.KeyText([MothraId(), TermCode()], ["jsmith  ", 202409], 0));
        Assert.Equal("(00123456, <digits only, 6 chars>)", ResultSql.KeyText([MothraId(), TermCode()], ["00123456", 202409], 0));
    }

    [Fact]
    public void KeyText_SafeColumn_IsShown()
    {
        var termCode = Column(TestSchema.Person, "person_TermCode",
            """{ "safeColumns": [{ "table": "[dbo].[tblPerson]", "column": "person_TermCode", "reason": "Term codes." }] }""");

        Assert.Equal("202409", ResultSql.KeyText([termCode], ["ignored", 202409], 1));
    }

    [Fact]
    public void KeyText_TextInAColumnTheRulesDoNotFlag_IsMaskedToo()
    {
        Assert.Equal("<letters only, 6 chars>", ResultSql.KeyText([Column(TestSchema.Status, "Label")], ["jsmith"], 0));
    }

    [Fact]
    public void Masking_NotesTheColumnForTheReport()
    {
        var termCode = TermCode();
        var id = Column(TestSchema.Visitor, "id");

        ResultSql.KeyCellText(termCode, 202409);
        ResultSql.KeyCellText(id, 12);

        Assert.True(termCode.WasMasked);
        Assert.False(id.WasMasked);
    }

    [Fact]
    public void MaskedColumn_PersonIdColumn_IsMarkedWithTheShapesOfItsDistinctHiddenValues()
    {
        var mothraId = MothraId();

        ResultSql.KeyCellText(mothraId, "jsmith  ");
        ResultSql.KeyCellText(mothraId, "jsmith");
        ResultSql.KeyCellText(mothraId, "ab12cd");
        ResultSql.KeyCellText(mothraId, "00123456");

        Assert.Equal(new MaskedColumn(TestSchema.Person, "person_MothraID", true, "letters and digits, 6 chars (1 distinct value); letters only, 6 chars (1 distinct value)"),
            MaskedColumn.Of(mothraId));
    }

    [Fact]
    public void MaskedColumn_OtherColumn_IsDeclarableWithNoShapes()
    {
        var termCode = TermCode();

        ResultSql.KeyCellText(termCode, 202409);

        Assert.Equal(new MaskedColumn(TestSchema.Person, "person_TermCode"), MaskedColumn.Of(termCode));
    }

    [Fact]
    public void RangeText_IsMonthsOnly_AndOneMonthWhenBothFallInIt()
    {
        var first = new DateTime(1990, 3, 14, 0, 0, 0, DateTimeKind.Local);
        var later = new DateTime(1990, 3, 28, 0, 0, 0, DateTimeKind.Local);

        Assert.Equal("1990-03", ResultSql.RangeText(first, later));
        Assert.Equal("1990-03 to 1991-07", ResultSql.RangeText(first, new DateTime(1991, 7, 1, 0, 0, 0, DateTimeKind.Local)));
    }

    [Fact]
    public void MaskedColumn_TextLookupColumn_IsNotAPersonId()
    {
        var detail = Column(TestSchema.ChangeLog, "detail", """
            { "checks": [{ "check": "valuesMustExistIn", "table": "[audit].[ChangeLog]", "columns": ["detail"],
                           "otherTable": "[dbo].[visitor]", "otherColumns": ["id"] }] }
            """);

        ResultSql.KeyCellText(detail, "jsmith");

        Assert.Equal(new MaskedColumn(TestSchema.ChangeLog, "detail"), MaskedColumn.Of(detail));
    }

    [Fact]
    public void NamedKeyText_NamesEachColumnAndStillMasks()
    {
        Assert.Equal("person_MothraID <letters only, 6 chars>, person_TermCode <digits only, 6 chars>",
            ResultSql.NamedKeyText([MothraId(), TermCode()], ["jsmith", 202409], 0));
    }

    [Theory]
    [InlineData(null, "<null>")]
    [InlineData("   ", "<blank>")]
    [InlineData(" Vice", "\" Vice\"")]
    [InlineData(42, "42")]
    public void ShowValue_QuotesTextAndNamesNullAndBlank(object? value, string expected)
    {
        Assert.Equal(expected, ResultSql.ShowValue(value));
    }

    [Fact]
    public void ShowValue_PaddedColumn_DropsOnlyThePadding()
    {
        const string json = """
            { "safeColumns": [{ "table": "[dbo].[Status]", "column": "Abbrev", "reason": "Codes." },
                              { "table": "[dbo].[Status]", "column": "Label", "reason": "Codes." }] }
            """;

        Assert.Equal("\"SA\"", ResultSql.ShowValue(Column(TestSchema.Status, "Abbrev", json), "SA   "));
        Assert.Equal("\"SA  \"", ResultSql.ShowValue(Column(TestSchema.Status, "Label", json), "SA  "));
    }

    [Fact]
    public void ShowValue_PersonColumnHoldingALoginId_IsMaskedByShape()
    {
        Assert.Equal("<letters only, 6 chars>", ResultSql.ShowValue(MothraId(), "jsmith"));
        Assert.Equal("\"00123456\"", ResultSql.ShowValue(MothraId(), "00123456"));
    }
}
