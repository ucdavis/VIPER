using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for CourseClassificationService.
/// Tests classification logic for determining course types and allowed effort types.
/// </summary>
public sealed class CourseClassificationServiceTests
{
    private readonly CourseClassificationService _service = new();

    #region IsDvmCourse Tests

    [Fact]
    public void IsDvmCourse_NullInput_ReturnsFalse()
    {
        var result = _service.IsDvmCourse(null);
        Assert.False(result);
    }

    [Fact]
    public void IsDvmCourse_EmptyString_ReturnsFalse()
    {
        var result = _service.IsDvmCourse("");
        Assert.False(result);
    }

    [Theory]
    [InlineData("DVM")]
    [InlineData("dvm")]
    [InlineData("Dvm")]
    [InlineData("VET")]
    [InlineData("vet")]
    [InlineData("Vet")]
    public void IsDvmCourse_DvmOrVetSubjectCode_ReturnsTrue(string subjCode)
    {
        var result = _service.IsDvmCourse(subjCode);
        Assert.True(result);
    }

    [Theory]
    [InlineData("APC")]
    [InlineData("PHR")]
    [InlineData("PMI")]
    [InlineData("BIO")]
    [InlineData("CHE")]
    public void IsDvmCourse_OtherSubjectCodes_ReturnsFalse(string subjCode)
    {
        var result = _service.IsDvmCourse(subjCode);
        Assert.False(result);
    }

    #endregion

    #region Is199299Course Tests

    [Fact]
    public void Is199299Course_NullInput_ReturnsFalse()
    {
        var result = _service.Is199299Course(null);
        Assert.False(result);
    }

    [Fact]
    public void Is199299Course_EmptyString_ReturnsFalse()
    {
        var result = _service.Is199299Course("");
        Assert.False(result);
    }

    [Theory]
    [InlineData("199")]
    [InlineData("299")]
    [InlineData("199A")]
    [InlineData("199B")]
    [InlineData("199R")]
    [InlineData("299A")]
    [InlineData("299R")]
    public void Is199299Course_ValidCourseNumber_ReturnsTrue(string crseNumb)
    {
        var result = _service.Is199299Course(crseNumb);
        Assert.True(result);
    }

    [Theory]
    [InlineData("1990")]
    [InlineData("2995")]
    [InlineData("19900")]
    [InlineData("29912")]
    [InlineData("200")]
    [InlineData("19")]
    [InlineData("99")]
    [InlineData("100")]
    [InlineData("300")]
    public void Is199299Course_InvalidCourseNumber_ReturnsFalse(string crseNumb)
    {
        var result = _service.Is199299Course(crseNumb);
        Assert.False(result);
    }

    #endregion

    #region IsRCourse Tests

    [Fact]
    public void IsRCourse_NullInput_ReturnsFalse()
    {
        var result = _service.IsRCourse(null);
        Assert.False(result);
    }

    [Fact]
    public void IsRCourse_EmptyString_ReturnsFalse()
    {
        var result = _service.IsRCourse("");
        Assert.False(result);
    }

    [Theory]
    [InlineData("200R")]
    [InlineData("299R")]
    [InlineData("300R")]
    [InlineData("200r")]
    [InlineData("299r")]
    public void IsRCourse_EndsWithR_ReturnsTrue(string crseNumb)
    {
        var result = _service.IsRCourse(crseNumb);
        Assert.True(result);
    }

    [Theory]
    [InlineData("200")]
    [InlineData("299")]
    [InlineData("R200")]
    [InlineData("199A")]
    public void IsRCourse_DoesNotEndWithR_ReturnsFalse(string crseNumb)
    {
        var result = _service.IsRCourse(crseNumb);
        Assert.False(result);
    }

    #endregion

    #region IsGenericRCourse Tests

    [Fact]
    public void IsGenericRCourse_NullInput_ReturnsFalse()
    {
        var result = _service.IsGenericRCourse(null);
        Assert.False(result);
    }

    [Fact]
    public void IsGenericRCourse_EmptyString_ReturnsFalse()
    {
        var result = _service.IsGenericRCourse("");
        Assert.False(result);
    }

    [Theory]
    [InlineData("RESID")]
    [InlineData("resid")]
    [InlineData("Resid")]
    [InlineData("ReSiD")]
    public void IsGenericRCourse_ResidCrn_ReturnsTrue(string crn)
    {
        var result = _service.IsGenericRCourse(crn);
        Assert.True(result);
    }

