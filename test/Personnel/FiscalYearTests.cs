using Viper.Areas.Personnel.Reports;

namespace Viper.test.Personnel;

public sealed class FiscalYearTests
{
    [Theory]
    [InlineData(2026, 7, 1, 2026)]
    [InlineData(2026, 12, 31, 2026)]
    [InlineData(2027, 1, 1, 2026)]
    [InlineData(2027, 6, 30, 2026)]
    public void StartOf_IsTheMostRecentJulyFirst(int year, int month, int day, int startYear)
    {
        var date = new DateOnly(year, month, day);

        Assert.Equal(new DateOnly(startYear, 7, 1), FiscalYear.StartOf(date));
        Assert.Equal(new DateOnly(startYear + 1, 6, 30), FiscalYear.EndOf(date));
    }
}
