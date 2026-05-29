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

    // ── Term-code-based methods ──────────────────────────────────────

    #region GetAcademicYearFromTermCode Tests

    [Theory]
    [InlineData(202501, "2024-2025")]   // Winter Quarter -> previous year
    [InlineData(202502, "2024-2025")]   // Spring Semester -> previous year
    [InlineData(202503, "2024-2025")]   // Spring Quarter -> previous year
    [InlineData(202504, "2025-2026")]   // Summer Session 1 -> current year
    [InlineData(202505, "2025-2026")]   // Summer Session 2 -> current year
    [InlineData(202506, "2025-2026")]   // Summer Special -> current year
    [InlineData(202508, "2025-2026")]   // Summer Semester -> current year
    [InlineData(202409, "2024-2025")]   // Fall Semester -> current year
    [InlineData(202410, "2024-2025")]   // Fall Quarter -> current year
    public void GetAcademicYearFromTermCode_ReturnsCorrectYear(int termCode, string expectedAY)
    {
        Assert.Equal(expectedAY, AcademicYearHelper.GetAcademicYearFromTermCode(termCode));
    }

    [Fact]
    public void GetAcademicYearFromTermCode_SummerBelongsToCurrentYear_NotPrevious()
    {
        // This is the specific bug that was fixed: Summer 2024 should be in "2024-2025", not "2023-2024"
        Assert.Equal("2024-2025", AcademicYearHelper.GetAcademicYearFromTermCode(202404)); // Summer 2024
        Assert.Equal("2025-2026", AcademicYearHelper.GetAcademicYearFromTermCode(202504)); // Summer 2025
    }

    #endregion

    #region GetTermCodesForAcademicYear Tests

    [Fact]
    public void GetTermCodesForAcademicYear_FiltersAndOrdersCorrectly()
    {
        var allTerms = new[] { 202410, 202409, 202501, 202502, 202503, 202504, 202510 };
        var result = AcademicYearHelper.GetTermCodesForAcademicYear(allTerms, 2024);

        // AY 2024-2025: Fall 2024 (09, 10) + Winter/Spring 2025 (01, 02, 03)
        // NOT Summer 2025 (04) or Fall 2025 (10)
        Assert.Equal(new[] { 202503, 202502, 202501, 202410, 202409 }, result);
    }

    [Fact]
    public void GetTermCodesForAcademicYear_IncludesSummerInCorrectYear()
    {
        var allTerms = new[] { 202404, 202409, 202504 };
        var result = AcademicYearHelper.GetTermCodesForAcademicYear(allTerms, 2024);

        // AY 2024-2025 should include Summer 2024 (202404) and Fall 2024 (202409)
        // but NOT Summer 2025 (202504)
        Assert.Equal(new[] { 202409, 202404 }, result);
    }

    [Fact]
    public void GetTermCodesForAcademicYear_EmptyInput_ReturnsEmpty()
    {
        var result = AcademicYearHelper.GetTermCodesForAcademicYear([], 2024);
        Assert.Empty(result);
    }

    [Fact]
    public void GetTermCodesForAcademicYear_NoMatchingTerms_ReturnsEmpty()
    {
        var allTerms = new[] { 202310, 202401 };
        var result = AcademicYearHelper.GetTermCodesForAcademicYear(allTerms, 2024);
        Assert.Empty(result);
    }

    #endregion
}
