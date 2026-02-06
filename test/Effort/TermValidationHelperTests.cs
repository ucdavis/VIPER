using Viper.Areas.Effort.Helpers;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for TermValidationHelper utility class.
/// Tests centralized term eligibility checks for harvest, clinical import, and percent rollover.
/// </summary>
public sealed class TermValidationHelperTests
{
    #region IsFallTerm Tests

    [Fact]
    public void IsFallTerm_ReturnsTrue_ForFallSemester()
    {
        var result = TermValidationHelper.IsFallTerm("Fall Semester");
        Assert.True(result);
    }

    [Fact]
    public void IsFallTerm_ReturnsTrue_ForFallQuarter()
    {
        var result = TermValidationHelper.IsFallTerm("Fall Quarter");
        Assert.True(result);
    }

    [Theory]
    [InlineData("Winter Quarter")]
    [InlineData("Spring Semester")]
    [InlineData("Spring Quarter")]
    [InlineData("Summer Session 1")]
    [InlineData("Summer Session 2")]
    [InlineData("Summer Semester")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown")]
    public void IsFallTerm_ReturnsFalse_ForNonFallTermTypes(string termType)
    {
        var result = TermValidationHelper.IsFallTerm(termType);
        Assert.False(result);
    }

    [Fact]
    public void IsFallTerm_ReturnsFalse_WhenTermTypeNull()
    {
        var result = TermValidationHelper.IsFallTerm(null);
        Assert.False(result);
    }

    #endregion

    #region CanHarvest Tests

    [Theory]
    [InlineData("Created")]
    [InlineData("Harvested")]
    public void CanHarvest_ReturnsTrue_ForCreatedOrHarvested(string status)
    {
        var result = TermValidationHelper.CanHarvest(status);
        Assert.True(result);
    }

    [Theory]
    [InlineData("Opened")]
    [InlineData("Closed")]
    [InlineData("Verified")]
    public void CanHarvest_ReturnsFalse_ForOpenedOrClosed(string status)
    {
        var result = TermValidationHelper.CanHarvest(status);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CanHarvest_ReturnsFalse_ForNullOrEmpty(string? status)
    {
        var result = TermValidationHelper.CanHarvest(status);
        Assert.False(result);
    }

    #endregion

    #region IsFallTermByCode Tests

    [Theory]
    [InlineData(202509)] // Fall Semester (ends in 09)
    [InlineData(202410)] // Fall Quarter (ends in 10)
    [InlineData(201609)] // Older Fall Semester
    [InlineData(201610)] // Older Fall Quarter
    public void IsFallTermByCode_ReturnsTrue_ForFallTermCodes(int termCode)
    {
        var result = TermValidationHelper.IsFallTermByCode(termCode);
        Assert.True(result);
    }

    [Theory]
    [InlineData(202501)] // Winter Quarter
    [InlineData(202502)] // Spring Semester
    [InlineData(202503)] // Spring Quarter
    [InlineData(202504)] // Summer Semester
    [InlineData(202505)] // Summer Session 1
    [InlineData(202506)] // Special Session
    [InlineData(202507)] // Summer Session 2
    [InlineData(202508)] // Summer Quarter
    public void IsFallTermByCode_ReturnsFalse_ForNonFallTermCodes(int termCode)
    {
        var result = TermValidationHelper.IsFallTermByCode(termCode);
        Assert.False(result);
    }

    #endregion

    #region CanRolloverPercentByCode Tests

    [Theory]
    [InlineData(202509)] // Fall Semester
    [InlineData(202510)] // Fall Quarter
    public void CanRolloverPercentByCode_ReturnsTrue_OnlyForFallTerms(int termCode)
    {
        var result = TermValidationHelper.CanRolloverPercentByCode(termCode);
        Assert.True(result);
    }

    [Theory]
    [InlineData(202501)] // Winter Quarter
    [InlineData(202502)] // Spring Semester
    [InlineData(202504)] // Summer Semester
    public void CanRolloverPercentByCode_ReturnsFalse_ForNonFallTerms(int termCode)
    {
        var result = TermValidationHelper.CanRolloverPercentByCode(termCode);
        Assert.False(result);
    }

    #endregion

    #region CanRolloverPercent Tests

    [Theory]
    [InlineData("Fall Semester")]
    [InlineData("Fall Quarter")]
    public void CanRolloverPercent_ReturnsTrue_OnlyForFallTerms(string termType)
    {
        var result = TermValidationHelper.CanRolloverPercent(termType);
        Assert.True(result);
    }

    [Theory]
    [InlineData("Spring Semester")]
    [InlineData("Spring Quarter")]
    [InlineData("Winter Quarter")]
    [InlineData("Summer Session 1")]
    [InlineData(null)]
    public void CanRolloverPercent_ReturnsFalse_ForNonFallTerms(string? termType)
    {
        var result = TermValidationHelper.CanRolloverPercent(termType);
        Assert.False(result);
    }

    #endregion

    #region CanRolloverPercent (status, termCode) Tests

    [Theory]
    [InlineData("Created", 202509)]   // Fall Semester, Created
    [InlineData("Harvested", 202509)] // Fall Semester, Harvested
    [InlineData("Opened", 202509)]      // Fall Semester, Opened (legacy: "Openeded")
    [InlineData("Created", 202510)]   // Fall Quarter, Created
    [InlineData("Harvested", 202510)] // Fall Quarter, Harvested
    [InlineData("Opened", 202510)]      // Fall Quarter, Opened
    public void CanRolloverPercent_StatusAndTermCode_ReturnsTrue_ForNonClosedFallTerms(string status, int termCode)
    {
        var result = TermValidationHelper.CanRolloverPercent(status, termCode);
        Assert.True(result);
    }

    [Theory]
    [InlineData("Closed", 202509)]    // Fall Semester, Closed
    [InlineData("Closed", 202510)]    // Fall Quarter, Closed
    [InlineData("Created", 202502)]   // Spring Semester (not Fall)
    [InlineData("Created", 202504)]   // Summer Semester (not Fall)
    [InlineData("Harvested", 202501)] // Winter Quarter (not Fall)
    [InlineData("Opened", 202503)]      // Spring Quarter (not Fall)
    [InlineData(null, 202509)]
    [InlineData("", 202509)]
    public void CanRolloverPercent_StatusAndTermCode_ReturnsFalse_ForInvalidConditions(string? status, int termCode)
    {
        var result = TermValidationHelper.CanRolloverPercent(status, termCode);
        Assert.False(result);
    }

    #endregion

    #region IsSemesterTerm Tests

    [Theory]
    [InlineData(202402)] // Spring Semester (ends in 02)
    [InlineData(202404)] // Summer Semester (ends in 04)
    [InlineData(202409)] // Fall Semester (ends in 09)
    [InlineData(201602)] // Older Spring
    [InlineData(201609)] // Older Fall
    public void IsSemesterTerm_ReturnsTrue_ForSemesterCodes(int termCode)
    {
        var result = TermValidationHelper.IsSemesterTerm(termCode);
        Assert.True(result);
    }

    [Theory]
    [InlineData(202401)] // Winter Quarter (ends in 01)
    [InlineData(202403)] // Spring Quarter (ends in 03)
    [InlineData(202410)] // Fall Quarter (ends in 10)
    [InlineData(201601)] // Older Winter Quarter
    [InlineData(201603)] // Older Spring Quarter
    [InlineData(201610)] // Older Fall Quarter
    public void IsSemesterTerm_ReturnsFalse_ForQuarterCodes(int termCode)
    {
        var result = TermValidationHelper.IsSemesterTerm(termCode);
        Assert.False(result);
    }

    #endregion

    #region CanImportClinical Tests

    [Theory]
    [InlineData("Created", 202402)]
    [InlineData("Created", 202404)]
    [InlineData("Created", 202409)]
    [InlineData("Harvested", 202402)]
    [InlineData("Harvested", 202409)]
    [InlineData("Opened", 202402)]
    [InlineData("Opened", 202409)]
    public void CanImportClinical_ReturnsTrue_ForSemesterTermCreatedHarvestedOpened(string status, int termCode)
    {
        var result = TermValidationHelper.CanImportClinical(status, termCode);
        Assert.True(result);
    }

    [Theory]
    [InlineData("Closed", 202402)]
    [InlineData("Closed", 202409)]
    [InlineData("Verified", 202402)]
    [InlineData("Created", 202401)]  // Winter Quarter
    [InlineData("Created", 202403)]  // Spring Quarter
    [InlineData("Created", 202410)]  // Fall Quarter
    [InlineData("Harvested", 202401)]
    [InlineData("Opened", 202403)]
    [InlineData(null, 202402)]
    [InlineData("", 202402)]
    [InlineData("Archived", 202402)]
    [InlineData("Deleted", 202402)]
    [InlineData("PendingReview", 202402)]
    [InlineData("Unknown", 202402)]
    public void CanImportClinical_ReturnsFalse_ForInvalidConditions(string? status, int termCode)
    {
        var result = TermValidationHelper.CanImportClinical(status, termCode);
        Assert.False(result);
    }

    #endregion
}

