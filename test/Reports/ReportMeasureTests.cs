using Viper.Areas.Reports.Engine;

namespace Viper.test.Reports;

public sealed class ReportMeasureTests
{
    private static readonly ReportBuilder<SampleRow, SampleParams> Builder = new();

    #region ReportMath

    [Fact]
    public void Sum_EmptyIsZero()
    {
        Assert.Equal(0m, ReportMath.Sum([]));
        Assert.Equal(6m, ReportMath.Sum([1m, 2m, 3m]));
    }

    [Fact]
    public void AverageMinimumMaximum_EmptyIsNull()
    {
        Assert.Null(ReportMath.Average([]));
        Assert.Null(ReportMath.Minimum([]));
        Assert.Null(ReportMath.Maximum([]));
    }

    [Fact]
    public void AverageMinimumMaximum_ComputeFromValues()
    {
        decimal[] values = [4m, 1m, 7m];

        Assert.Equal(4m, ReportMath.Average(values));
        Assert.Equal(1m, ReportMath.Minimum(values));
        Assert.Equal(7m, ReportMath.Maximum(values));
    }

    [Fact]
    public void StandardDeviation_FewerThanTwoValues_IsNull()
    {
        Assert.Null(ReportMath.StandardDeviation([]));
        Assert.Null(ReportMath.StandardDeviation([5m]));
    }

    [Fact]
    public void StandardDeviation_IsSampleStandardDeviation()
    {
        decimal? deviation = ReportMath.StandardDeviation([1m, 2m, 3m, 4m]);

        Assert.NotNull(deviation);
        Assert.Equal(1.2910m, Math.Round(deviation.Value, 4));
    }

    #endregion

    #region Measure factories

    [Fact]
    public void Count_CountsRows()
    {
        Assert.Equal(5m, Builder.Count().Compute(TestReport.Rows()));
    }

    [Fact]
    public void CountDistinct_IgnoresNullKeys()
    {
        ReportMeasure<SampleRow> measure = Builder.CountDistinct(row => row.Salary);

        Assert.Equal(4m, measure.Compute(TestReport.Rows()));
    }

    [Fact]
    public void NumericMeasures_IgnoreNullValues()
    {
        IReadOnlyList<SampleRow> rows = TestReport.Rows();

        Assert.Equal(430_000m, Builder.Sum(row => row.Salary).Compute(rows));
        Assert.Equal(107_500m, Builder.Average(row => row.Salary).Compute(rows));
        Assert.Equal(70_000m, Builder.Minimum(row => row.Salary).Compute(rows));
        Assert.Equal(150_000m, Builder.Maximum(row => row.Salary).Compute(rows));
        Assert.NotNull(Builder.StandardDeviation(row => row.Age).Compute(rows));
    }

    [Fact]
    public void MeasureFactories_NullSelector_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Builder.CountDistinct<string>(null!));
        Assert.Throws<ArgumentNullException>(() => Builder.Sum(null!));
    }

    #endregion
}
