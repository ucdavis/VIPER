using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for EvalHarvestService pure/static helper methods.
/// </summary>
public sealed class EvalHarvestServiceTests
{
    #region ApplyRatingCounts

    [Fact]
    public void ApplyRatingCounts_SetsCountsAndPercentages()
    {
        var quant = new EhQuant();
        EvalHarvestService.ApplyRatingCounts(quant, 1, 2, 3, 4, 10);

        Assert.Equal(1, quant.Count1N);
        Assert.Equal(2, quant.Count2N);
        Assert.Equal(3, quant.Count3N);
        Assert.Equal(4, quant.Count4N);
        Assert.Equal(10, quant.Count5N);

        Assert.Equal(20, quant.Respondents);
        Assert.Equal(5.0, quant.Count1P);
        Assert.Equal(10.0, quant.Count2P);
        Assert.Equal(15.0, quant.Count3P);
        Assert.Equal(20.0, quant.Count4P);
        Assert.Equal(50.0, quant.Count5P);
    }

    [Fact]
    public void ApplyRatingCounts_ComputesMeanCorrectly()
    {
        var quant = new EhQuant();
        // All respondents gave a 3 → mean should be exactly 3.0
        EvalHarvestService.ApplyRatingCounts(quant, 0, 0, 10, 0, 0);

        Assert.Equal(3.0, quant.Mean);
    }

    [Fact]
    public void ApplyRatingCounts_ComputesMeanWithMixedCounts()
    {
        var quant = new EhQuant();
        // 2×1 + 3×5 = 17, total=5, mean=3.4
        EvalHarvestService.ApplyRatingCounts(quant, 2, 0, 0, 0, 3);

        Assert.Equal(3.4, quant.Mean);
    }

    [Fact]
    public void ApplyRatingCounts_ComputesStandardDeviation()
    {
        var quant = new EhQuant();
        // All same rating → SD should be 0
        EvalHarvestService.ApplyRatingCounts(quant, 0, 0, 0, 5, 0);

        Assert.Equal(0.0, quant.Sd);
    }

    [Fact]
    public void ApplyRatingCounts_ComputesStandardDeviationWithSpread()
    {
        var quant = new EhQuant();
        // 5×1, 5×5 → mean=3.0, variance=((1-3)²×5 + (5-3)²×5)/10 = (20+20)/10 = 4, SD=2.0
        EvalHarvestService.ApplyRatingCounts(quant, 5, 0, 0, 0, 5);

        Assert.Equal(3.0, quant.Mean);
        Assert.Equal(2.0, quant.Sd);
    }

    [Fact]
    public void ApplyRatingCounts_AllZeros_SetsMeanAndSdToZero()
    {
        var quant = new EhQuant();
        EvalHarvestService.ApplyRatingCounts(quant, 0, 0, 0, 0, 0);

        Assert.Equal(0, quant.Respondents);
        Assert.Equal(0.0, quant.Mean);
        Assert.Equal(0.0, quant.Sd);
        Assert.Equal(0.0, quant.Count1P);
    }

    [Fact]
    public void ApplyRatingCounts_SingleRespondent()
    {
        var quant = new EhQuant();
        EvalHarvestService.ApplyRatingCounts(quant, 0, 0, 0, 0, 1);

        Assert.Equal(1, quant.Respondents);
        Assert.Equal(5.0, quant.Mean);
        Assert.Equal(0.0, quant.Sd);
        Assert.Equal(100.0, quant.Count5P);
    }

    #endregion

    #region HasAnyResponses

    [Fact]
    public void HasAnyResponses_AllZeros_ReturnsFalse()
    {
        var request = new UpdateAdHocEvalRequest
        {
            Count1 = 0, Count2 = 0, Count3 = 0, Count4 = 0, Count5 = 0
        };
        Assert.False(EvalHarvestService.HasAnyResponses(request));
    }

    [Fact]
    public void HasAnyResponses_OneNonZero_ReturnsTrue()
    {
        var request = new UpdateAdHocEvalRequest
        {
            Count1 = 0, Count2 = 0, Count3 = 1, Count4 = 0, Count5 = 0
        };
        Assert.True(EvalHarvestService.HasAnyResponses(request));
    }

    #endregion

    #region IsCereBlocked

    [Fact]
    public void IsCereBlocked_NullCourse_ReturnsFalse()
    {
        Assert.False(EvalHarvestService.IsCereBlocked(null));
    }

    [Fact]
    public void IsCereBlocked_AdHocTrue_ReturnsFalse()
    {
        var course = new EhCourse { IsAdHoc = true };
        Assert.False(EvalHarvestService.IsCereBlocked(course));
    }

    [Fact]
    public void IsCereBlocked_AdHocFalse_ReturnsTrue()
    {
        var course = new EhCourse { IsAdHoc = false };
        Assert.True(EvalHarvestService.IsCereBlocked(course));
    }

    [Fact]
    public void IsCereBlocked_AdHocNull_ReturnsTrue()
    {
        var course = new EhCourse { IsAdHoc = null };
        Assert.True(EvalHarvestService.IsCereBlocked(course));
    }

    #endregion
}
