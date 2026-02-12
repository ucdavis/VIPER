using Viper.Areas.Effort.Helpers;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for TermValidationHelper utility class.
/// Tests centralized term eligibility checks for harvest, clinical import, and percent rollover.
/// </summary>
public sealed class TermValidationHelperTests
{
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

