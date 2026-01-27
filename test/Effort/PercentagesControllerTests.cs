using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort.Controllers;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for PercentagesController API endpoints.
/// </summary>
public sealed class PercentagesControllerTests
{
    private readonly Mock<IPercentageService> _percentageServiceMock;
    private readonly Mock<IEffortPermissionService> _permissionServiceMock;
    private readonly Mock<ILogger<PercentagesController>> _loggerMock;
    private readonly PercentagesController _controller;

    private const int TestPercentageId = 1;
    private const int TestPersonId = 100;

    public PercentagesControllerTests()
    {
        _percentageServiceMock = new Mock<IPercentageService>();
        _permissionServiceMock = new Mock<IEffortPermissionService>();
        _loggerMock = new Mock<ILogger<PercentagesController>>();

        _controller = new PercentagesController(
            _percentageServiceMock.Object,
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

    private static PercentageDto CreateTestPercentage(int id = TestPercentageId) => new()
    {
        Id = id,
        PersonId = TestPersonId,
        PercentAssignTypeId = 1,
        TypeName = "Teaching",
        TypeClass = "Other",
        UnitId = 1,
        UnitName = "VME",
        Modifier = null,
        Comment = null,
        PercentageValue = 50,
        StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate = null,
        Compensated = false,
        IsActive = true,
        ModifiedDate = DateTime.UtcNow
    };

    private static CreatePercentageRequest CreateTestCreateRequest() => new()
    {
        PersonId = TestPersonId,
        PercentAssignTypeId = 1,
        UnitId = 1,
        Modifier = null,
        Comment = null,
        PercentageValue = 50,
        StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate = null,
        Compensated = false
    };

    private static UpdatePercentageRequest CreateTestUpdateRequest() => new()
    {
        PercentAssignTypeId = 1,
        UnitId = 1,
        Modifier = null,
        Comment = "Updated",
        PercentageValue = 60,
        StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate = null,
        Compensated = false
    };

    #region GetPercentagesForPerson Tests

    [Fact]
    public async Task GetPercentagesForPerson_ReturnsOk_WhenAuthorized()
    {
        // Arrange
        var percentages = new List<PercentageDto> { CreateTestPercentage() };
        _permissionServiceMock.Setup(s => s.CanViewPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.GetPercentagesForPersonAsync(TestPersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(percentages);

        // Act
        var result = await _controller.GetPercentagesForPerson(TestPersonId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPercentages = Assert.IsType<List<PercentageDto>>(okResult.Value);
        Assert.Single(returnedPercentages);
        Assert.Equal(TestPercentageId, returnedPercentages[0].Id);
    }

    [Fact]
    public async Task GetPercentagesForPerson_ReturnsNotFound_WhenUserNotAuthorized()
    {
        // Arrange
        _permissionServiceMock.Setup(s => s.CanViewPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetPercentagesForPerson(TestPersonId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region GetPercentage Tests

    [Fact]
    public async Task GetPercentage_ReturnsOk_WhenPercentageExistsAndAuthorized()
    {
        // Arrange
        var percentage = CreateTestPercentage();
        _percentageServiceMock.Setup(s => s.GetPercentageAsync(TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(percentage);
        _permissionServiceMock.Setup(s => s.CanViewPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.GetPercentage(TestPercentageId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPercentage = Assert.IsType<PercentageDto>(okResult.Value);
        Assert.Equal(TestPercentageId, returnedPercentage.Id);
    }

    [Fact]
    public async Task GetPercentage_ReturnsNotFound_WhenPercentageDoesNotExist()
    {
        // Arrange
        _percentageServiceMock.Setup(s => s.GetPercentageAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PercentageDto?)null);

        // Act
        var result = await _controller.GetPercentage(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetPercentage_ReturnsNotFound_WhenUserNotAuthorized()
    {
        // Arrange
        var percentage = CreateTestPercentage();
        _percentageServiceMock.Setup(s => s.GetPercentageAsync(TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(percentage);
        _permissionServiceMock.Setup(s => s.CanViewPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetPercentage(TestPercentageId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region CreatePercentage Tests

    [Fact]
    public async Task CreatePercentage_ReturnsCreatedAtAction_WhenSuccessful()
    {
        // Arrange
        var request = CreateTestCreateRequest();
        var percentage = CreateTestPercentage();
        var validationResult = new PercentageValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = []
        };

        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.ValidatePercentageAsync(request, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _percentageServiceMock.Setup(s => s.CreatePercentageAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(percentage);

        // Act
        var result = await _controller.CreatePercentage(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetPercentage), createdResult.ActionName);
    }

    [Fact]
    public async Task CreatePercentage_ReturnsNotFound_WhenUserNotAuthorizedForPerson()
    {
        // Arrange
        var request = CreateTestCreateRequest();

        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CreatePercentage(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreatePercentage_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var request = CreateTestCreateRequest();
        var validationResult = new PercentageValidationResult
        {
            IsValid = false,
            Errors = ["Start date must be before end date"],
            Warnings = []
        };

        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.ValidatePercentageAsync(request, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.CreatePercentage(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreatePercentage_ReturnsCreatedWithWarnings_WhenValidationHasWarnings()
    {
        // Arrange
        var request = CreateTestCreateRequest();
        var percentage = CreateTestPercentage();
        var validationResult = new PercentageValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = ["Total percentage exceeds 100%"]
        };

        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.ValidatePercentageAsync(request, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _percentageServiceMock.Setup(s => s.CreatePercentageAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(percentage);

        // Act
        var result = await _controller.CreatePercentage(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(createdResult.Value);
    }

    [Fact]
    public async Task CreatePercentage_ReturnsBadRequest_WhenInvalidOperationException()
    {
        // Arrange
        var request = CreateTestCreateRequest();
        var validationResult = new PercentageValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = []
        };

        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.ValidatePercentageAsync(request, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _percentageServiceMock.Setup(s => s.CreatePercentageAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate percentage assignment"));

        // Act
        var result = await _controller.CreatePercentage(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Duplicate percentage assignment", badRequest.Value);
    }

    [Fact]
    public async Task CreatePercentage_ReturnsBadRequest_WhenDbUpdateException()
    {
        // Arrange
        var request = CreateTestCreateRequest();
        var validationResult = new PercentageValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = []
        };

        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.ValidatePercentageAsync(request, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _percentageServiceMock.Setup(s => s.CreatePercentageAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("DB error"));

        // Act
        var result = await _controller.CreatePercentage(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Failed to create", badRequest.Value?.ToString());
    }

    #endregion

    #region ValidatePercentage Tests

    [Fact]
    public async Task ValidatePercentage_ReturnsOk_WhenAuthorized()
    {
        // Arrange
        var request = CreateTestCreateRequest();
        var validationResult = new PercentageValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = [],
            TotalActivePercent = 50
        };

        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.ValidatePercentageAsync(request, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ValidatePercentage(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedValidation = Assert.IsType<PercentageValidationResult>(okResult.Value);
        Assert.True(returnedValidation.IsValid);
    }

    [Fact]
    public async Task ValidatePercentage_ReturnsNotFound_WhenUserNotAuthorized()
    {
        // Arrange
        var request = CreateTestCreateRequest();

        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ValidatePercentage(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region UpdatePercentage Tests

    [Fact]
    public async Task UpdatePercentage_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var existingPercentage = CreateTestPercentage();
        var request = CreateTestUpdateRequest();
        var updatedPercentage = CreateTestPercentage();
        updatedPercentage.Comment = "Updated";
        updatedPercentage.PercentageValue = 60;

        var validationResult = new PercentageValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = []
        };

        _percentageServiceMock.Setup(s => s.GetPercentageAsync(TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPercentage);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.ValidatePercentageAsync(It.IsAny<CreatePercentageRequest>(), TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _percentageServiceMock.Setup(s => s.UpdatePercentageAsync(TestPercentageId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedPercentage);

        // Act
        var result = await _controller.UpdatePercentage(TestPercentageId, request);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePercentage_ReturnsNotFound_WhenPercentageDoesNotExist()
    {
        // Arrange
        var request = CreateTestUpdateRequest();

        _percentageServiceMock.Setup(s => s.GetPercentageAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PercentageDto?)null);

        // Act
        var result = await _controller.UpdatePercentage(999, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePercentage_ReturnsNotFound_WhenUserNotAuthorized()
    {
        // Arrange
        var existingPercentage = CreateTestPercentage();
        var request = CreateTestUpdateRequest();

        _percentageServiceMock.Setup(s => s.GetPercentageAsync(TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPercentage);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdatePercentage(TestPercentageId, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePercentage_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var existingPercentage = CreateTestPercentage();
        var request = CreateTestUpdateRequest();
        var validationResult = new PercentageValidationResult
        {
            IsValid = false,
            Errors = ["Invalid percentage value"],
            Warnings = []
        };

        _percentageServiceMock.Setup(s => s.GetPercentageAsync(TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPercentage);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.ValidatePercentageAsync(It.IsAny<CreatePercentageRequest>(), TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.UpdatePercentage(TestPercentageId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePercentage_ReturnsConflict_WhenConcurrencyConflict()
    {
        // Arrange
        var existingPercentage = CreateTestPercentage();
        var request = CreateTestUpdateRequest();
        var validationResult = new PercentageValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = []
        };

        _percentageServiceMock.Setup(s => s.GetPercentageAsync(TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPercentage);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.ValidatePercentageAsync(It.IsAny<CreatePercentageRequest>(), TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _percentageServiceMock.Setup(s => s.UpdatePercentageAsync(TestPercentageId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("The record has been modified by another user. Please refresh and try again."));

        // Act
        var result = await _controller.UpdatePercentage(TestPercentageId, request);

        // Assert
        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePercentage_ReturnsBadRequest_WhenInvalidOperationException()
    {
        // Arrange
        var existingPercentage = CreateTestPercentage();
        var request = CreateTestUpdateRequest();
        var validationResult = new PercentageValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = []
        };

        _percentageServiceMock.Setup(s => s.GetPercentageAsync(TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPercentage);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.ValidatePercentageAsync(It.IsAny<CreatePercentageRequest>(), TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _percentageServiceMock.Setup(s => s.UpdatePercentageAsync(TestPercentageId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Some other error"));

        // Act
        var result = await _controller.UpdatePercentage(TestPercentageId, request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Some other error", badRequest.Value);
    }

    [Fact]
    public async Task UpdatePercentage_ReturnsBadRequest_WhenDbUpdateException()
    {
        // Arrange
        var existingPercentage = CreateTestPercentage();
        var request = CreateTestUpdateRequest();
        var validationResult = new PercentageValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = []
        };

        _percentageServiceMock.Setup(s => s.GetPercentageAsync(TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPercentage);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.ValidatePercentageAsync(It.IsAny<CreatePercentageRequest>(), TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _percentageServiceMock.Setup(s => s.UpdatePercentageAsync(TestPercentageId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("DB error"));

        // Act
        var result = await _controller.UpdatePercentage(TestPercentageId, request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Failed to update", badRequest.Value?.ToString());
    }

    #endregion

    #region DeletePercentage Tests

    [Fact]
    public async Task DeletePercentage_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var existingPercentage = CreateTestPercentage();

        _percentageServiceMock.Setup(s => s.GetPercentageAsync(TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPercentage);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.DeletePercentageAsync(TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeletePercentage(TestPercentageId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeletePercentage_ReturnsNotFound_WhenPercentageDoesNotExist()
    {
        // Arrange
        _percentageServiceMock.Setup(s => s.GetPercentageAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PercentageDto?)null);

        // Act
        var result = await _controller.DeletePercentage(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeletePercentage_ReturnsNotFound_WhenUserNotAuthorized()
    {
        // Arrange
        var existingPercentage = CreateTestPercentage();

        _percentageServiceMock.Setup(s => s.GetPercentageAsync(TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPercentage);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeletePercentage(TestPercentageId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeletePercentage_ReturnsNotFound_WhenDeleteFails()
    {
        // Arrange
        var existingPercentage = CreateTestPercentage();

        _percentageServiceMock.Setup(s => s.GetPercentageAsync(TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPercentage);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.DeletePercentageAsync(TestPercentageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeletePercentage(TestPercentageId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region GetAverages Tests

    [Fact]
    public async Task GetAverages_ReturnsOk_WhenAuthorized()
    {
        // Arrange
        var start = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var averages = new Dictionary<string, List<AveragePercentByTypeDto>>
        {
            ["Other"] = new List<AveragePercentByTypeDto>
            {
                new()
                {
                    TypeClass = "Other",
                    TypeName = "Teaching",
                    AcademicYear = "2024-2025",
                    AveragedPercent = 50,
                    AveragedPercentDisplay = "50%"
                }
            }
        };

        _permissionServiceMock.Setup(s => s.CanViewPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _percentageServiceMock.Setup(s => s.GetAveragePercentsByTypeAsync(TestPersonId, start, end, It.IsAny<CancellationToken>()))
            .ReturnsAsync(averages);

        // Act
        var result = await _controller.GetAverages(TestPersonId, start, end);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAverages = Assert.IsType<Dictionary<string, List<AveragePercentByTypeDto>>>(okResult.Value);
        Assert.Single(returnedAverages);
        Assert.Single(returnedAverages["Other"]);
    }

    [Fact]
    public async Task GetAverages_ReturnsNotFound_WhenUserNotAuthorized()
    {
        // Arrange
        var start = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc);

        _permissionServiceMock.Setup(s => s.CanViewPersonEffortAsync(TestPersonId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetAverages(TestPersonId, start, end);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion
}
