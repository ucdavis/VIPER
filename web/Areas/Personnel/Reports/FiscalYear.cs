namespace Viper.Areas.Personnel.Reports;

/// <summary>
/// The university fiscal year, July 1 through June 30.
/// </summary>
public static class FiscalYear
{
    private const int FirstMonth = 7;

    public static DateOnly StartOf(DateOnly date)
    {
        int year = date.Month >= FirstMonth ? date.Year : date.Year - 1;
        return new DateOnly(year, FirstMonth, 1);
    }

    public static DateOnly EndOf(DateOnly date)
    {
        return StartOf(date).AddYears(1).AddDays(-1);
    }
}
