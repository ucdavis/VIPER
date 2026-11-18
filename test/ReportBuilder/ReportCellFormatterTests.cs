using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>Values as text, the way the PDF download and filter headings show them.</summary>
public sealed class ReportCellFormatterTests
{
    private static readonly DateTime Midnight = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Local);
    private static readonly DateTime Afternoon = new(2026, 3, 1, 14, 5, 0, DateTimeKind.Local);

    [Fact]
    public void Null_IsEmpty()
    {
        Assert.Equal(string.Empty, ReportCellFormatter.Text(null, ReportColumnFormat.Number));
    }

    [Theory]
    [InlineData(ReportColumnFormat.Text, "1,234.5")]
    [InlineData(ReportColumnFormat.Number, "1,234.5")]
    [InlineData(ReportColumnFormat.Currency, "$1,234.50")]
    [InlineData(ReportColumnFormat.Percent, "123,450%")]
    public void Numbers_FollowTheColumnFormat(ReportColumnFormat format, string expected)
    {
        Assert.Equal(expected, ReportCellFormatter.Text(1234.5m, format));
    }

    [Fact]
    public void Percent_IsAFraction()
    {
        Assert.Equal("25.5%", ReportCellFormatter.Text(0.255, ReportColumnFormat.Percent));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(7L)]
    [InlineData((short)7)]
    [InlineData((byte)7)]
    [InlineData(7.0)]
    [InlineData(7f)]
    public void EveryNumberType_IsANumber(object value)
    {
        Assert.Equal("7", ReportCellFormatter.Text(value, ReportColumnFormat.Number));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(1e30)]
    public void DoublesThatDontFitADecimal_AreNotNumbers(double value)
    {
        Assert.False(ReportCellFormatter.TryGetNumber(value, out _));
    }

    [Fact]
    public void Dates_ShowTheTimeOnlyWhenTheyHaveOneOrTheColumnAsks()
    {
        Assert.Equal("03/01/2026", ReportCellFormatter.Text(Midnight, ReportColumnFormat.Text));
        Assert.Equal("03/01/2026 2:05 PM", ReportCellFormatter.Text(Afternoon, ReportColumnFormat.Text));
        Assert.Equal("03/01/2026", ReportCellFormatter.Text(Afternoon, ReportColumnFormat.Date));
        Assert.Equal("03/01/2026 12:00 AM", ReportCellFormatter.Text(Midnight, ReportColumnFormat.DateTime));
        Assert.Equal("03/01/2026", ReportCellFormatter.Text(new DateTimeOffset(Midnight), ReportColumnFormat.Date));
        Assert.Equal("03/01/2026", ReportCellFormatter.Text(new DateOnly(2026, 3, 1), ReportColumnFormat.Text));
    }

    [Theory]
    [InlineData(true, "Yes")]
    [InlineData(false, "No")]
    [InlineData(1, "Yes")]
    [InlineData(0, "No")]
    [InlineData(" y ", "Yes")]
    [InlineData("TRUE", "Yes")]
    [InlineData("n", "No")]
    [InlineData("0", "No")]
    [InlineData("Maybe", "Maybe")]
    public void YesNo_ReadsFlagsNumbersAndWords(object value, string expected)
    {
        Assert.Equal(expected, ReportCellFormatter.Text(value, ReportColumnFormat.YesNo));
    }

    [Fact]
    public void OtherValues_AreShownPlainly()
    {
        Guid id = Guid.Parse("8a6f1d2e-0b7c-4a59-9d3e-1f2a3b4c5d6e");

        Assert.Equal("Yes", ReportCellFormatter.Text(true, ReportColumnFormat.Text));
        Assert.Equal("No", ReportCellFormatter.Text(false, ReportColumnFormat.Number));
        Assert.Equal("1:30 PM", ReportCellFormatter.Text(new TimeSpan(13, 30, 0), ReportColumnFormat.Text));
        Assert.Equal("8a6f1d2e-0b7c-4a59-9d3e-1f2a3b4c5d6e", ReportCellFormatter.Text(id, ReportColumnFormat.Text));
        Assert.Equal("abc", ReportCellFormatter.Text("abc", ReportColumnFormat.Currency));
    }

    [Theory]
    [InlineData(ReportColumnFormat.Number, true)]
    [InlineData(ReportColumnFormat.Currency, true)]
    [InlineData(ReportColumnFormat.Percent, true)]
    [InlineData(ReportColumnFormat.Text, false)]
    [InlineData(ReportColumnFormat.Date, false)]
    public void IsNumeric(ReportColumnFormat format, bool expected)
    {
        Assert.Equal(expected, ReportCellFormatter.IsNumeric(format));
    }

    [Fact]
    public void TryGetDateTime_OtherValues_AreNotDates()
    {
        Assert.False(ReportCellFormatter.TryGetDateTime("2026-03-01", out _));
    }
}
