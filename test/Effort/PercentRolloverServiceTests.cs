using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for PercentRolloverService percent assignment rollover operations.
/// </summary>
public sealed class PercentRolloverServiceTests : IDisposable
{
    private const int TestUserId = 12345;
    private readonly EffortDbContext _effortContext;
    private readonly VIPERContext _viperContext;
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly Mock<ILogger<PercentRolloverService>> _loggerMock;
    private readonly PercentRolloverService _rolloverService;

    public PercentRolloverServiceTests()
    {
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var viperOptions = new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _effortContext = new EffortDbContext(effortOptions);
        _viperContext = new VIPERContext(viperOptions);
        _auditServiceMock = new Mock<IEffortAuditService>();
        _loggerMock = new Mock<ILogger<PercentRolloverService>>();

        _auditServiceMock
            .Setup(s => s.AddImportAudit(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _rolloverService = new PercentRolloverService(
            _effortContext, _viperContext, _auditServiceMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _effortContext.Dispose();
        _viperContext.Dispose();
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
        _effortContext.PercentAssignTypes.Add(type);
        await _effortContext.SaveChangesAsync();
        return type;
    }

    private async Task<Unit> CreateUnitAsync(string name = "Test Unit", bool isActive = true)
    {
        var unit = new Unit { Name = name, IsActive = isActive };
        _effortContext.Units.Add(unit);
        await _effortContext.SaveChangesAsync();
        return unit;
    }

    private async Task<Person> CreatePersonAsync(int personId, string firstName, string lastName, string mothraId)
    {
        var person = new Person
        {
            PersonId = personId,
            FirstName = firstName,
            LastName = lastName,
            MothraId = mothraId,
            ClientId = "test",
            FullName = $"{firstName} {lastName}"
        };
        _viperContext.People.Add(person);
        await _viperContext.SaveChangesAsync();
        return person;
    }

    private async Task<Percentage> CreatePercentageAsync(
        int personId, int typeId, double percentValue,
        DateTime startDate, DateTime? endDate = null, int? unitId = null,
        string? modifier = null, bool compensated = false, string? comment = null)
    {
        var percentage = new Percentage
        {
            PersonId = personId,
            PercentAssignTypeId = typeId,
            PercentageValue = percentValue,
            StartDate = startDate,
            EndDate = endDate,
            AcademicYear = CalculateAcademicYear(startDate),
            UnitId = unitId,
            Modifier = modifier,
            Compensated = compensated,
            Comment = comment,
            ModifiedDate = DateTime.Now,
            ModifiedBy = TestUserId
        };
        _effortContext.Percentages.Add(percentage);
        await _effortContext.SaveChangesAsync();
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

    #region ShouldRollover Tests

    [Fact]
    public void ShouldRollover_ReturnsTrueForFallSemester_202509()
    {
        // Arrange - Fall Semester term code ending in 09
        var termCode = 202509;

        // Act
        var result = _rolloverService.ShouldRollover(termCode);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRollover_ReturnsTrueForFallQuarter_202510()
    {
        // Arrange - Fall Quarter term code ending in 10
        var termCode = 202510;

        // Act
        var result = _rolloverService.ShouldRollover(termCode);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRollover_ReturnsFalseForWinterTerm_202601()
    {
        // Arrange - Winter term code ending in 01
        var termCode = 202601;

        // Act
        var result = _rolloverService.ShouldRollover(termCode);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRollover_ReturnsFalseForSpringTerm_202603()
    {
        // Arrange - Spring term code ending in 03
        var termCode = 202603;

        // Act
        var result = _rolloverService.ShouldRollover(termCode);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRollover_ReturnsFalseForSummerTerm_202506()
    {
        // Arrange - Summer term code ending in 06
        var termCode = 202506;

        // Act
        var result = _rolloverService.ShouldRollover(termCode);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetRolloverPreviewAsync Tests

    [Fact]
    public async Task GetRolloverPreviewAsync_FindsAssignmentsEndingJune30()
    {
        // Arrange - Create assignments ending on June 30, 2025
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        await CreatePersonAsync(100, "John", "Doe", "jdoe");

        await CreatePercentageAsync(
            100, type.Id, 0.5,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit.Id);

        // Act
        var result = await _rolloverService.GetRolloverPreviewAsync(202509);

        // Assert
        Assert.True(result.IsRolloverApplicable);
        Assert.Single(result.Assignments);
        Assert.Equal(100, result.Assignments[0].PersonId);
        Assert.Equal(0.5, result.Assignments[0].PercentageValue);
    }

    [Fact]
    public async Task GetRolloverPreviewAsync_IgnoresAssignmentsNotEndingJune30()
    {
        // Arrange - Create assignments with different end dates
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        await CreatePersonAsync(100, "John", "Doe", "jdoe");

        await CreatePercentageAsync(
            100, type.Id, 0.5,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 29, 0, 0, 0, DateTimeKind.Local),
            unit.Id);

        await CreatePercentageAsync(
            100, type.Id, 0.3,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local),
            unit.Id);

        await CreatePercentageAsync(
            100, type.Id, 0.25,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            null,  // No end date
            unit.Id);

        // Act
        var result = await _rolloverService.GetRolloverPreviewAsync(202509);

        // Assert
        Assert.False(result.IsRolloverApplicable);
        Assert.Empty(result.Assignments);
    }

    [Fact]
    public async Task GetRolloverPreviewAsync_CalculatesCorrectDates()
    {
        // Arrange - Fall 2025 (term 202509)
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        await CreatePersonAsync(100, "John", "Doe", "jdoe");

        await CreatePercentageAsync(
            100, type.Id, 0.5,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit.Id);

        // Act
        var result = await _rolloverService.GetRolloverPreviewAsync(202509);

        // Assert
        Assert.Equal(2025, result.SourceAcademicYear);
        Assert.Equal(2026, result.TargetAcademicYear);
        Assert.Equal("2024-2025", result.SourceAcademicYearDisplay);
        Assert.Equal("2025-2026", result.TargetAcademicYearDisplay);
        Assert.Equal(new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local), result.OldEndDate);
        Assert.Equal(new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local), result.NewStartDate);
        Assert.Equal(new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Local), result.NewEndDate);

        var assignment = result.Assignments[0];
        Assert.Equal(new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local), assignment.CurrentEndDate);
        Assert.Equal(new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local), assignment.ProposedStartDate);
        Assert.Equal(new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Local), assignment.ProposedEndDate);
    }

    [Fact]
    public async Task GetRolloverPreviewAsync_ExcludesAlreadyRolledAssignments()
    {
        // Arrange - Create a source assignment and a target assignment (already rolled)
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        await CreatePersonAsync(100, "John", "Doe", "jdoe");

        // Source assignment ending June 30, 2025
        await CreatePercentageAsync(
            100, type.Id, 0.5,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit.Id,
            modifier: "Test",
            compensated: true);

        // Target assignment starting July 1, 2025 (already rolled)
        await CreatePercentageAsync(
            100, type.Id, 0.5,
            new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit.Id,
            modifier: "Test",
            compensated: true);

        // Act
        var result = await _rolloverService.GetRolloverPreviewAsync(202509);

        // Assert - Should exclude already-rolled assignment (idempotency)
        Assert.False(result.IsRolloverApplicable);
        Assert.Empty(result.Assignments);
    }

    #endregion

    #region ExecuteRolloverAsync Tests

    [Fact]
    public async Task ExecuteRolloverAsync_CreatesNewPercentageRecords()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        await CreatePersonAsync(100, "John", "Doe", "jdoe");

        await CreatePercentageAsync(
            100, type.Id, 0.5,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit.Id);

        var initialCount = await _effortContext.Percentages.CountAsync();

        // Act
        var created = await _rolloverService.ExecuteRolloverAsync(202509, TestUserId);
        await _effortContext.SaveChangesAsync();

        // Assert
        Assert.Equal(1, created);
        var finalCount = await _effortContext.Percentages.CountAsync();
        Assert.Equal(initialCount + 1, finalCount);
    }

