using System.ComponentModel.DataAnnotations;
using Areas.CMS.Validation;

namespace Viper.test.CMS;

/// <summary>
/// Tests for MaxLengthEachAttribute, which bounds each string element of a collection (used for
/// left-nav item permission strings vs the varchar(500) permission column).
/// </summary>
public sealed class MaxLengthEachAttributeTests
{
    private static ValidationResult? Validate(object? value, int length)
    {
        var attribute = new MaxLengthEachAttribute(length);
        return attribute.GetValidationResult(value, new ValidationContext(new object()));
    }

    [Fact]
    public void AllElementsWithinLimit_Succeeds()
    {
        var value = new List<string> { "SVMSecure.CATS", new string('a', 500) };

        Assert.Equal(ValidationResult.Success, Validate(value, 500));
    }

    [Fact]
    public void AnyElementOverLimit_Fails()
    {
        var value = new List<string> { "ok", new string('a', 501) };

        Assert.NotEqual(ValidationResult.Success, Validate(value, 500));
    }

    [Fact]
    public void NullValue_Succeeds()
    {
        Assert.Equal(ValidationResult.Success, Validate(null, 500));
    }

    [Fact]
    public void EmptyCollection_Succeeds()
    {
        Assert.Equal(ValidationResult.Success, Validate(new List<string>(), 500));
    }
}
