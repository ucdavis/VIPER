using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Models;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for UnitService unit management operations.
/// </summary>
public sealed class UnitServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly IMapper _mapper;
    private readonly UnitService _unitService;

    public UnitServiceTests()
    {
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EffortDbContext(effortOptions);
        _auditServiceMock = new Mock<IEffortAuditService>();

        // Configure AutoMapper with the Effort profile
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperProfileEffort>();
        });
        _mapper = mapperConfig.CreateMapper();

        // Setup synchronous audit methods used within transactions
        _auditServiceMock
            .Setup(s => s.AddUnitChangeAudit(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>()));

        _unitService = new UnitService(_context, _auditServiceMock.Object, _mapper);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Helper Methods

    private async Task<Unit> CreateUnitWithPercentagesAsync(string name, int percentageCount = 1)
    {
        var unit = new Unit { Name = name, IsActive = true };
        var percentAssignType = await GetOrCreatePercentAssignTypeAsync();
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        for (int i = 0; i < percentageCount; i++)
        {
            _context.Percentages.Add(new Percentage
            {
                UnitId = unit.Id,
                PercentAssignTypeId = percentAssignType.Id,
                PercentageValue = 50,
                PersonId = 1,
                AcademicYear = "2024-25",
                StartDate = DateTime.Today
            });
        }
        await _context.SaveChangesAsync();
        return unit;
    }

    private async Task<PercentAssignType> GetOrCreatePercentAssignTypeAsync()
    {
        var percentAssignType = await _context.PercentAssignTypes.FirstOrDefaultAsync();
        if (percentAssignType == null)
        {
            percentAssignType = new PercentAssignType { Name = "Test Type", Class = "Test" };
            _context.PercentAssignTypes.Add(percentAssignType);
            await _context.SaveChangesAsync();
        }
        return percentAssignType;
    }

    #endregion

    #region GetUnitsAsync Tests

    [Fact]
    public async Task GetUnitsAsync_ReturnsAllUnits_OrderedByName()
    {
        // Arrange
        _context.Units.AddRange(
            new Unit { Id = 1, Name = "Zebra Unit", IsActive = true },
            new Unit { Id = 2, Name = "Alpha Unit", IsActive = true },
            new Unit { Id = 3, Name = "Middle Unit", IsActive = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var units = await _unitService.GetUnitsAsync();

        // Assert
        Assert.Equal(3, units.Count);
        Assert.Equal("Alpha Unit", units[0].Name);
        Assert.Equal("Middle Unit", units[1].Name);
        Assert.Equal("Zebra Unit", units[2].Name);
    }

    [Fact]
    public async Task GetUnitsAsync_FiltersActiveOnly_WhenActiveOnlyIsTrue()
    {
        // Arrange
        _context.Units.AddRange(
            new Unit { Id = 1, Name = "Active Unit", IsActive = true },
            new Unit { Id = 2, Name = "Inactive Unit", IsActive = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var units = await _unitService.GetUnitsAsync(activeOnly: true);

        // Assert
        Assert.Single(units);
        Assert.Equal("Active Unit", units[0].Name);
        Assert.True(units[0].IsActive);
    }

    [Fact]
    public async Task GetUnitsAsync_ReturnsEmptyList_WhenNoUnitsExist()
    {
        // Act
        var units = await _unitService.GetUnitsAsync();

        // Assert
        Assert.Empty(units);
    }

    [Fact]
    public async Task GetUnitsAsync_IncludesUsageCounts()
    {
        // Arrange
        await CreateUnitWithPercentagesAsync("Test Unit", percentageCount: 2);

        // Act
        var units = await _unitService.GetUnitsAsync();

        // Assert
        Assert.Single(units);
        Assert.Equal(2, units[0].UsageCount);
        Assert.False(units[0].CanDelete);
    }

    [Fact]
    public async Task GetUnitsAsync_SetsCanDeleteTrue_WhenNoUsage()
    {
        // Arrange
        _context.Units.Add(new Unit { Id = 1, Name = "Unused Unit", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var units = await _unitService.GetUnitsAsync();

        // Assert
        Assert.Single(units);
        Assert.Equal(0, units[0].UsageCount);
        Assert.True(units[0].CanDelete);
    }

    #endregion

    #region GetUnitAsync Tests

    [Fact]
    public async Task GetUnitAsync_ReturnsUnit_WhenExists()
    {
        // Arrange
        _context.Units.Add(new Unit { Id = 1, Name = "Test Unit", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var unit = await _unitService.GetUnitAsync(1);

        // Assert
        Assert.NotNull(unit);
        Assert.Equal(1, unit.Id);
        Assert.Equal("Test Unit", unit.Name);
        Assert.True(unit.IsActive);
    }

    [Fact]
    public async Task GetUnitAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var unit = await _unitService.GetUnitAsync(999);

        // Assert
        Assert.Null(unit);
    }

    [Fact]
    public async Task GetUnitAsync_IncludesUsageCount()
    {
        // Arrange
        var unit = await CreateUnitWithPercentagesAsync("Test Unit");

        // Act
        var result = await _unitService.GetUnitAsync(unit.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UsageCount);
        Assert.False(result.CanDelete);
    }

    #endregion

    #region CreateUnitAsync Tests

    [Fact]
    public async Task CreateUnitAsync_CreatesUnit_WithValidRequest()
    {
        // Arrange
        var request = new CreateUnitRequest { Name = "New Unit" };

        // Act
        var result = await _unitService.CreateUnitAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Unit", result.Name);
        Assert.True(result.IsActive);
        Assert.Equal(0, result.UsageCount);
        Assert.True(result.CanDelete);

        // Verify persisted
        var savedUnit = await _context.Units.FirstOrDefaultAsync(u => u.Name == "New Unit");
        Assert.NotNull(savedUnit);
    }

    [Fact]
    public async Task CreateUnitAsync_ThrowsInvalidOperationException_WhenDuplicateName()
    {
        // Arrange
        _context.Units.Add(new Unit { Id = 1, Name = "Existing Unit", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new CreateUnitRequest { Name = "Existing Unit" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _unitService.CreateUnitAsync(request));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CreateUnitAsync_CallsAuditService()
    {
        // Arrange
        var request = new CreateUnitRequest { Name = "Audited Unit" };

        // Act
        await _unitService.CreateUnitAsync(request);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddUnitChangeAudit(
                It.IsAny<int>(),
                It.Is<string>(action => action.Contains("Create")),
                null,
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateUnitAsync_TrimsWhitespace_FromName()
    {
        // Arrange
        var request = new CreateUnitRequest { Name = "  Padded Unit  " };

        // Act
        var result = await _unitService.CreateUnitAsync(request);

        // Assert
        Assert.Equal("Padded Unit", result.Name);
        var savedUnit = await _context.Units.FirstOrDefaultAsync(u => u.Name == "Padded Unit");
        Assert.NotNull(savedUnit);
    }

    [Fact]
    public async Task CreateUnitAsync_ThrowsInvalidOperationException_WhenNameIsWhitespaceOnly()
    {
        // Arrange
        var request = new CreateUnitRequest { Name = "   " };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _unitService.CreateUnitAsync(request));
        Assert.Contains("required", exception.Message);
    }

    #endregion

    #region UpdateUnitAsync Tests

    [Fact]
    public async Task UpdateUnitAsync_UpdatesUnit_WhenExists()
    {
        // Arrange
        _context.Units.Add(new Unit { Id = 1, Name = "Original Name", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new UpdateUnitRequest { Name = "Updated Name", IsActive = false };

        // Act
        var result = await _unitService.UpdateUnitAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateUnitAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var request = new UpdateUnitRequest { Name = "New Name", IsActive = true };

        // Act
        var result = await _unitService.UpdateUnitAsync(999, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUnitAsync_ThrowsInvalidOperationException_WhenDuplicateName()
    {
        // Arrange
        _context.Units.AddRange(
            new Unit { Id = 1, Name = "Unit One", IsActive = true },
            new Unit { Id = 2, Name = "Unit Two", IsActive = true }
        );
        await _context.SaveChangesAsync();

        var request = new UpdateUnitRequest { Name = "Unit Two", IsActive = true };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _unitService.UpdateUnitAsync(1, request));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task UpdateUnitAsync_AllowsSameName_WhenUpdatingSameUnit()
    {
        // Arrange
        _context.Units.Add(new Unit { Id = 1, Name = "Same Name", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new UpdateUnitRequest { Name = "Same Name", IsActive = false };

        // Act
        var result = await _unitService.UpdateUnitAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Same Name", result.Name);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateUnitAsync_CallsAuditService()
    {
        // Arrange
        _context.Units.Add(new Unit { Id = 1, Name = "Original", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new UpdateUnitRequest { Name = "Updated", IsActive = false };

        // Act
        await _unitService.UpdateUnitAsync(1, request);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddUnitChangeAudit(
                1,
                It.Is<string>(action => action.Contains("Update")),
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUnitAsync_TrimsWhitespace_FromName()
    {
        // Arrange
        _context.Units.Add(new Unit { Id = 1, Name = "Original", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new UpdateUnitRequest { Name = "  Updated Name  ", IsActive = true };

        // Act
        var result = await _unitService.UpdateUnitAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
    }

    [Fact]
    public async Task UpdateUnitAsync_ThrowsInvalidOperationException_WhenNameIsWhitespaceOnly()
    {
        // Arrange
        _context.Units.Add(new Unit { Id = 1, Name = "Original", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new UpdateUnitRequest { Name = "   ", IsActive = true };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _unitService.UpdateUnitAsync(1, request));
        Assert.Contains("required", exception.Message);
    }

    #endregion

    #region DeleteUnitAsync Tests

    [Fact]
    public async Task DeleteUnitAsync_DeletesUnit_WhenNoReferences()
    {
        // Arrange
        _context.Units.Add(new Unit { Id = 1, Name = "Deletable Unit", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var result = await _unitService.DeleteUnitAsync(1);

        // Assert
        Assert.True(result);
        var deletedUnit = await _context.Units.FindAsync(1);
        Assert.Null(deletedUnit);
    }

    [Fact]
    public async Task DeleteUnitAsync_ReturnsFalse_WhenReferencesExist()
    {
        // Arrange
        var unit = await CreateUnitWithPercentagesAsync("Referenced Unit");

        // Act
        var result = await _unitService.DeleteUnitAsync(unit.Id);

        // Assert
        Assert.False(result);
        var stillExists = await _context.Units.FindAsync(unit.Id);
        Assert.NotNull(stillExists);
    }

    [Fact]
    public async Task DeleteUnitAsync_ReturnsFalse_WhenNotFound()
    {
        // Act
        var result = await _unitService.DeleteUnitAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUnitAsync_CallsAuditService()
    {
        // Arrange
        _context.Units.Add(new Unit { Id = 1, Name = "To Delete", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        await _unitService.DeleteUnitAsync(1);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddUnitChangeAudit(
                1,
                It.Is<string>(action => action.Contains("Delete")),
                It.IsAny<object>(),
                null),
            Times.Once);
    }

    #endregion

    #region CanDeleteUnitAsync Tests

    [Fact]
    public async Task CanDeleteUnitAsync_ReturnsTrue_WhenNoReferences()
    {
        // Arrange
        _context.Units.Add(new Unit { Id = 1, Name = "Unused Unit", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var canDelete = await _unitService.CanDeleteUnitAsync(1);

        // Assert
        Assert.True(canDelete);
    }

    [Fact]
    public async Task CanDeleteUnitAsync_ReturnsFalse_WhenReferencesExist()
    {
        // Arrange
        var unit = await CreateUnitWithPercentagesAsync("Used Unit");

        // Act
        var canDelete = await _unitService.CanDeleteUnitAsync(unit.Id);

        // Assert
        Assert.False(canDelete);
    }

    #endregion

    #region GetUsageCountAsync Tests

    [Fact]
    public async Task GetUsageCountAsync_ReturnsZero_WhenNoReferences()
    {
        // Arrange
        _context.Units.Add(new Unit { Id = 1, Name = "Unused Unit", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var count = await _unitService.GetUsageCountAsync(1);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetUsageCountAsync_ReturnsCorrectCount_WhenReferencesExist()
    {
        // Arrange
        var unit = await CreateUnitWithPercentagesAsync("Used Unit", percentageCount: 3);

        // Act
        var count = await _unitService.GetUsageCountAsync(unit.Id);

        // Assert
        Assert.Equal(3, count);
    }

    #endregion
}
