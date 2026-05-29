using Viper.Areas.Effort.Services.Harvest;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for HarvestTimeParser utility class.
/// Tests parsing of various time formats used in CREST data.
/// </summary>
public sealed class HarvestTimeParserTests
{
    #region ParseTimeString Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseTimeString_NullOrEmpty_ReturnsNull(string? input)
    {
        var result = HarvestTimeParser.ParseTimeString(input);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("8:00 AM", 8, 0)]
    [InlineData("8:30 AM", 8, 30)]
    [InlineData("12:00 PM", 12, 0)]
    [InlineData("1:30 PM", 13, 30)]
    [InlineData("11:59 PM", 23, 59)]
    [InlineData("0800", 8, 0)]
    [InlineData("1430", 14, 30)]
    [InlineData("0000", 0, 0)]
    [InlineData("2359", 23, 59)]
    [InlineData("800", 8, 0)]
    [InlineData("900", 9, 0)]
    [InlineData("130", 1, 30)]
    public void ParseTimeString_ValidTimeFormats_ParsesCorrectly(string input, int expectedHour, int expectedMinute)
    {
        var result = HarvestTimeParser.ParseTimeString(input);

        Assert.NotNull(result);
        var value = result!.Value;
        Assert.Equal(expectedHour, value.Hours);
        Assert.Equal(expectedMinute, value.Minutes);
    }

    [Fact]
    public void ParseTimeString_MilitaryTime2400_Returns24Hours()
    {
        var result = HarvestTimeParser.ParseTimeString("2400");

        Assert.NotNull(result);
        // TimeSpan(24, 0, 0) stores as 1 day, 0 hours - check TotalHours
        Assert.Equal(24, result.GetValueOrDefault().TotalHours);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("25:00")]
    [InlineData("12:60")]
    [InlineData("99")]
    [InlineData("12345")]
    public void ParseTimeString_InvalidFormat_ReturnsNull(string input)
    {
        var result = HarvestTimeParser.ParseTimeString(input);
        Assert.Null(result);
    }

    #endregion

    #region CalculateSessionMinutes Tests

    [Fact]
    public void CalculateSessionMinutes_NullDates_ReturnsZero()
    {
        var result = HarvestTimeParser.CalculateSessionMinutes(
            null, "0800", DateTime.Today, "0900");

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateSessionMinutes_NullTimes_ReturnsZero()
    {
        var result = HarvestTimeParser.CalculateSessionMinutes(
            DateTime.Today, null, DateTime.Today, "0900");

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateSessionMinutes_SameDay_OneHourSession_Returns60()
    {
        var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var result = HarvestTimeParser.CalculateSessionMinutes(
            date, "0800", date, "0900");

        Assert.Equal(60, result);
    }

    [Fact]
    public void CalculateSessionMinutes_SameDay_TwoHourSession_Returns120()
    {
        var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var result = HarvestTimeParser.CalculateSessionMinutes(
            date, "1400", date, "1600");

        Assert.Equal(120, result);
    }

    [Fact]
    public void CalculateSessionMinutes_SameDay_90MinuteSession_Returns90()
    {
        var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var result = HarvestTimeParser.CalculateSessionMinutes(
            date, "0930", date, "1100");

        Assert.Equal(90, result);
    }

    [Fact]
    public void CalculateSessionMinutes_EndTime2400_CalculatesCorrectly()
    {
        var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var result = HarvestTimeParser.CalculateSessionMinutes(
            date, "2200", date, "2400");

        Assert.Equal(120, result);
    }

    [Fact]
    public void CalculateSessionMinutes_CrossesMidnight_CalculatesCorrectly()
    {
        var startDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 1, 16, 0, 0, 0, DateTimeKind.Utc);

        var result = HarvestTimeParser.CalculateSessionMinutes(
            startDate, "2200", endDate, "0100");

        Assert.Equal(180, result);
    }

    [Fact]
    public void CalculateSessionMinutes_EndBeforeStart_ReturnsZero()
    {
        var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var result = HarvestTimeParser.CalculateSessionMinutes(
            date, "1400", date, "1300");

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateSessionMinutes_SameStartAndEnd_ReturnsZero()
    {
        var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var result = HarvestTimeParser.CalculateSessionMinutes(
            date, "1400", date, "1400");

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateSessionMinutes_InvalidTimeFormat_ReturnsZero()
    {
        var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var result = HarvestTimeParser.CalculateSessionMinutes(
            date, "invalid", date, "0900");

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateSessionMinutes_WithStandardTimeFormat_ParsesCorrectly()
    {
        var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var result = HarvestTimeParser.CalculateSessionMinutes(
            date, "8:00 AM", date, "9:30 AM");

        Assert.Equal(90, result);
    }

    #endregion
}
