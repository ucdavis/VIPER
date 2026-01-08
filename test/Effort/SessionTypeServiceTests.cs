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
/// Unit tests for SessionTypeService session type management operations.
/// </summary>
public sealed class SessionTypeServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly IMapper _mapper;
    private readonly SessionTypeService _sessionTypeService;

    public SessionTypeServiceTests()
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
            .Setup(s => s.AddSessionTypeChangeAudit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>()));

        _sessionTypeService = new SessionTypeService(_context, _auditServiceMock.Object, _mapper);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Helper Methods

    private async Task<SessionType> CreateSessionTypeWithRecordsAsync(string id, string description, int recordCount = 1)
    {
        var sessionType = new SessionType
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
        _context.SessionTypes.Add(sessionType);
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
                SessionType = sessionType.Id,
                Role = role.Id,
                Hours = 10,
                Crn = course.Crn
            });
        }
        await _context.SaveChangesAsync();
        return sessionType;
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

    #region GetSessionTypesAsync Tests

    [Fact]
    public async Task GetSessionTypesAsync_ReturnsAllSessionTypes_OrderedByDescription()
    {
        // Arrange
        _context.SessionTypes.AddRange(
            new SessionType { Id = "ZZZ", Description = "Zebra Type", IsActive = true },
            new SessionType { Id = "AAA", Description = "Alpha Type", IsActive = true },
            new SessionType { Id = "MMM", Description = "Middle Type", IsActive = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var sessionTypes = await _sessionTypeService.GetSessionTypesAsync();

        // Assert
        Assert.Equal(3, sessionTypes.Count);
        Assert.Equal("Alpha Type", sessionTypes[0].Description);
        Assert.Equal("Middle Type", sessionTypes[1].Description);
        Assert.Equal("Zebra Type", sessionTypes[2].Description);
    }

    [Fact]
    public async Task GetSessionTypesAsync_FiltersActiveOnly_WhenActiveOnlyIsTrue()
    {
        // Arrange
        _context.SessionTypes.AddRange(
            new SessionType { Id = "ACT", Description = "Active Type", IsActive = true },
            new SessionType { Id = "INA", Description = "Inactive Type", IsActive = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var sessionTypes = await _sessionTypeService.GetSessionTypesAsync(activeOnly: true);

        // Assert
        Assert.Single(sessionTypes);
        Assert.Equal("Active Type", sessionTypes[0].Description);
        Assert.True(sessionTypes[0].IsActive);
    }

    [Fact]
    public async Task GetSessionTypesAsync_ReturnsEmptyList_WhenNoSessionTypesExist()
    {
        // Act
        var sessionTypes = await _sessionTypeService.GetSessionTypesAsync();

        // Assert
        Assert.Empty(sessionTypes);
    }

    [Fact]
    public async Task GetSessionTypesAsync_IncludesUsageCounts()
    {
        // Arrange
        await CreateSessionTypeWithRecordsAsync("TST", "Test Type", recordCount: 2);

        // Act
        var sessionTypes = await _sessionTypeService.GetSessionTypesAsync();

        // Assert
        Assert.Single(sessionTypes);
        Assert.Equal(2, sessionTypes[0].UsageCount);
        Assert.False(sessionTypes[0].CanDelete);
    }

    [Fact]
    public async Task GetSessionTypesAsync_SetsCanDeleteTrue_WhenNoUsage()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "NEW", Description = "Unused Type", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var sessionTypes = await _sessionTypeService.GetSessionTypesAsync();

        // Assert
        Assert.Single(sessionTypes);
        Assert.Equal(0, sessionTypes[0].UsageCount);
        Assert.True(sessionTypes[0].CanDelete);
    }

    #endregion

    #region GetSessionTypeAsync Tests

    [Fact]
    public async Task GetSessionTypeAsync_ReturnsSessionType_WhenExists()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "LEC", Description = "Lecture", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var sessionType = await _sessionTypeService.GetSessionTypeAsync("LEC");

        // Assert
        Assert.NotNull(sessionType);
        Assert.Equal("LEC", sessionType.Id);
        Assert.Equal("Lecture", sessionType.Description);
        Assert.True(sessionType.IsActive);
    }

    [Fact]
    public async Task GetSessionTypeAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var sessionType = await _sessionTypeService.GetSessionTypeAsync("XXX");

        // Assert
        Assert.Null(sessionType);
    }

    [Fact]
    public async Task GetSessionTypeAsync_IncludesUsageCount()
    {
        // Arrange
        await CreateSessionTypeWithRecordsAsync("CLI", "Clinical");

        // Act
        var result = await _sessionTypeService.GetSessionTypeAsync("CLI");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UsageCount);
        Assert.False(result.CanDelete);
    }

    [Fact]
    public async Task GetSessionTypeAsync_NormalizesToUppercase()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "LEC", Description = "Lecture", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var sessionType = await _sessionTypeService.GetSessionTypeAsync("lec");

        // Assert
        Assert.NotNull(sessionType);
        Assert.Equal("LEC", sessionType.Id);
    }

    [Fact]
    public async Task GetSessionTypeAsync_TrimsWhitespace()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "LEC", Description = "Lecture", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var sessionType = await _sessionTypeService.GetSessionTypeAsync("  LEC  ");

        // Assert
        Assert.NotNull(sessionType);
        Assert.Equal("LEC", sessionType.Id);
    }

    #endregion

    #region CreateSessionTypeAsync Tests

    [Fact]
    public async Task CreateSessionTypeAsync_CreatesSessionType_WithValidRequest()
    {
        // Arrange
        var request = new CreateSessionTypeRequest
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
        var result = await _sessionTypeService.CreateSessionTypeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NEW", result.Id);
        Assert.Equal("New Type", result.Description);
        Assert.True(result.IsActive);
        Assert.Equal(0, result.UsageCount);
        Assert.True(result.CanDelete);

        var savedSessionType = await _context.SessionTypes.FirstOrDefaultAsync(s => s.Id == "NEW");
        Assert.NotNull(savedSessionType);
    }

    [Fact]
    public async Task CreateSessionTypeAsync_ThrowsInvalidOperationException_WhenDuplicateId()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "DUP", Description = "Existing", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new CreateSessionTypeRequest { Id = "DUP", Description = "Duplicate" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionTypeService.CreateSessionTypeAsync(request));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CreateSessionTypeAsync_NormalizesIdToUppercase()
    {
        // Arrange
        var request = new CreateSessionTypeRequest { Id = "low", Description = "Lowercase Test" };

        // Act
        var result = await _sessionTypeService.CreateSessionTypeAsync(request);

        // Assert
        Assert.Equal("LOW", result.Id);
        var savedSessionType = await _context.SessionTypes.FirstOrDefaultAsync(s => s.Id == "LOW");
        Assert.NotNull(savedSessionType);
    }

    [Fact]
    public async Task CreateSessionTypeAsync_TrimsWhitespace_FromIdAndDescription()
    {
        // Arrange
        var request = new CreateSessionTypeRequest { Id = "  PAD  ", Description = "  Padded Description  " };

        // Act
        var result = await _sessionTypeService.CreateSessionTypeAsync(request);

        // Assert
        Assert.Equal("PAD", result.Id);
        Assert.Equal("Padded Description", result.Description);
    }

    [Fact]
    public async Task CreateSessionTypeAsync_CallsAuditService()
    {
        // Arrange
        var request = new CreateSessionTypeRequest { Id = "AUD", Description = "Audited Type" };

        // Act
        await _sessionTypeService.CreateSessionTypeAsync(request);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddSessionTypeChangeAudit(
                "AUD",
                It.Is<string>(action => action.Contains("Create")),
                null,
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateSessionTypeAsync_SetsAllBooleanFlags()
    {
        // Arrange
        var request = new CreateSessionTypeRequest
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
        var result = await _sessionTypeService.CreateSessionTypeAsync(request);

        // Assert
        Assert.True(result.UsesWeeks);
        Assert.False(result.FacultyCanEnter);
        Assert.False(result.AllowedOnDvm);
        Assert.False(result.AllowedOn199299);
        Assert.False(result.AllowedOnRCourses);
    }

    #endregion

    #region UpdateSessionTypeAsync Tests

    [Fact]
    public async Task UpdateSessionTypeAsync_UpdatesSessionType_WhenExists()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "UPD", Description = "Original", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new UpdateSessionTypeRequest { Description = "Updated", IsActive = false, UsesWeeks = true };

        // Act
        var result = await _sessionTypeService.UpdateSessionTypeAsync("UPD", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Description);
        Assert.False(result.IsActive);
        Assert.True(result.UsesWeeks);
    }

    [Fact]
    public async Task UpdateSessionTypeAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var request = new UpdateSessionTypeRequest { Description = "Updated", IsActive = true };

        // Act
        var result = await _sessionTypeService.UpdateSessionTypeAsync("XXX", request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateSessionTypeAsync_NormalizesIdToUppercase()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "UPP", Description = "Original", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new UpdateSessionTypeRequest { Description = "Updated", IsActive = true };

        // Act
        var result = await _sessionTypeService.UpdateSessionTypeAsync("upp", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("UPP", result.Id);
    }

    [Fact]
    public async Task UpdateSessionTypeAsync_CallsAuditService()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "AUD", Description = "Original", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new UpdateSessionTypeRequest { Description = "Updated", IsActive = false };

        // Act
        await _sessionTypeService.UpdateSessionTypeAsync("AUD", request);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddSessionTypeChangeAudit(
                "AUD",
                It.Is<string>(action => action.Contains("Update")),
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateSessionTypeAsync_TrimsWhitespace_FromDescription()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "TRM", Description = "Original", IsActive = true });
        await _context.SaveChangesAsync();

        var request = new UpdateSessionTypeRequest { Description = "  Updated Name  ", IsActive = true };

        // Act
        var result = await _sessionTypeService.UpdateSessionTypeAsync("TRM", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Description);
    }

    [Fact]
    public async Task UpdateSessionTypeAsync_UpdatesAllBooleanFlags()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType
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

        var request = new UpdateSessionTypeRequest
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
        var result = await _sessionTypeService.UpdateSessionTypeAsync("FLG", request);

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

    #region DeleteSessionTypeAsync Tests

    [Fact]
    public async Task DeleteSessionTypeAsync_DeletesSessionType_WhenNoReferences()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "DEL", Description = "Deletable", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionTypeService.DeleteSessionTypeAsync("DEL");

        // Assert
        Assert.True(result);
        var deletedSessionType = await _context.SessionTypes.FindAsync("DEL");
        Assert.Null(deletedSessionType);
    }

    [Fact]
    public async Task DeleteSessionTypeAsync_ReturnsFalse_WhenReferencesExist()
    {
        // Arrange
        var sessionType = await CreateSessionTypeWithRecordsAsync("REF", "Referenced Type");

        // Act
        var result = await _sessionTypeService.DeleteSessionTypeAsync(sessionType.Id);

        // Assert
        Assert.False(result);
        var stillExists = await _context.SessionTypes.FindAsync(sessionType.Id);
        Assert.NotNull(stillExists);
    }

    [Fact]
    public async Task DeleteSessionTypeAsync_ReturnsFalse_WhenNotFound()
    {
        // Act
        var result = await _sessionTypeService.DeleteSessionTypeAsync("XXX");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteSessionTypeAsync_CallsAuditService()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "AUD", Description = "To Delete", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        await _sessionTypeService.DeleteSessionTypeAsync("AUD");

        // Assert
        _auditServiceMock.Verify(
            s => s.AddSessionTypeChangeAudit(
                "AUD",
                It.Is<string>(action => action.Contains("Delete")),
                It.IsAny<object>(),
                null),
            Times.Once);
    }

    [Fact]
    public async Task DeleteSessionTypeAsync_NormalizesIdToUppercase()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "LOW", Description = "Lowercase", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionTypeService.DeleteSessionTypeAsync("low");

        // Assert
        Assert.True(result);
        var deleted = await _context.SessionTypes.FindAsync("LOW");
        Assert.Null(deleted);
    }

    #endregion

    #region CanDeleteSessionTypeAsync Tests

    [Fact]
    public async Task CanDeleteSessionTypeAsync_ReturnsTrue_WhenNoReferences()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "CAN", Description = "Unused", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var canDelete = await _sessionTypeService.CanDeleteSessionTypeAsync("CAN");

        // Assert
        Assert.True(canDelete);
    }

    [Fact]
    public async Task CanDeleteSessionTypeAsync_ReturnsFalse_WhenReferencesExist()
    {
        // Arrange
        var sessionType = await CreateSessionTypeWithRecordsAsync("USE", "Used Type");

        // Act
        var canDelete = await _sessionTypeService.CanDeleteSessionTypeAsync(sessionType.Id);

        // Assert
        Assert.False(canDelete);
    }

    [Fact]
    public async Task CanDeleteSessionTypeAsync_NormalizesIdToUppercase()
    {
        // Arrange
        _context.SessionTypes.Add(new SessionType { Id = "UPP", Description = "Uppercase", IsActive = true });
        await _context.SaveChangesAsync();

        // Act
        var canDelete = await _sessionTypeService.CanDeleteSessionTypeAsync("upp");

        // Assert
        Assert.True(canDelete);
    }

    #endregion

    #region Case-Insensitive Duplicate Check Tests

    [Fact]
    public async Task CreateSessionTypeAsync_ThrowsInvalidOperationException_WhenDuplicateIdDifferentCase()
    {
        // Arrange - create a session type with uppercase ID
        _context.SessionTypes.Add(new SessionType { Id = "DUP", Description = "Existing", IsActive = true });
        await _context.SaveChangesAsync();

        // Try to create with lowercase version of same ID
        var request = new CreateSessionTypeRequest { Id = "dup", Description = "Duplicate Lowercase" };

        // Act & Assert - should fail because IDs are case-insensitive
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionTypeService.CreateSessionTypeAsync(request));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CreateSessionTypeAsync_ThrowsInvalidOperationException_WhenDuplicateIdMixedCase()
    {
        // Arrange - create a session type with lowercase ID
        _context.SessionTypes.Add(new SessionType { Id = "ABC", Description = "Existing", IsActive = true });
        await _context.SaveChangesAsync();

        // Try to create with mixed case version
        var request = new CreateSessionTypeRequest { Id = "AbC", Description = "Mixed Case" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionTypeService.CreateSessionTypeAsync(request));
        Assert.Contains("already exists", exception.Message);
    }

    #endregion
}
