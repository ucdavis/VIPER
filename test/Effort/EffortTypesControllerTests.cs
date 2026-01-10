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
/// Unit tests for EffortTypesController API endpoints.
/// </summary>
public sealed class EffortTypesControllerTests
{
    private readonly Mock<IEffortTypeService> _effortTypeServiceMock;
    private readonly Mock<ILogger<EffortTypesController>> _loggerMock;
    private readonly EffortTypesController _controller;

    public EffortTypesControllerTests()
    {
        _effortTypeServiceMock = new Mock<IEffortTypeService>();
        _loggerMock = new Mock<ILogger<EffortTypesController>>();

        _controller = new EffortTypesController(
            _effortTypeServiceMock.Object,
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

    #region GetEffortTypes Tests

    [Fact]
    public async Task GetEffortTypes_ReturnsOk_WithEffortTypeList()
    {
        // Arrange
        var effortTypes = new List<EffortTypeDto>
        {
            new() { Id = "LEC", Description = "Lecture", IsActive = true, UsageCount = 0, CanDelete = true },
            new() { Id = "LAB", Description = "Laboratory", IsActive = true, UsageCount = 5, CanDelete = false }
        };
        _effortTypeServiceMock.Setup(s => s.GetEffortTypesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(effortTypes);

        // Act
        var result = await _controller.GetEffortTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEffortTypes = Assert.IsAssignableFrom<IEnumerable<EffortTypeDto>>(okResult.Value);
        Assert.Equal(2, returnedEffortTypes.Count());
    }

    [Fact]
    public async Task GetEffortTypes_PassesActiveOnlyFilter()
    {
        // Arrange
        var effortTypes = new List<EffortTypeDto>
        {
            new() { Id = "ACT", Description = "Active Type", IsActive = true, UsageCount = 0, CanDelete = true }
        };
        _effortTypeServiceMock.Setup(s => s.GetEffortTypesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(effortTypes);

        // Act
        var result = await _controller.GetEffortTypes(activeOnly: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEffortTypes = Assert.IsAssignableFrom<IEnumerable<EffortTypeDto>>(okResult.Value);
        Assert.Single(returnedEffortTypes);
        _effortTypeServiceMock.Verify(s => s.GetEffortTypesAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetEffortType Tests

    [Fact]
    public async Task GetEffortType_ReturnsOk_WhenFound()
    {
        // Arrange
        var effortType = new EffortTypeDto { Id = "LEC", Description = "Lecture", IsActive = true, UsageCount = 0, CanDelete = true };
        _effortTypeServiceMock.Setup(s => s.GetEffortTypeAsync("LEC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(effortType);

        // Act
        var result = await _controller.GetEffortType("LEC", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEffortType = Assert.IsType<EffortTypeDto>(okResult.Value);
        Assert.Equal("Lecture", returnedEffortType.Description);
    }

    [Fact]
    public async Task GetEffortType_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        _effortTypeServiceMock.Setup(s => s.GetEffortTypeAsync("XXX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EffortTypeDto?)null);

        // Act
        var result = await _controller.GetEffortType("XXX", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region CreateEffortType Tests

    [Fact]
    public async Task CreateEffortType_ReturnsCreated_OnSuccess()
    {
        // Arrange
        var request = new CreateEffortTypeRequest { Id = "NEW", Description = "New Type" };
        var createdEffortType = new EffortTypeDto { Id = "NEW", Description = "New Type", IsActive = true, UsageCount = 0, CanDelete = true };
        _effortTypeServiceMock.Setup(s => s.CreateEffortTypeAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEffortType);

        // Act
        var result = await _controller.CreateEffortType(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetEffortType", createdResult.ActionName);
        var returnedEffortType = Assert.IsType<EffortTypeDto>(createdResult.Value);
        Assert.Equal("New Type", returnedEffortType.Description);
    }

    [Fact]
    public async Task CreateEffortType_ReturnsConflict_OnDuplicateId()
    {
        // Arrange
        var request = new CreateEffortTypeRequest { Id = "DUP", Description = "Duplicate Type" };
        _effortTypeServiceMock.Setup(s => s.CreateEffortTypeAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("A effort type with ID 'DUP' already exists"));

        // Act
        var result = await _controller.CreateEffortType(request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("already exists", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateEffortType_ReturnsConflict_OnDbUpdateException()
    {
        // Arrange
        var request = new CreateEffortTypeRequest { Id = "ERR", Description = "Error Type" };
        _effortTypeServiceMock.Setup(s => s.CreateEffortTypeAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Constraint violation"));

        // Act
        var result = await _controller.CreateEffortType(request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("constraint violation", conflictResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region UpdateEffortType Tests

    [Fact]
    public async Task UpdateEffortType_ReturnsOk_OnSuccess()
    {
        // Arrange
        var request = new UpdateEffortTypeRequest { Description = "Updated Type", IsActive = false };
        var updatedEffortType = new EffortTypeDto { Id = "UPD", Description = "Updated Type", IsActive = false, UsageCount = 0, CanDelete = true };
        _effortTypeServiceMock.Setup(s => s.UpdateEffortTypeAsync("UPD", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedEffortType);

        // Act
        var result = await _controller.UpdateEffortType("UPD", request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEffortType = Assert.IsType<EffortTypeDto>(okResult.Value);
        Assert.Equal("Updated Type", returnedEffortType.Description);
        Assert.False(returnedEffortType.IsActive);
    }

    [Fact]
    public async Task UpdateEffortType_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var request = new UpdateEffortTypeRequest { Description = "Updated Type", IsActive = true };
        _effortTypeServiceMock.Setup(s => s.UpdateEffortTypeAsync("XXX", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EffortTypeDto?)null);

        // Act
        var result = await _controller.UpdateEffortType("XXX", request, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateEffortType_ReturnsConflict_OnInvalidOperationException()
    {
        // Arrange
        var request = new UpdateEffortTypeRequest { Description = "Invalid Update", IsActive = true };
        _effortTypeServiceMock.Setup(s => s.UpdateEffortTypeAsync("INV", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid update operation"));

        // Act
        var result = await _controller.UpdateEffortType("INV", request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("Invalid update operation", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateEffortType_ReturnsConflict_OnDbUpdateException()
    {
        // Arrange
        var request = new UpdateEffortTypeRequest { Description = "Error Update", IsActive = true };
        _effortTypeServiceMock.Setup(s => s.UpdateEffortTypeAsync("ERR", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Constraint violation"));

        // Act
        var result = await _controller.UpdateEffortType("ERR", request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("constraint violation", conflictResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region DeleteEffortType Tests

    [Fact]
    public async Task DeleteEffortType_ReturnsNoContent_OnSuccess()
    {
        // Arrange
        var effortType = new EffortTypeDto { Id = "DEL", Description = "Deletable", IsActive = true, UsageCount = 0, CanDelete = true };
        _effortTypeServiceMock.Setup(s => s.GetEffortTypeAsync("DEL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(effortType);
        _effortTypeServiceMock.Setup(s => s.DeleteEffortTypeAsync("DEL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteEffortType("DEL", CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteEffortType_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        _effortTypeServiceMock.Setup(s => s.GetEffortTypeAsync("XXX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EffortTypeDto?)null);

        // Act
        var result = await _controller.DeleteEffortType("XXX", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteEffortType_ReturnsConflict_WhenInUse()
    {
        // Arrange
        var effortType = new EffortTypeDto { Id = "REF", Description = "Referenced", IsActive = true, UsageCount = 5, CanDelete = false };
        _effortTypeServiceMock.Setup(s => s.GetEffortTypeAsync("REF", It.IsAny<CancellationToken>()))
            .ReturnsAsync(effortType);

        // Act
        var result = await _controller.DeleteEffortType("REF", CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("cannot delete", conflictResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteEffortType_ReturnsConflict_WhenDeleteReturnsFalse()
    {
        // Arrange - session exists, canDelete=true, but delete returns false (race condition)
        var effortType = new EffortTypeDto { Id = "RC", Description = "Race Condition", IsActive = true, UsageCount = 0, CanDelete = true };
        _effortTypeServiceMock.Setup(s => s.GetEffortTypeAsync("RC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(effortType);
        _effortTypeServiceMock.Setup(s => s.DeleteEffortTypeAsync("RC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteEffortType("RC", CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("related data", conflictResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteEffortType_ReturnsConflict_OnDbUpdateException()
    {
        // Arrange
        var effortType = new EffortTypeDto { Id = "ERR", Description = "Error Type", IsActive = true, UsageCount = 0, CanDelete = true };
        _effortTypeServiceMock.Setup(s => s.GetEffortTypeAsync("ERR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(effortType);
        _effortTypeServiceMock.Setup(s => s.DeleteEffortTypeAsync("ERR", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Foreign key constraint"));

        // Act
        var result = await _controller.DeleteEffortType("ERR", CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("related data", conflictResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CanDeleteEffortType Tests

    [Fact]
    public async Task CanDeleteEffortType_ReturnsUsageInfo()
    {
        // Arrange
        var effortType = new EffortTypeDto { Id = "USE", Description = "In Use Type", IsActive = true, UsageCount = 5, CanDelete = false };
        _effortTypeServiceMock.Setup(s => s.GetEffortTypeAsync("USE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(effortType);

        // Act
        var result = await _controller.CanDeleteEffortType("USE", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CanDeleteResponse>(okResult.Value);

        Assert.False(response.CanDelete);
        Assert.Equal(5, response.UsageCount);
    }

    [Fact]
    public async Task CanDeleteEffortType_ReturnsCanDeleteTrue_WhenZeroUsage()
    {
        // Arrange
        var effortType = new EffortTypeDto { Id = "NEW", Description = "New Type", IsActive = true, UsageCount = 0, CanDelete = true };
        _effortTypeServiceMock.Setup(s => s.GetEffortTypeAsync("NEW", It.IsAny<CancellationToken>()))
            .ReturnsAsync(effortType);

        // Act
        var result = await _controller.CanDeleteEffortType("NEW", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CanDeleteResponse>(okResult.Value);

        Assert.True(response.CanDelete);
        Assert.Equal(0, response.UsageCount);
    }

    [Fact]
    public async Task CanDeleteEffortType_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        _effortTypeServiceMock.Setup(s => s.GetEffortTypeAsync("XXX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EffortTypeDto?)null);

        // Act
        var result = await _controller.CanDeleteEffortType("XXX", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Empty/Whitespace ID Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetEffortType_ReturnsBadRequest_WhenIdIsEmptyOrWhitespace(string id)
    {
        // Act
        var result = await _controller.GetEffortType(id, CancellationToken.None);

        // Assert - BadRequest for invalid input, not NotFound
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateEffortType_ReturnsBadRequest_WhenIdIsEmptyOrWhitespace(string id)
    {
        // Arrange
        var request = new UpdateEffortTypeRequest { Description = "Test", IsActive = true };

        // Act
        var result = await _controller.UpdateEffortType(id, request, CancellationToken.None);

        // Assert - BadRequest for invalid input, not NotFound
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteEffortType_ReturnsBadRequest_WhenIdIsEmptyOrWhitespace(string id)
    {
        // Act
        var result = await _controller.DeleteEffortType(id, CancellationToken.None);

        // Assert - BadRequest for invalid input, not NotFound
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CanDeleteEffortType_ReturnsBadRequest_WhenIdIsEmptyOrWhitespace(string id)
    {
        // Act
        var result = await _controller.CanDeleteEffortType(id, CancellationToken.None);

        // Assert - BadRequest for invalid input, not NotFound
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region GetEffortTypes Empty Result Tests

    [Fact]
    public async Task GetEffortTypes_ReturnsOk_WithEmptyList()
    {
        // Arrange
        _effortTypeServiceMock.Setup(s => s.GetEffortTypesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EffortTypeDto>());

        // Act
        var result = await _controller.GetEffortTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEffortTypes = Assert.IsAssignableFrom<IEnumerable<EffortTypeDto>>(okResult.Value);
        Assert.Empty(returnedEffortTypes);
    }

    #endregion
}