    [Fact]
    public async Task ExecuteRolloverAsync_CopiesAllFieldsCorrectly()
    {
        // Arrange - Create source assignment with all fields populated
        var type = await CreatePercentAssignTypeAsync("Research", "Clinical");
        var unit = await CreateUnitAsync("Surgery");
        await CreatePersonAsync(100, "Jane", "Smith", "jsmith");

        await CreatePercentageAsync(
            100, type.Id, 0.75,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit.Id,
            modifier: "Primary",
            compensated: true,
            comment: "Test comment");

        // Act
        var created = await _rolloverService.ExecuteRolloverAsync(202509, TestUserId);
        await _effortContext.SaveChangesAsync();

        // Assert
        Assert.Equal(1, created);

        var newRecords = await _effortContext.Percentages
            .Where(p => p.StartDate == new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local))
            .ToListAsync();

        var newRecord = Assert.Single(newRecords);
        Assert.Equal(100, newRecord.PersonId);
        Assert.Equal("2025-2026", newRecord.AcademicYear);
        Assert.Equal(0.75, newRecord.PercentageValue);
        Assert.Equal(type.Id, newRecord.PercentAssignTypeId);
        Assert.Equal(unit.Id, newRecord.UnitId);
        Assert.Equal("Primary", newRecord.Modifier);
        Assert.Equal("Test comment", newRecord.Comment);
        Assert.True(newRecord.Compensated);
        Assert.Equal(new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local), newRecord.StartDate);
        Assert.Equal(new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Local), newRecord.EndDate);
        Assert.Equal(TestUserId, newRecord.ModifiedBy);
        Assert.NotNull(newRecord.ModifiedDate);
    }

    [Fact]
    public async Task ExecuteRolloverAsync_ReturnsZeroWhenNoRolloverNeeded()
    {
        // Arrange - No assignments ending June 30, 2025
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        await CreatePersonAsync(100, "John", "Doe", "jdoe");

        await CreatePercentageAsync(
            100, type.Id, 0.5,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Local),
            unit.Id);

        // Act
        var created = await _rolloverService.ExecuteRolloverAsync(202509, TestUserId);

        // Assert
        Assert.Equal(0, created);
    }

    [Fact]
    public async Task ExecuteRolloverAsync_IsIdempotent_SecondRunCreatesZero()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        await CreatePersonAsync(100, "John", "Doe", "jdoe");

        await CreatePercentageAsync(
            100, type.Id, 0.5,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit.Id,
            modifier: "Test",
            compensated: true);

        // Act - First rollover
        var firstRun = await _rolloverService.ExecuteRolloverAsync(202509, TestUserId);
        await _effortContext.SaveChangesAsync();

        // Act - Second rollover (should be idempotent)
        var secondRun = await _rolloverService.ExecuteRolloverAsync(202509, TestUserId);
        await _effortContext.SaveChangesAsync();

        // Assert
        Assert.Equal(1, firstRun);
        Assert.Equal(0, secondRun);  // Idempotency: second run creates nothing

        var targetYearCount = await _effortContext.Percentages
            .Where(p => p.StartDate == new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local))
            .CountAsync();
        Assert.Equal(1, targetYearCount);  // Only one record created, not two
    }

    [Fact]
    public async Task ExecuteRolloverAsync_SkipsNonFallTerms()
    {
        // Arrange - Create assignment and try to rollover in Winter term
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        await CreatePersonAsync(100, "John", "Doe", "jdoe");

        await CreatePercentageAsync(
            100, type.Id, 0.5,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit.Id);

        // Act - Try Winter term (202601)
        var created = await _rolloverService.ExecuteRolloverAsync(202601, TestUserId);

        // Assert
        Assert.Equal(0, created);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Skipping percent rollover for non-Fall term")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Integration Tests - Complex Scenarios

    [Fact]
    public async Task ExecuteRolloverAsync_HandlesMultiplePeopleAndTypes()
    {
        // Arrange - Multiple people with different types
        var teachingType = await CreatePercentAssignTypeAsync("Teaching", "Other");
        var researchType = await CreatePercentAssignTypeAsync("Research", "Clinical");
        var adminType = await CreatePercentAssignTypeAsync("Admin", "Admin");
        var unit1 = await CreateUnitAsync("Surgery");
        var unit2 = await CreateUnitAsync("Medicine");

        await CreatePersonAsync(100, "John", "Doe", "jdoe");
        await CreatePersonAsync(200, "Jane", "Smith", "jsmith");

        await CreatePercentageAsync(100, teachingType.Id, 0.5,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit1.Id);

        await CreatePercentageAsync(100, researchType.Id, 0.3,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit2.Id);

        await CreatePercentageAsync(200, adminType.Id, 0.75,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit1.Id);

        // Act
        var created = await _rolloverService.ExecuteRolloverAsync(202509, TestUserId);
        await _effortContext.SaveChangesAsync();

        // Assert
        Assert.Equal(3, created);

        var newRecords = await _effortContext.Percentages
            .Where(p => p.StartDate == new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local))
            .ToListAsync();

        Assert.Equal(3, newRecords.Count);
        Assert.Contains(newRecords, r => r.PersonId == 100 && Math.Abs(r.PercentageValue - 0.5) < 0.0001);
        Assert.Contains(newRecords, r => r.PersonId == 100 && Math.Abs(r.PercentageValue - 0.3) < 0.0001);
        Assert.Contains(newRecords, r => r.PersonId == 200 && Math.Abs(r.PercentageValue - 0.75) < 0.0001);
    }

    [Fact]
    public async Task ExecuteRolloverAsync_PreservesCompositeKeyForIdempotency()
    {
        // Arrange - Same person, type, unit, but different modifiers (should create separate records)
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        await CreatePersonAsync(100, "John", "Doe", "jdoe");

        await CreatePercentageAsync(100, type.Id, 0.3,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit.Id,
            modifier: "Primary",
            compensated: true);

        await CreatePercentageAsync(100, type.Id, 0.2,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit.Id,
            modifier: "Secondary",
            compensated: false);

        // Act
        var created = await _rolloverService.ExecuteRolloverAsync(202509, TestUserId);
        await _effortContext.SaveChangesAsync();

        // Assert
        Assert.Equal(2, created);

        var newRecords = await _effortContext.Percentages
            .Where(p => p.StartDate == new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local))
            .OrderBy(p => p.Modifier)
            .ToListAsync();

        Assert.Equal(2, newRecords.Count);
        Assert.Equal("Primary", newRecords[0].Modifier);
        Assert.True(newRecords[0].Compensated);
        Assert.Equal(0.3, newRecords[0].PercentageValue);
        Assert.Equal("Secondary", newRecords[1].Modifier);
        Assert.False(newRecords[1].Compensated);
        Assert.Equal(0.2, newRecords[1].PercentageValue);
    }

    [Fact]
    public async Task GetRolloverPreviewAsync_EnrichesWithPersonDetails()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync("Teaching", "Other");
        var unit = await CreateUnitAsync("Surgery");
        await CreatePersonAsync(100, "John", "Doe", "jdoe");

        await CreatePercentageAsync(100, type.Id, 0.5,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit.Id);

        // Act
        var result = await _rolloverService.GetRolloverPreviewAsync(202509);

        // Assert
        var assignment = Assert.Single(result.Assignments);
        Assert.Equal("Doe, John", assignment.PersonName);
        Assert.Equal("jdoe", assignment.MothraId);
        Assert.Equal("Teaching", assignment.TypeName);
        Assert.Equal("Other", assignment.TypeClass);
        Assert.Equal("Surgery", assignment.UnitName);
    }

    [Fact]
    public async Task ExecuteRolloverAsync_CallsAuditService()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync();
        var unit = await CreateUnitAsync();
        await CreatePersonAsync(100, "John", "Doe", "jdoe");

        await CreatePercentageAsync(100, type.Id, 0.5,
            new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Local),
            unit.Id);

        // Act
        await _rolloverService.ExecuteRolloverAsync(202509, TestUserId);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddImportAudit(
                202509,
                It.Is<string>(a => a.Contains("RolloverPercentAssignments")),
                It.Is<string>(m => m.Contains("Rolled over 1 percent assignments"))),
            Times.Once);
    }

    #endregion
}
