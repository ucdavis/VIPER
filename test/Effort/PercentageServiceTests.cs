using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Models;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;
using Viper.Models.AAUD;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for PercentageService percentage assignment operations.
/// </summary>
public sealed class PercentageServiceTests : IDisposable
{
    private const int TestUserId = 12345;
    private readonly EffortDbContext _context;
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly Mock<IUserHelper> _userHelperMock;
    private readonly IMapper _mapper;
    private readonly PercentageService _percentageService;

    public PercentageServiceTests()
    {
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EffortDbContext(effortOptions);
        _auditServiceMock = new Mock<IEffortAuditService>();
        _userHelperMock = new Mock<IUserHelper>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperProfileEffort>();
        });
        _mapper = mapperConfig.CreateMapper();

        var testUser = new AaudUser { AaudUserId = TestUserId, MothraId = "testuser" };
        _userHelperMock.Setup(x => x.GetCurrentUser()).Returns(testUser);

        _auditServiceMock
            .Setup(s => s.LogPercentageChangeAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _percentageService = new PercentageService(
            _context, _mapper, _auditServiceMock.Object, _userHelperMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Helper Methods

    private async Task<PercentAssignType> CreatePercentAssignTypeAsync(
        string name = "Teaching", string typeClass = "Other", bool isActive = true)
    {
        var type = new PercentAssignType
        {
            Name = name,
            Class = typeClass,
            IsActive = isActive,
            ShowOnTemplate = true
        };
        _context.PercentAssignTypes.Add(type);
        await _context.SaveChangesAsync();
        return type;
    }

    private async Task<PercentAssignType> CreateLeaveTypeAsync()
    {
        return await CreatePercentAssignTypeAsync("Sabbatical", "Leave");
    }

    private async Task<Unit> CreateUnitAsync(string name = "Test Unit", bool isActive = true)
    {
        var unit = new Unit { Name = name, IsActive = isActive };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();
        return unit;
    }

    private async Task<Percentage> CreatePercentageAsync(
        int personId, int typeId, decimal percentValue,
        DateTime startDate, DateTime? endDate = null, int? unitId = null)
    {
        var percentage = new Percentage
        {
            PersonId = personId,
            PercentAssignTypeId = typeId,
            PercentageValue = (double)(percentValue / 100m),
            StartDate = startDate,
            EndDate = endDate,
            AcademicYear = CalculateAcademicYear(startDate),
            UnitId = unitId
        };
        _context.Percentages.Add(percentage);
        await _context.SaveChangesAsync();
        return percentage;
    }

    private static string CalculateAcademicYear(DateTime date)
    {
        if (date.Month >= 7)
        {
            return $"{date.Year}-{date.Year + 1}";
        }
        return $"{date.Year - 1}-{date.Year}";
    }

    #endregion

    #region GetPercentagesForPersonAsync Tests

    [Fact]
    public async Task GetPercentagesForPersonAsync_ReturnsCorrectData()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        await CreatePercentageAsync(100, type.Id, 50, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local), null, unit.Id);
        await CreatePercentageAsync(100, type.Id, 30, new DateTime(2023, 7, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Local), unit.Id);
        await CreatePercentageAsync(200, type.Id, 25, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local));

        // Act
        var results = await _percentageService.GetPercentagesForPersonAsync(100);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(100, r.PersonId));
        Assert.Contains(results, r => r.PercentageValue == 50);
        Assert.Contains(results, r => r.PercentageValue == 30);
    }

    [Fact]
    public async Task GetPercentagesForPersonAsync_ReturnsEmptyList_WhenNoPercentagesExist()
    {
        // Act
        var results = await _percentageService.GetPercentagesForPersonAsync(999);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetPercentagesForPersonAsync_EnrichesDto_WithTypeAndUnitNames()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync("Research", "Clinical");
        var unit = await CreateUnitAsync("Surgery");
        await CreatePercentageAsync(100, type.Id, 75, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local), null, unit.Id);

        // Act
        var results = await _percentageService.GetPercentagesForPersonAsync(100);

        // Assert
        var dto = Assert.Single(results);
        Assert.Equal("Research", dto.TypeName);
        Assert.Equal("Clinical", dto.TypeClass);
        Assert.Equal("Surgery", dto.UnitName);
    }

    #endregion

    #region CreatePercentageAsync Tests

    [Fact]
    public async Task CreatePercentageAsync_ConvertPercent_100To1()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 100,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.CreatePercentageAsync(request);

        // Assert
        Assert.Equal(100, result.PercentageValue);

        var stored = await _context.Percentages.FirstAsync(p => p.Id == result.Id);
        Assert.Equal(1.0, stored.PercentageValue);
    }

    [Fact]
    public async Task CreatePercentageAsync_ConvertPercent_50To05()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 50,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.CreatePercentageAsync(request);

        // Assert
        Assert.Equal(50, result.PercentageValue);

        var stored = await _context.Percentages.FirstAsync(p => p.Id == result.Id);
        Assert.Equal(0.5, stored.PercentageValue);
    }

    [Fact]
    public async Task CreatePercentageAsync_ThrowsException_WhenInvalidTypeId()
    {
        // Arrange
        var unit = await CreateUnitAsync();
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = 999,
            UnitId = unit.Id,
            PercentageValue = 50,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _percentageService.CreatePercentageAsync(request));
        Assert.Contains("Invalid percent assignment type", ex.Message);
    }

    [Fact]
    public async Task CreatePercentageAsync_ThrowsException_WhenInactiveType()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync("Inactive Type", "Other", isActive: false);
        var unit = await CreateUnitAsync();
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 50,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _percentageService.CreatePercentageAsync(request));
        Assert.Contains("inactive", ex.Message);
    }

    [Fact]
    public async Task CreatePercentageAsync_ThrowsException_WhenPercentageBelow0()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = -10,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _percentageService.CreatePercentageAsync(request));
        Assert.Contains("between 0 and 100", ex.Message);
    }

    [Fact]
    public async Task CreatePercentageAsync_ThrowsException_WhenPercentageAbove100()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 150,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _percentageService.CreatePercentageAsync(request));
        Assert.Contains("between 0 and 100", ex.Message);
    }

    [Fact]
    public async Task CreatePercentageAsync_ThrowsException_WhenEndDateBeforeStartDate()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 50,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            EndDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _percentageService.CreatePercentageAsync(request));
        Assert.Contains("End date cannot be before start date", ex.Message);
    }

    [Fact]
    public async Task CreatePercentageAsync_CalculatesAcademicYear_ForJulyStart()
    {
        // Arrange - July 2024 should be 2024-2025
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 50,
            StartDate = new DateTime(2024, 7, 15, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.CreatePercentageAsync(request);

        // Assert
        var stored = await _context.Percentages.FirstAsync(p => p.Id == result.Id);
        Assert.Equal("2024-2025", stored.AcademicYear);
    }

    [Fact]
    public async Task CreatePercentageAsync_CalculatesAcademicYear_ForMarchStart()
    {
        // Arrange - March 2024 should be 2023-2024
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 50,
            StartDate = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.CreatePercentageAsync(request);

        // Assert
        var stored = await _context.Percentages.FirstAsync(p => p.Id == result.Id);
        Assert.Equal("2023-2024", stored.AcademicYear);
    }

    [Fact]
    public async Task CreatePercentageAsync_CallsAuditService()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 50,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        await _percentageService.CreatePercentageAsync(request);

        // Assert
        _auditServiceMock.Verify(
            s => s.LogPercentageChangeAsync(
                It.IsAny<int>(),
                null,
                It.Is<string>(a => a.Contains("Create")),
                null,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region UpdatePercentageAsync Tests

    [Fact]
    public async Task UpdatePercentageAsync_UpdatesPercentage_WhenExists()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        var existing = await CreatePercentageAsync(100, type.Id, 50, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local));

        var request = new UpdatePercentageRequest
        {
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 75,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.UpdatePercentageAsync(existing.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(75, result.PercentageValue);
    }

    [Fact]
    public async Task UpdatePercentageAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        var request = new UpdatePercentageRequest
        {
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 75,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.UpdatePercentageAsync(999, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePercentageAsync_CallsAuditService()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        var existing = await CreatePercentageAsync(100, type.Id, 50, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local));

        var request = new UpdatePercentageRequest
        {
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 75,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        await _percentageService.UpdatePercentageAsync(existing.Id, request);

        // Assert
        _auditServiceMock.Verify(
            s => s.LogPercentageChangeAsync(
                existing.Id,
                null,
                It.Is<string>(a => a.Contains("Update")),
                It.IsAny<object>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region DeletePercentageAsync Tests

    [Fact]
    public async Task DeletePercentageAsync_RemovesRecord()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var existing = await CreatePercentageAsync(100, type.Id, 50, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local));

        // Act
        var result = await _percentageService.DeletePercentageAsync(existing.Id);

        // Assert
        Assert.True(result);
        var deleted = await _context.Percentages.FindAsync(existing.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeletePercentageAsync_ReturnsFalse_WhenNotFound()
    {
        // Act
        var result = await _percentageService.DeletePercentageAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeletePercentageAsync_CallsAuditService()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var existing = await CreatePercentageAsync(100, type.Id, 50, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local));

        // Act
        await _percentageService.DeletePercentageAsync(existing.Id);

        // Assert
        _auditServiceMock.Verify(
            s => s.LogPercentageChangeAsync(
                existing.Id,
                null,
                It.Is<string>(a => a.Contains("Delete")),
                It.IsAny<object>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ValidatePercentageAsync Tests

    [Fact]
    public async Task ValidatePercentageAsync_DetectsOverlap_WithSameType()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        await CreatePercentageAsync(100, type.Id, 50, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Local));

        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 25,
            StartDate = new DateTime(2024, 9, 1, 0, 0, 0, DateTimeKind.Local),
            EndDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.ValidatePercentageAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("overlaps"));
    }

    [Fact]
    public async Task ValidatePercentageAsync_NoOverlapWarning_WhenDifferentTypes()
    {
        // Arrange
        var type1 = await CreatePercentAssignTypeAsync("Teaching", "Other");
        var type2 = await CreatePercentAssignTypeAsync("Research", "Other");
        var unit = await CreateUnitAsync();
        await CreatePercentageAsync(100, type1.Id, 50, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Local));

        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type2.Id,
            UnitId = unit.Id,
            PercentageValue = 25,
            StartDate = new DateTime(2024, 9, 1, 0, 0, 0, DateTimeKind.Local),
            EndDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.ValidatePercentageAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Warnings, w => w.Contains("overlaps"));
    }

    [Fact]
    public async Task ValidatePercentageAsync_WarnsWhenTotalExceeds100()
    {
        // Arrange
        var type1 = await CreatePercentAssignTypeAsync("Teaching", "Other");
        var type2 = await CreatePercentAssignTypeAsync("Research", "Other");
        var unit = await CreateUnitAsync();
        await CreatePercentageAsync(100, type1.Id, 60, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local));

        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type2.Id,
            UnitId = unit.Id,
            PercentageValue = 50,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.ValidatePercentageAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("exceeds 100%"));
    }

    [Fact]
    public async Task ValidatePercentageAsync_ExcludesLeaveFromTotalCalculation()
    {
        // Arrange
        var teachingType = await CreatePercentAssignTypeAsync("Teaching", "Other");
        var leaveType = await CreateLeaveTypeAsync();
        var unit = await CreateUnitAsync();
        await CreatePercentageAsync(100, teachingType.Id, 60, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local));
        await CreatePercentageAsync(100, leaveType.Id, 100, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local));

        var researchType = await CreatePercentAssignTypeAsync("Research", "Other");
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = researchType.Id,
            UnitId = unit.Id,
            PercentageValue = 30,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.ValidatePercentageAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Warnings, w => w.Contains("exceeds 100%"));
    }

    [Fact]
    public async Task ValidatePercentageAsync_DoesNotWarnForLeaveTypeEvenIfTotalHigh()
    {
        // Arrange
        var teachingType = await CreatePercentAssignTypeAsync("Teaching", "Other");
        var unit = await CreateUnitAsync();
        await CreatePercentageAsync(100, teachingType.Id, 100, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local));

        var leaveType = await CreateLeaveTypeAsync();
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = leaveType.Id,
            UnitId = unit.Id,
            PercentageValue = 50,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.ValidatePercentageAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Warnings, w => w.Contains("exceeds 100%"));
    }

    [Fact]
    public async Task ValidatePercentageAsync_ReturnsError_WhenInvalidType()
    {
        // Arrange
        var unit = await CreateUnitAsync();
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = 999,
            UnitId = unit.Id,
            PercentageValue = 50,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.ValidatePercentageAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Invalid percent assignment type"));
    }

    [Fact]
    public async Task ValidatePercentageAsync_ReturnsError_WhenEndDateBeforeStartDate()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 50,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            EndDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.ValidatePercentageAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("End date cannot be before start date"));
    }

    [Fact]
    public async Task ValidatePercentageAsync_ExcludesIdFromOverlapCheck()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        var existing = await CreatePercentageAsync(100, type.Id, 50, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local));

        var request = new CreatePercentageRequest
        {
            PersonId = 100,
            PercentAssignTypeId = type.Id,
            UnitId = unit.Id,
            PercentageValue = 75,
            StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Compensated = false
        };

        // Act
        var result = await _percentageService.ValidatePercentageAsync(request, excludeId: existing.Id);

        // Assert
        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Warnings, w => w.Contains("overlaps"));
    }

    #endregion

    #region GetPercentagesForPersonByDateRangeAsync Tests

    [Fact]
    public async Task GetPercentagesForPersonByDateRangeAsync_ReturnsOverlappingRecords()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        await CreatePercentageAsync(100, type.Id, 50, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Local));
        await CreatePercentageAsync(100, type.Id, 60, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Local));
        await CreatePercentageAsync(100, type.Id, 70, new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Local));

        // Act
        var results = await _percentageService.GetPercentagesForPersonByDateRangeAsync(
            100, new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2024, 9, 30, 0, 0, 0, DateTimeKind.Local));

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.PercentageValue == 50);
        Assert.Contains(results, r => r.PercentageValue == 60);
    }

    [Fact]
    public async Task GetPercentagesForPersonByDateRangeAsync_IncludesOpenEndedRecords()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        await CreatePercentageAsync(100, type.Id, 50, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local));

        // Act
        var results = await _percentageService.GetPercentagesForPersonByDateRangeAsync(
            100, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Local));

        // Assert
        var dto = Assert.Single(results);
        Assert.Equal(50, dto.PercentageValue);
    }

    #endregion

    #region IsActive Calculation Tests

    [Fact]
    public async Task GetPercentageAsync_CalculatesIsActive_TrueWhenNoEndDate()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var percentage = await CreatePercentageAsync(100, type.Id, 50, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local));

        // Act
        var result = await _percentageService.GetPercentageAsync(percentage.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetPercentageAsync_CalculatesIsActive_TrueWhenEndDateInFuture()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var percentage = await CreatePercentageAsync(100, type.Id, 50, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local), DateTime.Today.AddYears(1));

        // Act
        var result = await _percentageService.GetPercentageAsync(percentage.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetPercentageAsync_CalculatesIsActive_FalseWhenEndDateInPast()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var percentage = await CreatePercentageAsync(100, type.Id, 50, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local), DateTime.Today.AddDays(-1));

        // Act
        var result = await _percentageService.GetPercentageAsync(percentage.Id);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    #endregion
}
