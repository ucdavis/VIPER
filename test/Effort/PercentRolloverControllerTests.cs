using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort.Controllers;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for PercentRolloverController API endpoints.
/// </summary>
public sealed class PercentRolloverControllerTests
{
    private readonly Mock<IPercentRolloverService> _rolloverServiceMock;
    private readonly Mock<ITermService> _termServiceMock;
    private readonly Mock<IEffortPermissionService> _permissionServiceMock;
    private readonly Mock<ILogger<PercentRolloverController>> _loggerMock;
    private readonly PercentRolloverController _controller;

    public PercentRolloverControllerTests()
    {
        _rolloverServiceMock = new Mock<IPercentRolloverService>();
        _termServiceMock = new Mock<ITermService>();
        _permissionServiceMock = new Mock<IEffortPermissionService>();
        _loggerMock = new Mock<ILogger<PercentRolloverController>>();

        _controller = new PercentRolloverController(
            _rolloverServiceMock.Object,
            _termServiceMock.Object,
            _permissionServiceMock.Object,
            _loggerMock.Object);

        SetupControllerContext();
    }

    private void SetupControllerContext()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            }
        };
    }

    #region GetPreview Tests - Validation

    [Fact]
    public async Task Preview_ReturnsNotFound_WhenTermNotFound()
    {
        // Arrange
        _termServiceMock.Setup(s => s.GetTermAsync(202510, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TermDto?)null);

        // Act
        var result = await _controller.GetPreview(202510, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Term 202510 not found", notFoundResult.Value);
    }

    [Fact]
    public async Task Preview_ReturnsBadRequest_ForNonFallTerm()
    {
        // Arrange - Spring Semester term (ends in 02)
        var term = new TermDto { TermCode = 202502, TermName = "Spring 2025" };
        _termServiceMock.Setup(s => s.GetTermAsync(202502, It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);

        // Act
        var result = await _controller.GetPreview(202502, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Percent rollover is only available for Fall terms", badRequestResult.Value);
    }

    [Fact]
    public async Task Preview_ReturnsBadRequest_ForSummerTerm()
    {
        // Arrange - Summer Semester term (ends in 04)
        var term = new TermDto { TermCode = 202504, TermName = "Summer 2025" };
        _termServiceMock.Setup(s => s.GetTermAsync(202504, It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);

        // Act
        var result = await _controller.GetPreview(202504, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Preview_ReturnsBadRequest_ForWinterQuarter()
    {
        // Arrange - Winter Quarter term (ends in 01)
        var term = new TermDto { TermCode = 202501, TermName = "Winter 2025" };
        _termServiceMock.Setup(s => s.GetTermAsync(202501, It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);

        // Act
        var result = await _controller.GetPreview(202501, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region GetPreview Tests - Success Cases

    [Fact]
    public async Task Preview_ReturnsPreview_ForFallSemester()
    {
        // Arrange - Fall Semester term (ends in 09)
        var term = new TermDto { TermCode = 202509, TermName = "Fall 2025" };
        var preview = CreateSamplePreview();

        _termServiceMock.Setup(s => s.GetTermAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);
        _rolloverServiceMock.Setup(s => s.GetRolloverPreviewAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        var result = await _controller.GetPreview(202509, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPreview = Assert.IsType<PercentRolloverPreviewDto>(okResult.Value);
        Assert.True(returnedPreview.IsRolloverApplicable);
        Assert.Equal(2025, returnedPreview.SourceAcademicYear);
        Assert.Equal(2026, returnedPreview.TargetAcademicYear);
    }

    [Fact]
    public async Task Preview_ReturnsPreview_ForFallQuarter()
    {
        // Arrange - Fall Quarter term (ends in 10)
        var term = new TermDto { TermCode = 202510, TermName = "Fall 2025" };
        var preview = CreateSamplePreview();

        _termServiceMock.Setup(s => s.GetTermAsync(202510, It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);
        _rolloverServiceMock.Setup(s => s.GetRolloverPreviewAsync(202510, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        var result = await _controller.GetPreview(202510, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<PercentRolloverPreviewDto>(okResult.Value);
    }

    [Fact]
    public async Task Preview_IncludesAssignments_WhenPendingRollover()
    {
        // Arrange - Fall Semester term (ends in 09)
        var term = new TermDto { TermCode = 202509, TermName = "Fall 2025" };
        var preview = new PercentRolloverPreviewDto
        {
            IsRolloverApplicable = true,
            SourceAcademicYear = 2025,
            TargetAcademicYear = 2026,
            SourceAcademicYearDisplay = "2024-2025",
            TargetAcademicYearDisplay = "2025-2026",
            Assignments =
            [
                new PercentRolloverItemPreview
                {
                    SourcePercentageId = 1,
                    PersonId = 123,
                    PersonName = "John Doe",
                    TypeName = "Administrative",
                    PercentageValue = 0.50,
                    CurrentEndDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
                    ProposedStartDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local),
                    ProposedEndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Local)
                },
                new PercentRolloverItemPreview
                {
                    SourcePercentageId = 2,
                    PersonId = 456,
                    PersonName = "Jane Smith",
                    TypeName = "Clinical",
                    PercentageValue = 0.25,
                    CurrentEndDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
                    ProposedStartDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local),
                    ProposedEndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Local)
                }
            ],
            ExistingAssignments = []
        };

        _termServiceMock.Setup(s => s.GetTermAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);
        _rolloverServiceMock.Setup(s => s.GetRolloverPreviewAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        var result = await _controller.GetPreview(202509, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPreview = Assert.IsType<PercentRolloverPreviewDto>(okResult.Value);
        Assert.Equal(2, returnedPreview.Assignments.Count);
        Assert.Empty(returnedPreview.ExistingAssignments);
        Assert.Equal("John Doe", returnedPreview.Assignments[0].PersonName);
        Assert.Equal(0.50, returnedPreview.Assignments[0].PercentageValue);
    }

    [Fact]
    public async Task Preview_IncludesExistingAssignments_WhenAlreadyRolled()
    {
        // Arrange - Fall Semester term (ends in 09)
        var term = new TermDto { TermCode = 202509, TermName = "Fall 2025" };
        var preview = new PercentRolloverPreviewDto
        {
            IsRolloverApplicable = true,
            SourceAcademicYear = 2025,
            TargetAcademicYear = 2026,
            SourceAcademicYearDisplay = "2024-2025",
            TargetAcademicYearDisplay = "2025-2026",
            Assignments =
            [
                new PercentRolloverItemPreview
                {
                    SourcePercentageId = 1,
                    PersonId = 123,
                    PersonName = "John Doe",
                    TypeName = "Administrative",
                    PercentageValue = 0.50
                }
            ],
            ExistingAssignments =
            [
                new PercentRolloverItemPreview
                {
                    SourcePercentageId = 2,
                    PersonId = 456,
                    PersonName = "Jane Smith",
                    TypeName = "Clinical",
                    PercentageValue = 0.25
                }
            ]
        };

        _termServiceMock.Setup(s => s.GetTermAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);
        _rolloverServiceMock.Setup(s => s.GetRolloverPreviewAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        var result = await _controller.GetPreview(202509, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPreview = Assert.IsType<PercentRolloverPreviewDto>(okResult.Value);
        Assert.Single(returnedPreview.Assignments);
        Assert.Single(returnedPreview.ExistingAssignments);
        Assert.Equal("Jane Smith", returnedPreview.ExistingAssignments[0].PersonName);
    }

    [Fact]
    public async Task Preview_ReturnsZeroAssignments_WhenAllRolled()
    {
        // Arrange - Fall Semester term (ends in 09)
        var term = new TermDto { TermCode = 202509, TermName = "Fall 2025" };
        var preview = new PercentRolloverPreviewDto
        {
            IsRolloverApplicable = true,
            SourceAcademicYear = 2025,
            TargetAcademicYear = 2026,
            SourceAcademicYearDisplay = "2024-2025",
            TargetAcademicYearDisplay = "2025-2026",
            Assignments = [],
            ExistingAssignments =
            [
                new PercentRolloverItemPreview
                {
                    SourcePercentageId = 1,
                    PersonId = 123,
                    PersonName = "John Doe",
                    TypeName = "Administrative",
                    PercentageValue = 0.50
                },
                new PercentRolloverItemPreview
                {
                    SourcePercentageId = 2,
                    PersonId = 456,
                    PersonName = "Jane Smith",
                    TypeName = "Clinical",
                    PercentageValue = 0.25
                }
            ]
        };

        _termServiceMock.Setup(s => s.GetTermAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);
        _rolloverServiceMock.Setup(s => s.GetRolloverPreviewAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        var result = await _controller.GetPreview(202509, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPreview = Assert.IsType<PercentRolloverPreviewDto>(okResult.Value);
        Assert.Empty(returnedPreview.Assignments);
        Assert.Equal(2, returnedPreview.ExistingAssignments.Count);
    }

    #endregion

    #region GetPreview Tests - Edge Cases

    [Fact]
    public async Task Preview_UsesTermCodeMath_ToDetectFallTerm()
    {
        // Arrange - Term code 202509 ends in 09 (Fall Semester)
        // Verify that term code math is used to detect Fall terms
        var term = new TermDto { TermCode = 202509, TermName = "Fall 2025" };
        var preview = CreateSamplePreview();

        _termServiceMock.Setup(s => s.GetTermAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);
        _rolloverServiceMock.Setup(s => s.GetRolloverPreviewAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        var result = await _controller.GetPreview(202509, CancellationToken.None);

        // Assert - OkResult because term code ends in 09 (Fall)
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Preview_ReturnsPreview_WithCorrectDateRanges()
    {
        // Arrange - Fall Semester term (ends in 09)
        var term = new TermDto { TermCode = 202509, TermName = "Fall 2025" };
        var preview = new PercentRolloverPreviewDto
        {
            IsRolloverApplicable = true,
            SourceAcademicYear = 2025,
            TargetAcademicYear = 2026,
            SourceAcademicYearDisplay = "2024-2025",
            TargetAcademicYearDisplay = "2025-2026",
            OldEndDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            NewStartDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local),
            NewEndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Local),
            Assignments = [],
            ExistingAssignments = []
        };

        _termServiceMock.Setup(s => s.GetTermAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);
        _rolloverServiceMock.Setup(s => s.GetRolloverPreviewAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        var result = await _controller.GetPreview(202509, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPreview = Assert.IsType<PercentRolloverPreviewDto>(okResult.Value);
        Assert.Equal(new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local), returnedPreview.OldEndDate);
        Assert.Equal(new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local), returnedPreview.NewStartDate);
        Assert.Equal(new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Local), returnedPreview.NewEndDate);
    }

    [Fact]
    public async Task Preview_CallsRolloverService_OnlyOnceForValidTerm()
    {
        // Arrange - Fall Semester term (ends in 09)
        var term = new TermDto { TermCode = 202509, TermName = "Fall 2025" };
        var preview = CreateSamplePreview();

        _termServiceMock.Setup(s => s.GetTermAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);
        _rolloverServiceMock.Setup(s => s.GetRolloverPreviewAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        await _controller.GetPreview(202509, CancellationToken.None);

        // Assert - verify service was called exactly once
        _rolloverServiceMock.Verify(
            s => s.GetRolloverPreviewAsync(202509, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Preview_DoesNotCallRolloverService_WhenTermNotFound()
    {
        // Arrange - Fall Semester term (ends in 09) but not found
        _termServiceMock.Setup(s => s.GetTermAsync(202509, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TermDto?)null);

        // Act
        await _controller.GetPreview(202509, CancellationToken.None);

        // Assert - verify service was never called
        _rolloverServiceMock.Verify(
            s => s.GetRolloverPreviewAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Preview_DoesNotCallRolloverService_WhenNotFallTerm()
    {
        // Arrange - Spring Semester term (ends in 02)
        var term = new TermDto { TermCode = 202502, TermName = "Spring 2025" };

        _termServiceMock.Setup(s => s.GetTermAsync(202502, It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);

        // Act
        await _controller.GetPreview(202502, CancellationToken.None);

        // Assert - verify service was never called
        _rolloverServiceMock.Verify(
            s => s.GetRolloverPreviewAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Helper Methods

    private static PercentRolloverPreviewDto CreateSamplePreview()
    {
        return new PercentRolloverPreviewDto
        {
            IsRolloverApplicable = true,
            SourceAcademicYear = 2025,
            TargetAcademicYear = 2026,
            SourceAcademicYearDisplay = "2024-2025",
            TargetAcademicYearDisplay = "2025-2026",
            OldEndDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            NewStartDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local),
            NewEndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Local),
            Assignments =
            [
                new PercentRolloverItemPreview
                {
                    SourcePercentageId = 1,
                    PersonId = 123,
                    PersonName = "Test Person",
                    TypeName = "Administrative",
                    PercentageValue = 0.50
                }
            ],
            ExistingAssignments = []
        };
    }

    #endregion
}
