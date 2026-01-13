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
/// Unit tests for EffortTypeService effort type management operations.
/// </summary>
public sealed class EffortTypeServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly IMapper _mapper;
    private readonly EffortTypeService _effortTypeService;

    public EffortTypeServiceTests()
    {
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EffortDbContext(effortOptions);
        _auditServiceMock = new Mock<IEffortAuditService>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperProfileEffort>();
        });
        _mapper = mapperConfig.CreateMapper();

        _auditServiceMock
            .Setup(s => s.AddEffortTypeChangeAudit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>()));

        _effortTypeService = new EffortTypeService(_context, _auditServiceMock.Object, _mapper);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Helper Methods

    private async Task<EffortType> CreateEffortTypeWithRecordsAsync(string id, string description, int recordCount = 1)
    {
        var effortType = new EffortType
        {
            Id = id,
            Description = description,
            UsesWeeks = false,
            IsActive = true,
            FacultyCanEnter = true,
            AllowedOnDvm = true,
            AllowedOn199299 = true,
            AllowedOnRCourses = true
        };
        _context.EffortTypes.Add(effortType);
        await _context.SaveChangesAsync();

        // Create required related entities for EffortRecord
        var term = await GetOrCreateTermAsync();
        var course = await GetOrCreateCourseAsync(term.TermCode);
        var person = await GetOrCreatePersonAsync(term.TermCode);
        var role = await GetOrCreateRoleAsync();

        for (int i = 0; i < recordCount; i++)
        {
            _context.Records.Add(new EffortRecord
            {
                CourseId = course.Id,
                PersonId = person.PersonId,
                TermCode = term.TermCode,
                EffortTypeId = effortType.Id,
                RoleId = role.Id,
                Hours = 10,
                Crn = course.Crn
            });
        }
        await _context.SaveChangesAsync();
        return effortType;
    }

    private async Task<EffortTerm> GetOrCreateTermAsync()
    {
        var term = await _context.Terms.FirstOrDefaultAsync();
        if (term == null)
        {
            term = new EffortTerm { TermCode = 202410, Status = "Open", CreatedDate = DateTime.UtcNow };
            _context.Terms.Add(term);
            await _context.SaveChangesAsync();
        }
        return term;
    }

    private async Task<EffortCourse> GetOrCreateCourseAsync(int termCode)
    {
        var course = await _context.Courses.FirstOrDefaultAsync();
        if (course == null)
        {
            course = new EffortCourse
            {
                TermCode = termCode,
                Crn = "12345",
                SubjCode = "TEST",
                CrseNumb = "100",
                SeqNumb = "001",
                Enrollment = 20,
                Units = 4,
                CustDept = "TST"
            };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
        }
        return course;
    }

    private async Task<EffortPerson> GetOrCreatePersonAsync(int termCode)
    {
        var person = await _context.Persons.FirstOrDefaultAsync();
        if (person == null)
        {
            person = new EffortPerson
            {
                PersonId = 1,
                TermCode = termCode,
                FirstName = "Test",
                LastName = "User",
                EffortDept = "TST"
            };
            _context.Persons.Add(person);
            await _context.SaveChangesAsync();
        }
        return person;
    }

    private async Task<EffortRole> GetOrCreateRoleAsync()
    {
        var role = await _context.Roles.FirstOrDefaultAsync();
        if (role == null)
        {
            role = new EffortRole { Id = 1, Description = "Instructor" };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }
        return role;
    }

    #endregion

    #region GetEffortTypesAsync Tests

    [Fact]
    public async Task GetEffortTypesAsync_ReturnsAllEffortTypes_OrderedByDescription()
    {
        // Arrange
        _context.EffortTypes.AddRange(
            new EffortType { Id = "ZZZ", Description = "Zebra Type", IsActive = true },
            new EffortType { Id = "AAA", Description = "Alpha Type", IsActive = true },
            new EffortType { Id = "MMM", Description = "Middle Type", IsActive = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var effortTypes = await _effortTypeService.GetEffortTypesAsync();

        // Assert
        Assert.Equal(3, effortTypes.Count);
        Assert.Equal("Alpha Type", effortTypes[0].Description);
        Assert.Equal("Middle Type", effortTypes[1].Description);
        Assert.Equal("Zebra Type", effortTypes[2].Description);
    }

    [Fact]
    public async Task GetEffortTypesAsync_FiltersActiveOnly_WhenActiveOnlyIsTrue()
    {
        // Arrange
        _context.EffortTypes.AddRange(
            new EffortType { Id = "ACT", Description = "Active Type", IsActive = true },
            new EffortType { Id = "INA", Description = "Inactive Type", IsActive = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var effortTypes = await _effortTypeService.GetEffortTypesAsync(activeOnly: true);

        // Assert
        Assert.Single(effortTypes);
        Assert.Equal("Active Type", effortTypes[0].Description);
        Assert.True(effortTypes[0].IsActive);
    }

    [Fact]
    public async Task GetEffortTypesAsync_ReturnsEmptyList_WhenNoEffortTypesExist()
    {
        // Act
        var effortTypes = await _effortTypeService.GetEffortTypesAsync();

        // Assert
        Assert.Empty(effortTypes);
    }

    [Fact]
    public async Task GetEffortTypesAsync_IncludesUsageCounts()
    {
        // Arrange
        await CreateEffortTypeWithRecordsAsync("TST", "Test Type", recordCount: 2);

        // Act
        var effortTypes = await _effortTypeService.GetEffortTypesAsync();

        // Assert
        Assert.Single(effortTypes);
        Assert.Equal(2, effortTypes[0].UsageCount);
        Assert.False(effortTypes[0].CanDelete);
    }

    [Fact]
    public async Task GetEffortTypesAsync_SetsCanDeleteTrue_WhenNoUsage()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "NEW", Description = "Unused Type", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var effortTypes = await _effortTypeService.GetEffortTypesAsync();

        // Assert
        Assert.Single(effortTypes);
        Assert.Equal(0, effortTypes[0].UsageCount);
        Assert.True(effortTypes[0].CanDelete);
    }

    #endregion

    #region GetEffortTypeAsync Tests

    [Fact]
    public async Task GetEffortTypeAsync_ReturnsEffortType_WhenExists()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "LEC", Description = "Lecture", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var effortType = await _effortTypeService.GetEffortTypeAsync("LEC");

        // Assert
        Assert.NotNull(effortType);
        Assert.Equal("LEC", effortType.Id);
        Assert.Equal("Lecture", effortType.Description);
        Assert.True(effortType.IsActive);
    }

    [Fact]
    public async Task GetEffortTypeAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var effortType = await _effortTypeService.GetEffortTypeAsync("XXX");

        // Assert
        Assert.Null(effortType);
    }

    [Fact]
    public async Task GetEffortTypeAsync_IncludesUsageCount()
    {
        // Arrange
        await CreateEffortTypeWithRecordsAsync("CLI", "Clinical");

        // Act
        var result = await _effortTypeService.GetEffortTypeAsync("CLI");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UsageCount);
        Assert.False(result.CanDelete);
    }

    [Fact]
    public async Task GetEffortTypeAsync_NormalizesToUppercase()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "LEC", Description = "Lecture", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var effortType = await _effortTypeService.GetEffortTypeAsync("lec");

        // Assert
        Assert.NotNull(effortType);
        Assert.Equal("LEC", effortType.Id);
    }

    [Fact]
    public async Task GetEffortTypeAsync_TrimsWhitespace()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "LEC", Description = "Lecture", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var effortType = await _effortTypeService.GetEffortTypeAsync("  LEC  ");

        // Assert
        Assert.NotNull(effortType);
        Assert.Equal("LEC", effortType.Id);
    }

    #endregion

    #region CreateEffortTypeAsync Tests

    [Fact]
    public async Task CreateEffortTypeAsync_CreatesEffortType_WithValidRequest()
    {
        // Arrange
        var request = new CreateEffortTypeRequest
        {
            Id = "NEW",
            Description = "New Type",
            UsesWeeks = false,
            FacultyCanEnter = true,
            AllowedOnDvm = true,
            AllowedOn199299 = true,
            AllowedOnRCourses = true
        };

        // Act
        var result = await _effortTypeService.CreateEffortTypeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NEW", result.Id);
        Assert.Equal("New Type", result.Description);
        Assert.True(result.IsActive);
        Assert.Equal(0, result.UsageCount);
        Assert.True(result.CanDelete);

        var savedEffortType = await _context.EffortTypes.FirstOrDefaultAsync(s => s.Id == "NEW");
        Assert.NotNull(savedEffortType);
    }

    [Fact]
    public async Task CreateEffortTypeAsync_ThrowsInvalidOperationException_WhenDuplicateId()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "DUP", Description = "Existing", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new CreateEffortTypeRequest { Id = "DUP", Description = "Duplicate" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _effortTypeService.CreateEffortTypeAsync(request));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CreateEffortTypeAsync_NormalizesIdToUppercase()
    {
        // Arrange
        var request = new CreateEffortTypeRequest { Id = "low", Description = "Lowercase Test" };

        // Act
        var result = await _effortTypeService.CreateEffortTypeAsync(request);

        // Assert
        Assert.Equal("LOW", result.Id);
        var savedEffortType = await _context.EffortTypes.FirstOrDefaultAsync(s => s.Id == "LOW");
        Assert.NotNull(savedEffortType);
    }

    [Fact]
    public async Task CreateEffortTypeAsync_TrimsWhitespace_FromIdAndDescription()
    {
        // Arrange
        var request = new CreateEffortTypeRequest { Id = "  PAD  ", Description = "  Padded Description  " };

        // Act
        var result = await _effortTypeService.CreateEffortTypeAsync(request);

        // Assert
        Assert.Equal("PAD", result.Id);
        Assert.Equal("Padded Description", result.Description);
    }

    [Fact]
    public async Task CreateEffortTypeAsync_CallsAuditService()
    {
        // Arrange
        var request = new CreateEffortTypeRequest { Id = "AUD", Description = "Audited Type" };

        // Act
        await _effortTypeService.CreateEffortTypeAsync(request);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddEffortTypeChangeAudit(
                "AUD",
                It.Is<string>(action => action.Contains("Create")),
                null,
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateEffortTypeAsync_SetsAllBooleanFlags()
    {
        // Arrange
        var request = new CreateEffortTypeRequest
        {
            Id = "FLG",
            Description = "Flag Test",
            UsesWeeks = true,
            FacultyCanEnter = false,
            AllowedOnDvm = false,
            AllowedOn199299 = false,
            AllowedOnRCourses = false
        };

        // Act
        var result = await _effortTypeService.CreateEffortTypeAsync(request);

        // Assert
        Assert.True(result.UsesWeeks);
        Assert.False(result.FacultyCanEnter);
        Assert.False(result.AllowedOnDvm);
        Assert.False(result.AllowedOn199299);
        Assert.False(result.AllowedOnRCourses);
    }

    #endregion

    #region UpdateEffortTypeAsync Tests

    [Fact]
    public async Task UpdateEffortTypeAsync_UpdatesEffortType_WhenExists()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "UPD", Description = "Original", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new UpdateEffortTypeRequest { Description = "Updated", IsActive = false, UsesWeeks = true };

        // Act
        var result = await _effortTypeService.UpdateEffortTypeAsync("UPD", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Description);
        Assert.False(result.IsActive);
        Assert.True(result.UsesWeeks);
    }

    [Fact]
    public async Task UpdateEffortTypeAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var request = new UpdateEffortTypeRequest { Description = "Updated", IsActive = true };

        // Act
        var result = await _effortTypeService.UpdateEffortTypeAsync("XXX", request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateEffortTypeAsync_NormalizesIdToUppercase()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "UPP", Description = "Original", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new UpdateEffortTypeRequest { Description = "Updated", IsActive = true };

        // Act
        var result = await _effortTypeService.UpdateEffortTypeAsync("upp", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("UPP", result.Id);
    }

    [Fact]
    public async Task UpdateEffortTypeAsync_CallsAuditService()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "AUD", Description = "Original", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new UpdateEffortTypeRequest { Description = "Updated", IsActive = false };

        // Act
        await _effortTypeService.UpdateEffortTypeAsync("AUD", request);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddEffortTypeChangeAudit(
                "AUD",
                It.Is<string>(action => action.Contains("Update")),
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateEffortTypeAsync_TrimsWhitespace_FromDescription()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "TRM", Description = "Original", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new UpdateEffortTypeRequest { Description = "  Updated Name  ", IsActive = true };

        // Act
        var result = await _effortTypeService.UpdateEffortTypeAsync("TRM", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Description);
    }

    [Fact]
    public async Task UpdateEffortTypeAsync_UpdatesAllBooleanFlags()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType
        {
            Id = "FLG",
            Description = "Original",
            IsActive = true,
            UsesWeeks = false,
            FacultyCanEnter = true,
            AllowedOnDvm = true,
            AllowedOn199299 = true,
            AllowedOnRCourses = true
        });
        await _context.SaveChangesAsync();

        var request = new UpdateEffortTypeRequest
        {
            Description = "Updated",
            IsActive = false,
            UsesWeeks = true,
            FacultyCanEnter = false,
            AllowedOnDvm = false,
            AllowedOn199299 = false,
            AllowedOnRCourses = false
        };

        // Act
        var result = await _effortTypeService.UpdateEffortTypeAsync("FLG", request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.True(result.UsesWeeks);
        Assert.False(result.FacultyCanEnter);
        Assert.False(result.AllowedOnDvm);
        Assert.False(result.AllowedOn199299);
        Assert.False(result.AllowedOnRCourses);
    }

    #endregion

    #region DeleteEffortTypeAsync Tests

    [Fact]
    public async Task DeleteEffortTypeAsync_DeletesEffortType_WhenNoReferences()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "DEL", Description = "Deletable", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var result = await _effortTypeService.DeleteEffortTypeAsync("DEL");

        // Assert
        Assert.True(result);
        var deletedEffortType = await _context.EffortTypes.FindAsync("DEL");
        Assert.Null(deletedEffortType);
    }

    [Fact]
    public async Task DeleteEffortTypeAsync_ReturnsFalse_WhenReferencesExist()
    {
        // Arrange
        var effortType = await CreateEffortTypeWithRecordsAsync("REF", "Referenced Type");

        // Act
        var result = await _effortTypeService.DeleteEffortTypeAsync(effortType.Id);

        // Assert
        Assert.False(result);
        var stillExists = await _context.EffortTypes.FindAsync(effortType.Id);
        Assert.NotNull(stillExists);
    }

    [Fact]
    public async Task DeleteEffortTypeAsync_ReturnsFalse_WhenNotFound()
    {
        // Act
        var result = await _effortTypeService.DeleteEffortTypeAsync("XXX");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteEffortTypeAsync_CallsAuditService()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "AUD", Description = "To Delete", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        await _effortTypeService.DeleteEffortTypeAsync("AUD");

        // Assert
        _auditServiceMock.Verify(
            s => s.AddEffortTypeChangeAudit(
                "AUD",
                It.Is<string>(action => action.Contains("Delete")),
                It.IsAny<object>(),
                null),
            Times.Once);
    }

    [Fact]
    public async Task DeleteEffortTypeAsync_NormalizesIdToUppercase()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "LOW", Description = "Lowercase", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var result = await _effortTypeService.DeleteEffortTypeAsync("low");

        // Assert
        Assert.True(result);
        var deleted = await _context.EffortTypes.FindAsync("LOW");
        Assert.Null(deleted);
    }

    #endregion

    #region CanDeleteEffortTypeAsync Tests

    [Fact]
    public async Task CanDeleteEffortTypeAsync_ReturnsTrue_WhenNoReferences()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "CAN", Description = "Unused", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var canDelete = await _effortTypeService.CanDeleteEffortTypeAsync("CAN");

        // Assert
        Assert.True(canDelete);
    }

    [Fact]
    public async Task CanDeleteEffortTypeAsync_ReturnsFalse_WhenReferencesExist()
    {
        // Arrange
        var effortType = await CreateEffortTypeWithRecordsAsync("USE", "Used Type");

        // Act
        var canDelete = await _effortTypeService.CanDeleteEffortTypeAsync(effortType.Id);

        // Assert
        Assert.False(canDelete);
    }

    [Fact]
    public async Task CanDeleteEffortTypeAsync_NormalizesIdToUppercase()
    {
        // Arrange
        _context.EffortTypes.Add(new EffortType { Id = "UPP", Description = "Uppercase", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var canDelete = await _effortTypeService.CanDeleteEffortTypeAsync("upp");

        // Assert
        Assert.True(canDelete);
    }

    #endregion

    #region Case-Insensitive Duplicate Check Tests

    [Fact]
    public async Task CreateEffortTypeAsync_ThrowsInvalidOperationException_WhenDuplicateIdDifferentCase()
    {
        // Arrange - create a effort type with uppercase ID
        _context.EffortTypes.Add(new EffortType { Id = "DUP", Description = "Existing", IsActive = true });
        await _context.SaveChangesAsync();

        // Try to create with lowercase version of same ID
        var request = new CreateEffortTypeRequest { Id = "dup", Description = "Duplicate Lowercase" };

        // Act & Assert - should fail because IDs are case-insensitive
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _effortTypeService.CreateEffortTypeAsync(request));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CreateEffortTypeAsync_ThrowsInvalidOperationException_WhenDuplicateIdMixedCase()
    {
        // Arrange - create a effort type with lowercase ID
        _context.EffortTypes.Add(new EffortType { Id = "ABC", Description = "Existing", IsActive = true });
        await _context.SaveChangesAsync();

        // Try to create with mixed case version
        var request = new CreateEffortTypeRequest { Id = "AbC", Description = "Mixed Case" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _effortTypeService.CreateEffortTypeAsync(request));
        Assert.Contains("already exists", exception.Message);
    }

    #endregion
}
