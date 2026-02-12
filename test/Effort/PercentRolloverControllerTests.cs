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
    private readonly Mock<IEffortPermissionService> _permissionServiceMock;
    private readonly Mock<ILogger<PercentRolloverController>> _loggerMock;
    private readonly PercentRolloverController _controller;

    public PercentRolloverControllerTests()
    {
        _rolloverServiceMock = new Mock<IPercentRolloverService>();
        _permissionServiceMock = new Mock<IEffortPermissionService>();
        _loggerMock = new Mock<ILogger<PercentRolloverController>>();

        _controller = new PercentRolloverController(
            _rolloverServiceMock.Object,
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
    public async Task Preview_ReturnsBadRequest_WhenYearTooLow()
    {
        // Arrange & Act
        var result = await _controller.GetPreview(2019, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("2020", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task Preview_ReturnsBadRequest_WhenYearInFuture()
    {
        // Arrange & Act
        var futureYear = DateTime.Now.Year + 1;
        var result = await _controller.GetPreview(futureYear, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Preview_DoesNotCallRolloverService_WhenYearInvalid()
    {
        // Arrange & Act
        await _controller.GetPreview(2019, CancellationToken.None);

        // Assert
        _rolloverServiceMock.Verify(
            s => s.GetRolloverPreviewAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region GetPreview Tests - Success Cases

    [Fact]
    public async Task Preview_ReturnsPreview_ForValidYear()
    {
        // Arrange
        var preview = CreateSamplePreview();
        _rolloverServiceMock.Setup(s => s.GetRolloverPreviewAsync(2025, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        var result = await _controller.GetPreview(2025, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPreview = Assert.IsType<PercentRolloverPreviewDto>(okResult.Value);
        Assert.True(returnedPreview.IsRolloverApplicable);
        Assert.Equal(2025, returnedPreview.SourceAcademicYear);
        Assert.Equal(2026, returnedPreview.TargetAcademicYear);
    }

    [Fact]
    public async Task Preview_IncludesAssignments_WhenPendingRollover()
    {
        // Arrange
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

        _rolloverServiceMock.Setup(s => s.GetRolloverPreviewAsync(2025, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        var result = await _controller.GetPreview(2025, CancellationToken.None);

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
        // Arrange
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

        _rolloverServiceMock.Setup(s => s.GetRolloverPreviewAsync(2025, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        var result = await _controller.GetPreview(2025, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPreview = Assert.IsType<PercentRolloverPreviewDto>(okResult.Value);
        Assert.Single(returnedPreview.Assignments);
        Assert.Single(returnedPreview.ExistingAssignments);
        Assert.Equal("Jane Smith", returnedPreview.ExistingAssignments[0].PersonName);
    }

    [Fact]
    public async Task Preview_CallsRolloverService_OnlyOnceForValidYear()
    {
        // Arrange
        var preview = CreateSamplePreview();
        _rolloverServiceMock.Setup(s => s.GetRolloverPreviewAsync(2025, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        await _controller.GetPreview(2025, CancellationToken.None);

        // Assert
        _rolloverServiceMock.Verify(
            s => s.GetRolloverPreviewAsync(2025, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Preview_ReturnsPreview_WithCorrectDateRanges()
    {
        // Arrange
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

        _rolloverServiceMock.Setup(s => s.GetRolloverPreviewAsync(2025, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        var result = await _controller.GetPreview(2025, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPreview = Assert.IsType<PercentRolloverPreviewDto>(okResult.Value);
        Assert.Equal(new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local), returnedPreview.OldEndDate);
        Assert.Equal(new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local), returnedPreview.NewStartDate);
        Assert.Equal(new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Local), returnedPreview.NewEndDate);
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
