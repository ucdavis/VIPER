namespace Viper.Areas.Reports.Engine;

/// <summary>
/// A number computed from a set of rows, such as a count, sum or average. Totals, subtotals,
/// summaries, pivots and charts all use measures, so every aggregate in a report is computed
/// the same way. Create measures with the factory methods on <see cref="ReportBuilder{TRow, TParams}"/>.
/// </summary>
public sealed class ReportMeasure<TRow>
{
    private readonly Func<IReadOnlyCollection<TRow>, decimal?> _compute;

    internal ReportMeasure(Func<IReadOnlyCollection<TRow>, decimal?> compute)
    {
        _compute = compute;
    }

    internal decimal? Compute(IReadOnlyCollection<TRow> rows)
    {
        return _compute(rows);
    }
}

/// <summary>
/// Aggregate functions over the non-null values of a measure. Empty input gives null for
/// everything except the sum, which is zero, so an empty group shows "0" rather than a blank total.
/// </summary>
internal static class ReportMath
{
    public static decimal? Sum(IReadOnlyCollection<decimal> values)
    {
        return values.Sum();
    }

    public static decimal? Average(IReadOnlyCollection<decimal> values)
    {
        return values.Count == 0 ? null : values.Average();
    }

    public static decimal? Minimum(IReadOnlyCollection<decimal> values)
    {
        return values.Count == 0 ? null : values.Min();
    }

    public static decimal? Maximum(IReadOnlyCollection<decimal> values)
    {
        return values.Count == 0 ? null : values.Max();
    }

    /// <summary>
    /// Sample standard deviation (divides by n - 1), matching SQL Server's STDEV, which the
    /// legacy salary reports used. Needs at least two values.
    /// </summary>
    public static decimal? StandardDeviation(IReadOnlyCollection<decimal> values)
    {
        if (values.Count < 2)
        {
            return null;
        }

        decimal mean = values.Average();
        decimal sumOfSquares = values.Sum(value => (value - mean) * (value - mean));
        return (decimal)Math.Sqrt((double)(sumOfSquares / (values.Count - 1)));
    }
}
