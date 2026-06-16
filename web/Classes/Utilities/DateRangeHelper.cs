namespace Viper.Classes.Utilities;

/// <summary>
/// Helpers for date-range query filters.
/// </summary>
public static class DateRangeHelper
{
    /// <summary>
    /// Exclusive upper bound for an inclusive "To" date filter. A value with no time component
    /// (midnight) is treated as "through the end of that day", so the next midnight is returned;
    /// a value that already carries a time of day is returned unchanged. Compare with
    /// &lt; against the result.
    /// </summary>
    public static DateTime ExclusiveUpperBound(DateTime to)
        => to.Date == to ? to.AddDays(1) : to;
}
