using Viper.Areas.Effort.Constants;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for ClinicalEffortTypes constants class.
/// Tests the IsClinical helper method for identifying clinical effort types.
/// </summary>
public sealed class ClinicalEffortTypesTests
{
    #region IsClinical Tests

    [Fact]
    public void IsClinical_ReturnsTrue_ForCLI()
    {
        var result = ClinicalEffortTypes.IsClinical("CLI");
        Assert.True(result);
    }

    [Theory]
    [InlineData("cli")]
    [InlineData("CLI")]
    [InlineData("Cli")]
    public void IsClinical_IsCaseInsensitive(string effortType)
    {
        var result = ClinicalEffortTypes.IsClinical(effortType);
        Assert.True(result);
    }

    [Fact]
    public void IsClinical_ReturnsFalse_ForNull()
    {
        var result = ClinicalEffortTypes.IsClinical(null);
        Assert.False(result);
    }

    [Fact]
    public void IsClinical_ReturnsFalse_ForEmptyString()
    {
        var result = ClinicalEffortTypes.IsClinical("");
        Assert.False(result);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("DIS")]
    [InlineData("VAR")]
    [InlineData("Unknown")]
    public void IsClinical_ReturnsFalse_ForOtherTypes(string effortType)
    {
        var result = ClinicalEffortTypes.IsClinical(effortType);
        Assert.False(result);
    }

    [Fact]
    public void Clinical_MatchesEffortConstants()
    {
        Assert.Equal(EffortConstants.ClinicalEffortType, ClinicalEffortTypes.Clinical);
    }

    #endregion
}
