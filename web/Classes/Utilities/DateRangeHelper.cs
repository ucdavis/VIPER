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
    {
        // There is no day after DateTime.MaxValue.Date to advance to, so AddDays(1) would throw
        // for a user-suppliable filter value at the ceiling. Clamp to MaxValue, which is still a
        // safe exclusive upper bound (nothing sorts after it).
        if (to >= DateTime.MaxValue.Date)
        {
            return DateTime.MaxValue;
        }

        return to.Date == to ? to.AddDays(1) : to;
    }
}
