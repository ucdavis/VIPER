using Viper.Classes.Utilities;

namespace Test.Utilities;

public class AcademicYearHelperTests
{
    #region GetAcademicYear Tests

    [Theory]
    [InlineData(2025, 7, 1, 2026)]   // July 1, 2025 -> AY 2026
    [InlineData(2025, 12, 31, 2026)] // Dec 31, 2025 -> AY 2026
    [InlineData(2026, 1, 1, 2026)]   // Jan 1, 2026 -> AY 2026
    [InlineData(2026, 6, 30, 2026)]  // June 30, 2026 -> AY 2026
    [InlineData(2026, 7, 1, 2027)]   // July 1, 2026 -> AY 2027
    public void GetAcademicYear_ReturnsCorrectYear(int year, int month, int day, int expectedAY)
    {
        var date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local);
        Assert.Equal(expectedAY, AcademicYearHelper.GetAcademicYear(date));
    }

    #endregion

    #region GetAcademicYearStart Tests

    [Fact]
    public void GetAcademicYearStart_DateInJuly_ReturnsJuly1OfSameYear()
    {
        var date = new DateTime(2025, 9, 15, 0, 0, 0, DateTimeKind.Local);
        var result = AcademicYearHelper.GetAcademicYearStart(date);
        Assert.Equal(new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local), result);
    }

    [Fact]
    public void GetAcademicYearStart_DateInJanuary_ReturnsJuly1OfPriorYear()
    {
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Local);
        var result = AcademicYearHelper.GetAcademicYearStart(date);
        Assert.Equal(new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local), result);
    }

    #endregion

    #region GetDateRange Tests

    [Fact]
    public void GetDateRange_AcademicYear2026_ReturnsJuly2025ToJuly2026()
    {
        var range = AcademicYearHelper.GetDateRange(2026);

        Assert.Equal(new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local), range.StartDate);
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Local), range.EndDateExclusive);
    }

    [Fact]
    public void GetDateRange_EndDateInclusive_ReturnsJune30()
    {
        var range = AcademicYearHelper.GetDateRange(2026);

        Assert.Equal(new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Local), range.EndDateInclusive);
    }

    #endregion

    #region GetAcademicYearString Tests

    [Fact]
    public void GetAcademicYearString_DateInFall_ReturnsCorrectFormat()
    {
        var date = new DateTime(2025, 9, 15, 0, 0, 0, DateTimeKind.Local);
        Assert.Equal("2025-2026", AcademicYearHelper.GetAcademicYearString(date));
    }

    [Fact]
    public void GetAcademicYearString_DateInSpring_ReturnsCorrectFormat()
    {
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Local);
        Assert.Equal("2025-2026", AcademicYearHelper.GetAcademicYearString(date));
    }

    #endregion

    #region Boundary Tests

    [Fact]
    public void GetAcademicYear_June30AtEndOfDay_BelongsToCurrentAcademicYear()
    {
        var date = new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Local);
        Assert.Equal(2026, AcademicYearHelper.GetAcademicYear(date));
    }

    [Fact]
    public void GetAcademicYear_July1AtMidnight_BelongsToNextAcademicYear()
    {
        var date = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Local);
        Assert.Equal(2027, AcademicYearHelper.GetAcademicYear(date));
    }

    [Fact]
    public void GetDateRange_CanBeUsedForCorrectDateComparison()
    {
        var range = AcademicYearHelper.GetDateRange(2026);

        // A date on June 30 at end of day should be included
        var june30EndOfDay = new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Local);
        Assert.True(june30EndOfDay < range.EndDateExclusive);

        // A date on July 1 at midnight should be excluded
        var july1Midnight = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Local);
        Assert.False(july1Midnight < range.EndDateExclusive);
    }

    #endregion
}
