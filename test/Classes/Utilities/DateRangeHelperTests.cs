using Viper.Classes.Utilities;

namespace Viper.test.Classes.Utilities;

/// <summary>
/// Tests for DateRangeHelper.ExclusiveUpperBound, the shared "To" filter bound used by both
/// the CMS file-audit and content-history date filters. A date-only value is inclusive through
/// the end of that day (the next midnight is returned, to compare with &lt;); any value that
/// carries a time of day, even a single tick past midnight, is returned unchanged.
/// </summary>
public class DateRangeHelperTests
{
    [Fact]
    public void ExclusiveUpperBound_DateOnly_AdvancesOneDay()
    {
        var to = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Local);

        var result = DateRangeHelper.ExclusiveUpperBound(to);

        Assert.Equal(new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Local), result);
    }

    [Fact]
    public void ExclusiveUpperBound_WithTimeComponent_ReturnedUnchanged()
    {
        // A single tick past midnight already counts as carrying a time of day, so the bound is
        // used as-is. This pins the non-obvious "to.Date == to" midnight check in the helper.
        var to = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Local).AddTicks(1);

        var result = DateRangeHelper.ExclusiveUpperBound(to);

        Assert.Equal(to, result);
    }

    [Fact]
    public void ExclusiveUpperBound_MaxValueDate_ClampsToMaxValueWithoutOverflow()
    {
        // A user-suppliable "To" at the DateTime ceiling has no next midnight to advance to, so
        // advancing by a day would overflow. The helper clamps to MaxValue instead.
        var result = DateRangeHelper.ExclusiveUpperBound(DateTime.MaxValue.Date);

        Assert.Equal(DateTime.MaxValue, result);
    }
}