    [Theory]
    [InlineData("RESIDENT")]
    [InlineData("RES")]
    [InlineData("12345")]
    [InlineData("RESID1")]
    public void IsGenericRCourse_OtherCrns_ReturnsFalse(string crn)
    {
        var result = _service.IsGenericRCourse(crn);
        Assert.False(result);
    }

    #endregion

    #region IsAllowedForSelfImport Tests

    [Fact]
    public void IsAllowedForSelfImport_NullInput_ReturnsTrue()
    {
        var result = _service.IsAllowedForSelfImport(null);
        Assert.True(result);
    }

    [Fact]
    public void IsAllowedForSelfImport_EmptyString_ReturnsTrue()
    {
        var result = _service.IsAllowedForSelfImport("");
        Assert.True(result);
    }

    [Theory]
    [InlineData("DVM")]
    [InlineData("VET")]
    [InlineData("dvm")]
    [InlineData("vet")]
    public void IsAllowedForSelfImport_DvmOrVetSubjectCode_ReturnsFalse(string subjCode)
    {
        var result = _service.IsAllowedForSelfImport(subjCode);
        Assert.False(result);
    }

    [Theory]
    [InlineData("APC")]
    [InlineData("PHR")]
    [InlineData("PMI")]
    [InlineData("BIO")]
    [InlineData("CHE")]
    public void IsAllowedForSelfImport_OtherSubjectCodes_ReturnsTrue(string subjCode)
    {
        var result = _service.IsAllowedForSelfImport(subjCode);
        Assert.True(result);
    }

    #endregion

    #region Classify Tests

    [Fact]
    public void Classify_DvmSubjectCode_SetsIsDvmCourseTrue()
    {
        var course = new EffortCourse
        {
            SubjCode = "DVM",
            CrseNumb = "100",
            Crn = "12345"
        };

        var result = _service.Classify(course);

        Assert.True(result.IsDvmCourse);
        Assert.False(result.Is199299Course);
        Assert.False(result.IsRCourse);
        Assert.False(result.IsGenericRCourse);
    }

    [Fact]
    public void Classify_199CourseNumber_SetsIs199299CourseTrue()
    {
        var course = new EffortCourse
        {
            SubjCode = "APC",
            CrseNumb = "199",
            Crn = "12345"
        };

        var result = _service.Classify(course);

        Assert.False(result.IsDvmCourse);
        Assert.True(result.Is199299Course);
        Assert.False(result.IsRCourse);
        Assert.False(result.IsGenericRCourse);
    }

    [Fact]
    public void Classify_RSuffixCourseNumber_SetsIsRCourseTrue()
    {
        var course = new EffortCourse
        {
            SubjCode = "PHR",
            CrseNumb = "200R",
            Crn = "12345"
        };

        var result = _service.Classify(course);

        Assert.False(result.IsDvmCourse);
        Assert.False(result.Is199299Course);
        Assert.True(result.IsRCourse);
        Assert.False(result.IsGenericRCourse);
    }

    [Fact]
    public void Classify_ResidCrn_SetsIsGenericRCourseTrue()
    {
        var course = new EffortCourse
        {
            SubjCode = "VET",
            CrseNumb = "200R",
            Crn = "RESID"
        };

        var result = _service.Classify(course);

        Assert.True(result.IsDvmCourse);
        Assert.False(result.Is199299Course);
        Assert.True(result.IsRCourse);
        Assert.True(result.IsGenericRCourse);
    }

    [Fact]
    public void Classify_299RCourse_SetsBothIs199299AndIsRCourseTrue()
    {
        var course = new EffortCourse
        {
            SubjCode = "APC",
            CrseNumb = "299R",
            Crn = "12345"
        };

        var result = _service.Classify(course);

        Assert.False(result.IsDvmCourse);
        Assert.True(result.Is199299Course);
        Assert.True(result.IsRCourse);
        Assert.False(result.IsGenericRCourse);
    }

    [Fact]
    public void Classify_RegularCourse_AllFlagsAreFalse()
    {
        var course = new EffortCourse
        {
            SubjCode = "APC",
            CrseNumb = "101",
            Crn = "12345"
        };

        var result = _service.Classify(course);

        Assert.False(result.IsDvmCourse);
        Assert.False(result.Is199299Course);
        Assert.False(result.IsRCourse);
        Assert.False(result.IsGenericRCourse);
    }

    #endregion
}
