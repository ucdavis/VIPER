using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Tests for BaseReportService static helper methods.
/// Uses a concrete test subclass to access protected members.
/// </summary>
public sealed class BaseReportServiceTests
{
    // Concrete subclass to expose protected static methods for testing
    private class TestableReportService : BaseReportService
    {
        public TestableReportService() : base(null!) { }

        public static int TestParseAcademicYearStart(string academicYear)
            => ParseAcademicYearStart(academicYear);

        public static Dictionary<string, decimal> TestCalculateAverages(
            Dictionary<string, decimal> totals,
            IEnumerable<string> effortTypes,
            int facultyCount,
            int facultyWithCliCount)
            => CalculateAverages(totals, effortTypes, facultyCount, facultyWithCliCount);

        public static List<string> TestGetOrderedEffortTypes(List<string> effortTypes)
            => GetOrderedEffortTypes(effortTypes);

        public static List<(string Type, bool IsSpacer)> TestBuildExcelEffortColumns(List<string> orderedTypes)
            => BuildExcelEffortColumns(orderedTypes);
    }

    #region ParseAcademicYearStart

    [Theory]
    [InlineData("2024-2025", 2024)]
    [InlineData("2020-2021", 2020)]
    [InlineData("1999-2000", 1999)]
    public void ParseAcademicYearStart_ValidFormat_ReturnsStartYear(string input, int expected)
    {
        var result = TestableReportService.TestParseAcademicYearStart(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2024")]
    [InlineData("2024-25")]
    [InlineData("24-25")]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("2024/2025")]
    [InlineData("2024-2025-2026")]
    public void ParseAcademicYearStart_InvalidFormat_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() =>
            TestableReportService.TestParseAcademicYearStart(input));
    }

    #endregion

    #region CalculateAverages

    [Fact]
    public void CalculateAverages_BasicDivision_RoundsToOneDecimal()
    {
        var totals = new Dictionary<string, decimal> { { "LEC", 100m }, { "LAB", 33m } };
        var types = new[] { "LEC", "LAB" };

        var result = TestableReportService.TestCalculateAverages(totals, types, 3, 3);

        Assert.Equal(33.3m, result["LEC"]);
        Assert.Equal(11.0m, result["LAB"]);
    }

    [Fact]
    public void CalculateAverages_CliUsesFacultyWithCliCount()
    {
        var totals = new Dictionary<string, decimal> { { "CLI", 50m }, { "LEC", 50m } };
        var types = new[] { "CLI", "LEC" };

        var result = TestableReportService.TestCalculateAverages(totals, types, 10, 2);

        // CLI uses facultyWithCliCount=2, LEC uses facultyCount=10
        Assert.Equal(25.0m, result["CLI"]);
        Assert.Equal(5.0m, result["LEC"]);
    }

    [Fact]
    public void CalculateAverages_ZeroDivisor_SkipsEffortType()
    {
        var totals = new Dictionary<string, decimal> { { "CLI", 50m }, { "LEC", 50m } };
        var types = new[] { "CLI", "LEC" };

        var result = TestableReportService.TestCalculateAverages(totals, types, 5, 0);

        // CLI divisor is 0 → skipped; LEC divisor is 5 → included
        Assert.DoesNotContain("CLI", result.Keys);
        Assert.Equal(10.0m, result["LEC"]);
    }

    [Fact]
    public void CalculateAverages_ZeroTotal_SkipsEffortType()
    {
        var totals = new Dictionary<string, decimal> { { "LEC", 0m } };
        var types = new[] { "LEC" };

        var result = TestableReportService.TestCalculateAverages(totals, types, 10, 10);

        Assert.Empty(result);
    }

    [Fact]
    public void CalculateAverages_MissingTotal_SkipsEffortType()
    {
        var totals = new Dictionary<string, decimal> { { "LEC", 100m } };
        var types = new[] { "LEC", "LAB" }; // LAB not in totals

        var result = TestableReportService.TestCalculateAverages(totals, types, 5, 5);

        Assert.Single(result);
        Assert.Equal(20.0m, result["LEC"]);
    }

    [Fact]
    public void CalculateAverages_CliIsCaseInsensitive()
    {
        var totals = new Dictionary<string, decimal> { { "CLI", 30m } };
        var types = new[] { "CLI" };

        var result = TestableReportService.TestCalculateAverages(totals, types, 10, 3);

        // CLI uses facultyWithCliCount=3
        Assert.Equal(10.0m, result["CLI"]);
    }

    #endregion

    #region GetOrderedEffortTypes

    [Fact]
    public void GetOrderedEffortTypes_ReturnsFixedOrderFirst()
    {
        var types = new List<string> { "LEC", "CLI", "LAB" };

        var result = TestableReportService.TestGetOrderedEffortTypes(types);

        // Fixed order: CLI, VAR, LEC, LAB, DIS, PBL, CBL, TBL, PRS, JLC, EXM
        var cliIdx = result.IndexOf("CLI");
        var lecIdx = result.IndexOf("LEC");
        var labIdx = result.IndexOf("LAB");
        Assert.True(cliIdx < lecIdx);
        Assert.True(lecIdx < labIdx);
    }

    [Fact]
    public void GetOrderedEffortTypes_AppendsExtrasAlphabetically()
    {
        var types = new List<string> { "CLI", "ZZZ", "AAA" };

        var result = TestableReportService.TestGetOrderedEffortTypes(types);

        // AAA and ZZZ are not in the fixed set, appended alphabetically
        var aaaIdx = result.IndexOf("AAA");
        var zzzIdx = result.IndexOf("ZZZ");
        Assert.True(aaaIdx > 0);
        Assert.True(zzzIdx > aaaIdx);
    }

    [Fact]
    public void GetOrderedEffortTypes_IncludesAllFixedColumnsEvenIfNotInInput()
    {
        var types = new List<string> { "LEC" };

        var result = TestableReportService.TestGetOrderedEffortTypes(types);

        // All 11 fixed columns are always included
        Assert.Contains("CLI", result);
        Assert.Contains("VAR", result);
        Assert.Contains("EXM", result);
    }

    [Fact]
    public void GetOrderedEffortTypes_EmptyInput_ReturnsFixedColumnsOnly()
    {
        var result = TestableReportService.TestGetOrderedEffortTypes([]);

        Assert.Equal(11, result.Count);
        Assert.Equal("CLI", result[0]);
        Assert.Equal("EXM", result[^1]);
    }

    #endregion

    #region BuildExcelEffortColumns

    [Fact]
    public void BuildExcelEffortColumns_InsertsSpacerAfterVarAndExm()
    {
        var types = new List<string> { "CLI", "VAR", "LEC", "EXM" };

        var result = TestableReportService.TestBuildExcelEffortColumns(types);

        // CLI, VAR, spacer, LEC, EXM (no spacer because EXM is last)
        Assert.Equal(5, result.Count);
        Assert.Equal(("CLI", false), result[0]);
        Assert.Equal(("VAR", false), result[1]);
        Assert.Equal(("", true), result[2]); // spacer after VAR
        Assert.Equal(("LEC", false), result[3]);
        Assert.Equal(("EXM", false), result[4]); // no spacer — last column
    }

    [Fact]
    public void BuildExcelEffortColumns_NoSpacerAfterLastColumn()
    {
        var types = new List<string> { "CLI", "VAR" };

        var result = TestableReportService.TestBuildExcelEffortColumns(types);

        // VAR is last → no spacer
        Assert.Equal(2, result.Count);
        Assert.All(result, col => Assert.False(col.IsSpacer));
    }

    [Fact]
    public void BuildExcelEffortColumns_NoSpacerColumns_NoSpacersInserted()
    {
        var types = new List<string> { "CLI", "LEC", "LAB" };

        var result = TestableReportService.TestBuildExcelEffortColumns(types);

        Assert.Equal(3, result.Count);
        Assert.All(result, col => Assert.False(col.IsSpacer));
    }

    [Fact]
    public void BuildExcelEffortColumns_EmptyInput_ReturnsEmpty()
    {
        var result = TestableReportService.TestBuildExcelEffortColumns([]);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildExcelEffortColumns_SpacerAfterVarWhenNotLast()
    {
        var types = new List<string> { "VAR", "LEC" };

        var result = TestableReportService.TestBuildExcelEffortColumns(types);

        Assert.Equal(3, result.Count);
        Assert.Equal(("VAR", false), result[0]);
        Assert.Equal(("", true), result[1]);
        Assert.Equal(("LEC", false), result[2]);
    }

    #endregion
}
