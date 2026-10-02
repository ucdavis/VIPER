using Viper.Areas.Reports.Engine;

namespace Viper.test.Reports;

public sealed class ReportValueFormatterTests
{
    private static ReportColumnMetadata Column(ReportColumnFormat format, int? decimals = null)
    {
        return new ReportColumnMetadata("value", "Value", format, ReportAlignment.Left, decimals);
    }

    [Fact]
    public void Format_NonNumericValues()
    {
        ReportColumnMetadata text = Column(ReportColumnFormat.Text);

        Assert.Equal(string.Empty, ReportValueFormatter.Format(null, text));
        Assert.Equal("Ada", ReportValueFormatter.Format("Ada", text));
        Assert.Equal("07/01/2026", ReportValueFormatter.Format(new DateOnly(2026, 7, 1), text));
        Assert.Equal("07/01/2026", ReportValueFormatter.Format(new DateTime(2026, 7, 1, 13, 30, 0, DateTimeKind.Local), text));
        Assert.Equal("APC, VME", ReportValueFormatter.Format(new[] { "APC", "VME" }, Column(ReportColumnFormat.List)));
        Assert.Equal("True", ReportValueFormatter.Format(true, text));
    }

    [Fact]
    public void Format_NumbersInTextColumnsKeepTheirDigits()
    {
        Assert.Equal("1234567", ReportValueFormatter.Format(1_234_567, Column(ReportColumnFormat.Text)));
    }

    [Theory]
    [InlineData(ReportColumnFormat.Number, 0, "1,235")]
    [InlineData(ReportColumnFormat.Number, 1, "1,234.6")]
    [InlineData(ReportColumnFormat.Currency, 2, "$1,234.56")]
    public void Format_NumericColumns(ReportColumnFormat format, int decimals, string expected)
    {
        Assert.Equal(expected, ReportValueFormatter.Format(1234.56m, Column(format, decimals)));
    }

    [Fact]
    public void Format_PercentColumnTreatsValuesAsFractions()
    {
        string formatted = ReportValueFormatter.Format(0.25m, Column(ReportColumnFormat.Percent));

        Assert.StartsWith("25", formatted, StringComparison.Ordinal);
        Assert.EndsWith("%", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_NumericColumnWithoutDecimals_UsesWholeNumbers()
    {
        Assert.Equal("61", ReportValueFormatter.Format(61, Column(ReportColumnFormat.Number)));
    }

    [Fact]
    public void Format_NonNumberInNumericColumn_FallsBackToText()
    {
        Assert.Equal("n/a", ReportValueFormatter.Format("n/a", Column(ReportColumnFormat.Number)));
        Assert.Equal("True", ReportValueFormatter.Format(true, Column(ReportColumnFormat.Number)));
    }

    [Fact]
    public void Format_NullColumn_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ReportValueFormatter.Format("x", null!));
    }

    [Theory]
    [InlineData(ReportColumnFormat.Number, true)]
    [InlineData(ReportColumnFormat.Currency, true)]
    [InlineData(ReportColumnFormat.Percent, true)]
    [InlineData(ReportColumnFormat.Text, false)]
    [InlineData(ReportColumnFormat.Date, false)]
    [InlineData(ReportColumnFormat.List, false)]
    public void IsNumeric_OnlyNumberCurrencyAndPercent(ReportColumnFormat format, bool expected)
    {
        Assert.Equal(expected, ReportValueFormatter.IsNumeric(format));
    }

    [Fact]
    public void TryGetDecimal_AcceptsNumericTypes()
    {
        Assert.True(ReportValueFormatter.TryGetDecimal(1.5m, out decimal fromDecimal));
        Assert.True(ReportValueFormatter.TryGetDecimal(7, out decimal fromInt));
        Assert.True(ReportValueFormatter.TryGetDecimal(8L, out decimal fromLong));
        Assert.True(ReportValueFormatter.TryGetDecimal(2.5d, out decimal fromDouble));

        Assert.Equal([1.5m, 7m, 8m, 2.5m], new[] { fromDecimal, fromInt, fromLong, fromDouble });
    }

    [Fact]
    public void TryGetDecimal_RejectsNonNumbersAndOutOfRangeDoubles()
    {
        Assert.False(ReportValueFormatter.TryGetDecimal(null, out _));
        Assert.False(ReportValueFormatter.TryGetDecimal("12", out _));
        Assert.False(ReportValueFormatter.TryGetDecimal(double.NaN, out _));
        Assert.False(ReportValueFormatter.TryGetDecimal(1e30, out _));
    }

    [Fact]
    public void FlagText_JoinsDistinctLabels()
    {
        var row = new ReportRowResult(1, new Dictionary<string, object?>(),
        [
            new ReportFlag(ReportFlagKind.Highlight, ReportTone.Warning, "60+", null),
            new ReportFlag(ReportFlagKind.Badge, ReportTone.Muted, "Emeritus", "status"),
            new ReportFlag(ReportFlagKind.Badge, ReportTone.Warning, "60+", "age"),
        ]);

        Assert.Equal("60+; Emeritus", ReportValueFormatter.FlagText(row));
        Assert.Throws<ArgumentNullException>(() => ReportValueFormatter.FlagText(null!));
    }
}
