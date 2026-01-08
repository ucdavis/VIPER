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
/// Unit tests for SessionTypesController API endpoints.
/// </summary>
public sealed class SessionTypesControllerTests
{
    private readonly Mock<ISessionTypeService> _sessionTypeServiceMock;
    private readonly Mock<ILogger<SessionTypesController>> _loggerMock;
    private readonly SessionTypesController _controller;

    public SessionTypesControllerTests()
    {
        _sessionTypeServiceMock = new Mock<ISessionTypeService>();
        _loggerMock = new Mock<ILogger<SessionTypesController>>();

        _controller = new SessionTypesController(
            _sessionTypeServiceMock.Object,
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

    #region GetSessionTypes Tests

    [Fact]
    public async Task GetSessionTypes_ReturnsOk_WithSessionTypeList()
    {
        // Arrange
        var sessionTypes = new List<SessionTypeDto>
        {
            new() { Id = "LEC", Description = "Lecture", IsActive = true, UsageCount = 0, CanDelete = true },
            new() { Id = "LAB", Description = "Laboratory", IsActive = true, UsageCount = 5, CanDelete = false }
        };
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionTypes);

        // Act
        var result = await _controller.GetSessionTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessionTypes = Assert.IsAssignableFrom<IEnumerable<SessionTypeDto>>(okResult.Value);
        Assert.Equal(2, returnedSessionTypes.Count());
    }

    [Fact]
    public async Task GetSessionTypes_PassesActiveOnlyFilter()
    {
        // Arrange
        var sessionTypes = new List<SessionTypeDto>
        {
            new() { Id = "ACT", Description = "Active Type", IsActive = true, UsageCount = 0, CanDelete = true }
        };
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionTypes);

        // Act
        var result = await _controller.GetSessionTypes(activeOnly: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessionTypes = Assert.IsAssignableFrom<IEnumerable<SessionTypeDto>>(okResult.Value);
        Assert.Single(returnedSessionTypes);
        _sessionTypeServiceMock.Verify(s => s.GetSessionTypesAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetSessionType Tests

    [Fact]
    public async Task GetSessionType_ReturnsOk_WhenFound()
    {
        // Arrange
        var sessionType = new SessionTypeDto { Id = "LEC", Description = "Lecture", IsActive = true, UsageCount = 0, CanDelete = true };
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypeAsync("LEC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionType);

        // Act
        var result = await _controller.GetSessionType("LEC", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessionType = Assert.IsType<SessionTypeDto>(okResult.Value);
        Assert.Equal("Lecture", returnedSessionType.Description);
    }

    [Fact]
    public async Task GetSessionType_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypeAsync("XXX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionTypeDto?)null);

        // Act
        var result = await _controller.GetSessionType("XXX", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region CreateSessionType Tests

    [Fact]
    public async Task CreateSessionType_ReturnsCreated_OnSuccess()
    {
        // Arrange
        var request = new CreateSessionTypeRequest { Id = "NEW", Description = "New Type" };
        var createdSessionType = new SessionTypeDto { Id = "NEW", Description = "New Type", IsActive = true, UsageCount = 0, CanDelete = true };
        _sessionTypeServiceMock.Setup(s => s.CreateSessionTypeAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdSessionType);

        // Act
        var result = await _controller.CreateSessionType(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetSessionType", createdResult.ActionName);
        var returnedSessionType = Assert.IsType<SessionTypeDto>(createdResult.Value);
        Assert.Equal("New Type", returnedSessionType.Description);
    }

    [Fact]
    public async Task CreateSessionType_ReturnsConflict_OnDuplicateId()
    {
        // Arrange
        var request = new CreateSessionTypeRequest { Id = "DUP", Description = "Duplicate Type" };
        _sessionTypeServiceMock.Setup(s => s.CreateSessionTypeAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("A session type with ID 'DUP' already exists"));

        // Act
        var result = await _controller.CreateSessionType(request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("already exists", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateSessionType_ReturnsConflict_OnDbUpdateException()
    {
        // Arrange
        var request = new CreateSessionTypeRequest { Id = "ERR", Description = "Error Type" };
        _sessionTypeServiceMock.Setup(s => s.CreateSessionTypeAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Constraint violation"));

        // Act
        var result = await _controller.CreateSessionType(request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("constraint violation", conflictResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region UpdateSessionType Tests

    [Fact]
    public async Task UpdateSessionType_ReturnsOk_OnSuccess()
    {
        // Arrange
        var request = new UpdateSessionTypeRequest { Description = "Updated Type", IsActive = false };
        var updatedSessionType = new SessionTypeDto { Id = "UPD", Description = "Updated Type", IsActive = false, UsageCount = 0, CanDelete = true };
        _sessionTypeServiceMock.Setup(s => s.UpdateSessionTypeAsync("UPD", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedSessionType);

        // Act
        var result = await _controller.UpdateSessionType("UPD", request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessionType = Assert.IsType<SessionTypeDto>(okResult.Value);
        Assert.Equal("Updated Type", returnedSessionType.Description);
        Assert.False(returnedSessionType.IsActive);
    }

    [Fact]
    public async Task UpdateSessionType_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var request = new UpdateSessionTypeRequest { Description = "Updated Type", IsActive = true };
        _sessionTypeServiceMock.Setup(s => s.UpdateSessionTypeAsync("XXX", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionTypeDto?)null);

        // Act
        var result = await _controller.UpdateSessionType("XXX", request, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateSessionType_ReturnsConflict_OnInvalidOperationException()
    {
        // Arrange
        var request = new UpdateSessionTypeRequest { Description = "Invalid Update", IsActive = true };
        _sessionTypeServiceMock.Setup(s => s.UpdateSessionTypeAsync("INV", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid update operation"));

        // Act
        var result = await _controller.UpdateSessionType("INV", request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("Invalid update operation", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateSessionType_ReturnsConflict_OnDbUpdateException()
    {
        // Arrange
        var request = new UpdateSessionTypeRequest { Description = "Error Update", IsActive = true };
        _sessionTypeServiceMock.Setup(s => s.UpdateSessionTypeAsync("ERR", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Constraint violation"));

        // Act
        var result = await _controller.UpdateSessionType("ERR", request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("constraint violation", conflictResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region DeleteSessionType Tests

    [Fact]
    public async Task DeleteSessionType_ReturnsNoContent_OnSuccess()
    {
        // Arrange
        var sessionType = new SessionTypeDto { Id = "DEL", Description = "Deletable", IsActive = true, UsageCount = 0, CanDelete = true };
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypeAsync("DEL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionType);
        _sessionTypeServiceMock.Setup(s => s.DeleteSessionTypeAsync("DEL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteSessionType("DEL", CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteSessionType_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypeAsync("XXX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionTypeDto?)null);

        // Act
        var result = await _controller.DeleteSessionType("XXX", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteSessionType_ReturnsConflict_WhenInUse()
    {
        // Arrange
        var sessionType = new SessionTypeDto { Id = "REF", Description = "Referenced", IsActive = true, UsageCount = 5, CanDelete = false };
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypeAsync("REF", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionType);

        // Act
        var result = await _controller.DeleteSessionType("REF", CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("cannot delete", conflictResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteSessionType_ReturnsConflict_WhenDeleteReturnsFalse()
    {
        // Arrange - session exists, canDelete=true, but delete returns false (race condition)
        var sessionType = new SessionTypeDto { Id = "RC", Description = "Race Condition", IsActive = true, UsageCount = 0, CanDelete = true };
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypeAsync("RC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionType);
        _sessionTypeServiceMock.Setup(s => s.DeleteSessionTypeAsync("RC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteSessionType("RC", CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("related data", conflictResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteSessionType_ReturnsConflict_OnDbUpdateException()
    {
        // Arrange
        var sessionType = new SessionTypeDto { Id = "ERR", Description = "Error Type", IsActive = true, UsageCount = 0, CanDelete = true };
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypeAsync("ERR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionType);
        _sessionTypeServiceMock.Setup(s => s.DeleteSessionTypeAsync("ERR", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Foreign key constraint"));

        // Act
        var result = await _controller.DeleteSessionType("ERR", CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("related data", conflictResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CanDeleteSessionType Tests

    [Fact]
    public async Task CanDeleteSessionType_ReturnsUsageInfo()
    {
        // Arrange
        var sessionType = new SessionTypeDto { Id = "USE", Description = "In Use Type", IsActive = true, UsageCount = 5, CanDelete = false };
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypeAsync("USE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionType);

        // Act
        var result = await _controller.CanDeleteSessionType("USE", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);

        var valueType = okResult.Value.GetType();
        var canDeleteProp = valueType.GetProperty("canDelete");
        var usageCountProp = valueType.GetProperty("usageCount");

        Assert.NotNull(canDeleteProp);
        Assert.NotNull(usageCountProp);
        Assert.False((bool)canDeleteProp.GetValue(okResult.Value)!);
        Assert.Equal(5, (int)usageCountProp.GetValue(okResult.Value)!);
    }

    [Fact]
    public async Task CanDeleteSessionType_ReturnsCanDeleteTrue_WhenZeroUsage()
    {
        // Arrange
        var sessionType = new SessionTypeDto { Id = "NEW", Description = "New Type", IsActive = true, UsageCount = 0, CanDelete = true };
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypeAsync("NEW", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionType);

        // Act
        var result = await _controller.CanDeleteSessionType("NEW", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);

        var valueType = okResult.Value.GetType();
        var canDeleteProp = valueType.GetProperty("canDelete");
        Assert.True((bool)canDeleteProp!.GetValue(okResult.Value)!);
    }

    [Fact]
    public async Task CanDeleteSessionType_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypeAsync("XXX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionTypeDto?)null);

        // Act
        var result = await _controller.CanDeleteSessionType("XXX", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Empty/Whitespace ID Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSessionType_ReturnsNotFound_WhenIdIsEmptyOrWhitespace(string id)
    {
        // Arrange
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypeAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionTypeDto?)null);

        // Act
        var result = await _controller.GetSessionType(id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateSessionType_ReturnsNotFound_WhenIdIsEmptyOrWhitespace(string id)
    {
        // Arrange
        var request = new UpdateSessionTypeRequest { Description = "Test", IsActive = true };
        _sessionTypeServiceMock.Setup(s => s.UpdateSessionTypeAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionTypeDto?)null);

        // Act
        var result = await _controller.UpdateSessionType(id, request, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteSessionType_ReturnsNotFound_WhenIdIsEmptyOrWhitespace(string id)
    {
        // Arrange
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypeAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionTypeDto?)null);

        // Act
        var result = await _controller.DeleteSessionType(id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CanDeleteSessionType_ReturnsNotFound_WhenIdIsEmptyOrWhitespace(string id)
    {
        // Arrange
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypeAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionTypeDto?)null);

        // Act
        var result = await _controller.CanDeleteSessionType(id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region GetSessionTypes Empty Result Tests

    [Fact]
    public async Task GetSessionTypes_ReturnsOk_WithEmptyList()
    {
        // Arrange
        _sessionTypeServiceMock.Setup(s => s.GetSessionTypesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SessionTypeDto>());

        // Act
        var result = await _controller.GetSessionTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessionTypes = Assert.IsAssignableFrom<IEnumerable<SessionTypeDto>>(okResult.Value);
        Assert.Empty(returnedSessionTypes);
    }

    #endregion
}
