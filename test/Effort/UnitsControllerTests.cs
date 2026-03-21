using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Viper.Areas.Effort.Controllers;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for UnitsController API endpoints.
/// </summary>
public sealed class UnitsControllerTests
{
    private readonly IUnitService _unitServiceMock;
    private readonly ILogger<UnitsController> _loggerMock;
    private readonly UnitsController _controller;

    public UnitsControllerTests()
    {
        _unitServiceMock = Substitute.For<IUnitService>();
        _loggerMock = Substitute.For<ILogger<UnitsController>>();

        _controller = new UnitsController(
            _unitServiceMock,
            _loggerMock);

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

    #region GetUnits Tests

    [Fact]
    public async Task GetUnits_ReturnsOk_WithUnitList()
    {
        // Arrange
        var units = new List<UnitDto>
        {
            new UnitDto { Id = 1, Name = "Unit A", IsActive = true, UsageCount = 0, CanDelete = true },
            new UnitDto { Id = 2, Name = "Unit B", IsActive = false, UsageCount = 5, CanDelete = false }
        };
        _unitServiceMock.GetUnitsAsync(false, Arg.Any<CancellationToken>()).Returns(units);

        // Act
        var result = await _controller.GetUnits();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUnits = Assert.IsAssignableFrom<IEnumerable<UnitDto>>(okResult.Value);
        Assert.Equal(2, returnedUnits.Count());
    }

    [Fact]
    public async Task GetUnits_PassesActiveOnlyFilter()
    {
        // Arrange
        var units = new List<UnitDto>
        {
            new UnitDto { Id = 1, Name = "Active Unit", IsActive = true, UsageCount = 0, CanDelete = true }
        };
        _unitServiceMock.GetUnitsAsync(true, Arg.Any<CancellationToken>()).Returns(units);

        // Act
        var result = await _controller.GetUnits(activeOnly: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUnits = Assert.IsAssignableFrom<IEnumerable<UnitDto>>(okResult.Value);
        Assert.Single(returnedUnits);
        await _unitServiceMock.Received(1).GetUnitsAsync(true, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetUnit Tests

    [Fact]
    public async Task GetUnit_ReturnsOk_WhenFound()
    {
        // Arrange
        var unit = new UnitDto { Id = 1, Name = "Test Unit", IsActive = true, UsageCount = 0, CanDelete = true };
        _unitServiceMock.GetUnitAsync(1, Arg.Any<CancellationToken>()).Returns(unit);

        // Act
        var result = await _controller.GetUnit(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUnit = Assert.IsType<UnitDto>(okResult.Value);
        Assert.Equal("Test Unit", returnedUnit.Name);
    }

    [Fact]
    public async Task GetUnit_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        _unitServiceMock.GetUnitAsync(999, Arg.Any<CancellationToken>()).Returns((UnitDto?)null);

        // Act
        var result = await _controller.GetUnit(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region CreateUnit Tests

    [Fact]
    public async Task CreateUnit_ReturnsCreated_OnSuccess()
    {
        // Arrange
        var request = new CreateUnitRequest { Name = "New Unit" };
        var createdUnit = new UnitDto { Id = 1, Name = "New Unit", IsActive = true, UsageCount = 0, CanDelete = true };
        _unitServiceMock.CreateUnitAsync(request, Arg.Any<CancellationToken>()).Returns(createdUnit);

        // Act
        var result = await _controller.CreateUnit(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetUnit", createdResult.ActionName);
        var returnedUnit = Assert.IsType<UnitDto>(createdResult.Value);
        Assert.Equal("New Unit", returnedUnit.Name);
    }

    [Fact]
    public async Task CreateUnit_ReturnsConflict_OnDuplicateName()
    {
        // Arrange
        var request = new CreateUnitRequest { Name = "Existing Unit" };
        _unitServiceMock.CreateUnitAsync(request, Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("A unit with name 'Existing Unit' already exists"));

        // Act
        var result = await _controller.CreateUnit(request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("already exists", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateUnit_ReturnsConflict_OnDbUpdateException()
    {
        // Arrange
        var request = new CreateUnitRequest { Name = "Race Condition Unit" };
        _unitServiceMock.CreateUnitAsync(request, Arg.Any<CancellationToken>()).Throws(new DbUpdateException("Constraint violation"));

        // Act
        var result = await _controller.CreateUnit(request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("constraint violation", conflictResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region UpdateUnit Tests

    [Fact]
    public async Task UpdateUnit_ReturnsOk_OnSuccess()
    {
        // Arrange
        var request = new UpdateUnitRequest { Name = "Updated Unit", IsActive = false };
        var updatedUnit = new UnitDto { Id = 1, Name = "Updated Unit", IsActive = false, UsageCount = 0, CanDelete = true };
        _unitServiceMock.UpdateUnitAsync(1, request, Arg.Any<CancellationToken>()).Returns(updatedUnit);

        // Act
        var result = await _controller.UpdateUnit(1, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUnit = Assert.IsType<UnitDto>(okResult.Value);
        Assert.Equal("Updated Unit", returnedUnit.Name);
        Assert.False(returnedUnit.IsActive);
    }

    [Fact]
    public async Task UpdateUnit_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var request = new UpdateUnitRequest { Name = "Updated Unit", IsActive = true };
        _unitServiceMock.UpdateUnitAsync(999, request, Arg.Any<CancellationToken>()).Returns((UnitDto?)null);

        // Act
        var result = await _controller.UpdateUnit(999, request, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateUnit_ReturnsConflict_OnDuplicateName()
    {
        // Arrange
        var request = new UpdateUnitRequest { Name = "Existing Unit", IsActive = true };
        _unitServiceMock.UpdateUnitAsync(1, request, Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("A unit with name 'Existing Unit' already exists"));

        // Act
        var result = await _controller.UpdateUnit(1, request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("already exists", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateUnit_ReturnsConflict_OnDbUpdateException()
    {
        // Arrange
        var request = new UpdateUnitRequest { Name = "Race Condition", IsActive = true };
        _unitServiceMock.UpdateUnitAsync(1, request, Arg.Any<CancellationToken>()).Throws(new DbUpdateException("Constraint violation"));

        // Act
        var result = await _controller.UpdateUnit(1, request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("constraint violation", conflictResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region DeleteUnit Tests

    [Fact]
    public async Task DeleteUnit_ReturnsNoContent_OnSuccess()
    {
        // Arrange
        _unitServiceMock.DeleteUnitAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _controller.DeleteUnit(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteUnit_ReturnsBadRequest_WhenInUse()
    {
        // Arrange
        _unitServiceMock.DeleteUnitAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _controller.DeleteUnit(1, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("cannot delete", badRequestResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteUnit_ReturnsBadRequest_OnDbUpdateException()
    {
        // Arrange
        _unitServiceMock.DeleteUnitAsync(1, Arg.Any<CancellationToken>()).Throws(new DbUpdateException("Foreign key constraint"));

        // Act
        var result = await _controller.DeleteUnit(1, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("related data", badRequestResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CanDeleteUnit Tests

    [Fact]
    public async Task CanDeleteUnit_ReturnsUsageInfo()
    {
        // Arrange
        _unitServiceMock.GetUsageCountAsync(1, Arg.Any<CancellationToken>()).Returns(5);

        // Act
        var result = await _controller.CanDeleteUnit(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CanDeleteResponse>(okResult.Value);

        Assert.False(response.CanDelete);
        Assert.Equal(5, response.UsageCount);
    }

    [Fact]
    public async Task CanDeleteUnit_ReturnsCanDeleteTrue_WhenZeroUsage()
    {
        // Arrange
        _unitServiceMock.GetUsageCountAsync(1, Arg.Any<CancellationToken>()).Returns(0);

        // Act
        var result = await _controller.CanDeleteUnit(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CanDeleteResponse>(okResult.Value);

        Assert.True(response.CanDelete);
        Assert.Equal(0, response.UsageCount);
    }

    #endregion
}
