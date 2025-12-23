using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Models;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for InstructorService instructor management operations.
/// </summary>
public sealed class InstructorServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly AAUDContext _aaudContext;
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly Mock<ILogger<InstructorService>> _loggerMock;
    private readonly IConfiguration _configurationMock;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;
    private readonly InstructorService _instructorService;

    public InstructorServiceTests()
    {
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var viperOptions = new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var aaudOptions = new DbContextOptionsBuilder<AAUDContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EffortDbContext(effortOptions);
        _viperContext = new VIPERContext(viperOptions);
        _aaudContext = new AAUDContext(aaudOptions);
        _auditServiceMock = new Mock<IEffortAuditService>();
        _loggerMock = new Mock<ILogger<InstructorService>>();

        // Setup configuration with empty connection strings section to prevent null reference
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ConnectionStrings:VIPER", "" }
        });
        _configurationMock = configurationBuilder.Build();

        _cache = new MemoryCache(new MemoryCacheOptions());

        // Configure AutoMapper with the Effort profile
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperProfileEffort>();
        });
        _mapper = mapperConfig.CreateMapper();

        // Setup synchronous audit methods used within transactions
        _auditServiceMock
            .Setup(s => s.AddPersonChangeAudit(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>()));

        _instructorService = new InstructorService(
            _context,
            _viperContext,
            _aaudContext,
            _auditServiceMock.Object,
            _mapper,
            _loggerMock.Object,
            _configurationMock,
            _cache);
    }

    public void Dispose()
    {
        _context.Dispose();
        _viperContext.Dispose();
        _aaudContext.Dispose();
        _cache.Dispose();
    }

    #region GetInstructorsAsync Tests

    [Fact]
    public async Task GetInstructorsAsync_ReturnsAllInstructorsForTerm_OrderedByLastNameFirstName()
    {
        // Arrange
        _context.Persons.AddRange(
            new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234" },
            new EffortPerson { PersonId = 2, TermCode = 202410, FirstName = "Alice", LastName = "Smith", EffortDept = "VME", EffortTitleCode = "1234" },
            new EffortPerson { PersonId = 3, TermCode = 202410, FirstName = "Bob", LastName = "Doe", EffortDept = "APC", EffortTitleCode = "1234" }
        );
        await _context.SaveChangesAsync();

        // Act
        var instructors = await _instructorService.GetInstructorsAsync(202410);

        // Assert
        Assert.Equal(3, instructors.Count);
        Assert.Equal("Doe", instructors[0].LastName);
        Assert.Equal("Bob", instructors[0].FirstName);
        Assert.Equal("Doe", instructors[1].LastName);
        Assert.Equal("John", instructors[1].FirstName);
        Assert.Equal("Smith", instructors[2].LastName);
    }

    [Fact]
    public async Task GetInstructorsAsync_FiltersByDepartment_WhenDepartmentProvided()
    {
        // Arrange
        _context.Persons.AddRange(
            new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234" },
            new EffortPerson { PersonId = 2, TermCode = 202410, FirstName = "Jane", LastName = "Smith", EffortDept = "APC", EffortTitleCode = "1234" }
        );
        await _context.SaveChangesAsync();

        // Act
        var instructors = await _instructorService.GetInstructorsAsync(202410, "VME");

        // Assert
        Assert.Single(instructors);
        Assert.Equal("VME", instructors[0].EffortDept);
    }

    [Fact]
    public async Task GetInstructorsAsync_ReturnsEmptyList_WhenNoInstructorsExistForTerm()
    {
        // Act
        var instructors = await _instructorService.GetInstructorsAsync(999999);

        // Assert
        Assert.Empty(instructors);
    }

    #endregion

    #region GetInstructorAsync Tests

    [Fact]
    public async Task GetInstructorAsync_ReturnsInstructor_WhenInstructorExists()
    {
        // Arrange
        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234" });
        await _context.SaveChangesAsync();

        // Act
        var instructor = await _instructorService.GetInstructorAsync(1, 202410);

        // Assert
        Assert.NotNull(instructor);
        Assert.Equal(1, instructor.PersonId);
        Assert.Equal("John", instructor.FirstName);
        Assert.Equal("Doe", instructor.LastName);
    }

    [Fact]
    public async Task GetInstructorAsync_ReturnsNull_WhenInstructorDoesNotExist()
    {
        // Act
        var instructor = await _instructorService.GetInstructorAsync(999, 202410);

        // Assert
        Assert.Null(instructor);
    }

    [Fact]
    public async Task GetInstructorAsync_ReturnsNull_WhenWrongTermCode()
    {
        // Arrange
        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234" });
        await _context.SaveChangesAsync();

        // Act
        var instructor = await _instructorService.GetInstructorAsync(1, 202320);

        // Assert
        Assert.Null(instructor);
    }

    #endregion

    #region InstructorExistsAsync Tests

    [Fact]
    public async Task InstructorExistsAsync_ReturnsTrue_WhenInstructorExists()
    {
        // Arrange
        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234" });
        await _context.SaveChangesAsync();

        // Act & Assert
        Assert.True(await _instructorService.InstructorExistsAsync(1, 202410));
    }

    [Fact]
    public async Task InstructorExistsAsync_ReturnsFalse_WhenInstructorDoesNotExist()
    {
        // Act & Assert
        Assert.False(await _instructorService.InstructorExistsAsync(999, 202410));
    }

    #endregion

    #region UpdateInstructorAsync Tests

    [Fact]
    public async Task UpdateInstructorAsync_UpdatesInstructor_WithValidData()
    {
        // Arrange
        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234", VolunteerWos = 0 });
        await _context.SaveChangesAsync();

        var request = new UpdateInstructorRequest
        {
            EffortDept = "APC",
            EffortTitleCode = "5678",
            VolunteerWos = true
        };

        // Act
        var instructor = await _instructorService.UpdateInstructorAsync(1, 202410, request);

        // Assert
        Assert.NotNull(instructor);
        Assert.Equal("APC", instructor.EffortDept);
        Assert.Equal("5678", instructor.EffortTitleCode);
        Assert.True(instructor.VolunteerWos);
    }

    [Fact]
    public async Task UpdateInstructorAsync_ReturnsNull_WhenInstructorDoesNotExist()
    {
        // Arrange
        var request = new UpdateInstructorRequest
        {
            EffortDept = "APC",
            EffortTitleCode = "5678",
            VolunteerWos = false
        };

        // Act
        var instructor = await _instructorService.UpdateInstructorAsync(999, 202410, request);

        // Assert
        Assert.Null(instructor);
    }

    [Fact]
    public async Task UpdateInstructorAsync_ThrowsArgumentException_ForInvalidDepartment()
    {
        // Arrange
        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234" });
        await _context.SaveChangesAsync();

        var request = new UpdateInstructorRequest
        {
            EffortDept = "INVALID",
            EffortTitleCode = "5678",
            VolunteerWos = false
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _instructorService.UpdateInstructorAsync(1, 202410, request)
        );
        Assert.Contains("Invalid department", exception.Message);
    }

    [Fact]
    public async Task UpdateInstructorAsync_CreatesAuditEntryWithOldAndNewValues()
    {
        // Arrange
        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234" });
        await _context.SaveChangesAsync();

        var request = new UpdateInstructorRequest
        {
            EffortDept = "APC",
            EffortTitleCode = "5678",
            VolunteerWos = false
        };

        // Act
        await _instructorService.UpdateInstructorAsync(1, 202410, request);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddPersonChangeAudit(
                1,
                202410,
                "UpdatePerson",
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Once);
    }

    #endregion

    #region DeleteInstructorAsync Tests

    [Fact]
    public async Task DeleteInstructorAsync_DeletesInstructor_WhenInstructorExists()
    {
        // Arrange
        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _instructorService.DeleteInstructorAsync(1, 202410);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.Persons.FirstOrDefaultAsync(p => p.PersonId == 1 && p.TermCode == 202410));
    }

    [Fact]
    public async Task DeleteInstructorAsync_ReturnsFalse_WhenInstructorDoesNotExist()
    {
        // Act
        var result = await _instructorService.DeleteInstructorAsync(999, 202410);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteInstructorAsync_DeletesAssociatedRecords()
    {
        // Arrange
        var person = new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234" };
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        _context.Records.AddRange(
            new EffortRecord { Id = 1, TermCode = 202410, CourseId = 100, PersonId = 1 },
            new EffortRecord { Id = 2, TermCode = 202410, CourseId = 101, PersonId = 1 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _instructorService.DeleteInstructorAsync(1, 202410);

        // Assert
        Assert.True(result);
        Assert.Empty(await _context.Records.Where(r => r.PersonId == 1).ToListAsync());
    }

    [Fact]
    public async Task DeleteInstructorAsync_CreatesAuditEntry()
    {
        // Arrange
        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234" });
        await _context.SaveChangesAsync();

        // Act
        await _instructorService.DeleteInstructorAsync(1, 202410);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddPersonChangeAudit(
                1,
                202410,
                "DeleteInstructor",
                It.IsAny<object>(),
                null),
            Times.Once);
    }

    #endregion

    #region CanDeleteInstructorAsync Tests

    [Fact]
    public async Task CanDeleteInstructorAsync_ReturnsTrueAndZeroCount_WhenNoRecords()
    {
        // Arrange
        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234" });
        await _context.SaveChangesAsync();

        // Act
        var (canDelete, recordCount) = await _instructorService.CanDeleteInstructorAsync(1, 202410);

        // Assert
        Assert.True(canDelete);
        Assert.Equal(0, recordCount);
    }

    [Fact]
    public async Task CanDeleteInstructorAsync_ReturnsTrueWithRecordCount_WhenRecordsExist()
    {
        // Arrange
        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234" });
        _context.Records.AddRange(
            new EffortRecord { Id = 1, TermCode = 202410, CourseId = 100, PersonId = 1 },
            new EffortRecord { Id = 2, TermCode = 202410, CourseId = 101, PersonId = 1 },
            new EffortRecord { Id = 3, TermCode = 202410, CourseId = 102, PersonId = 1 }
        );
        await _context.SaveChangesAsync();

        // Act
        var (canDelete, recordCount) = await _instructorService.CanDeleteInstructorAsync(1, 202410);

        // Assert
        Assert.True(canDelete);
        Assert.Equal(3, recordCount);
    }

    #endregion

    #region GetDepartments / IsValidDepartment Tests

    [Fact]
    public void GetDepartments_ReturnsAllDepartments()
    {
        // Act
        var departments = _instructorService.GetDepartments();

        // Assert
        Assert.Contains(departments, d => d.Code == "VME");
        Assert.Contains(departments, d => d.Code == "APC");
        Assert.Contains(departments, d => d.Code == "WHC");
        Assert.Contains(departments, d => d.Group == "Academic");
        Assert.Contains(departments, d => d.Group == "Centers");
    }

    [Fact]
    public void GetValidDepartments_ReturnsAllValidDepartmentCodes()
    {
        // Act
        var departments = _instructorService.GetValidDepartments();

        // Assert
        Assert.Contains("VME", departments);
        Assert.Contains("APC", departments);
        Assert.Contains("WHC", departments);
    }

    [Fact]
    public void IsValidDepartment_ReturnsFalse_ForInvalidDepartment()
    {
        // Act & Assert
        Assert.False(_instructorService.IsValidDepartment("INVALID"));
    }

    [Fact]
    public void IsValidDepartment_ReturnsTrue_ForValidDepartment()
    {
        // Act & Assert
        Assert.True(_instructorService.IsValidDepartment("VME"));
        Assert.True(_instructorService.IsValidDepartment("vme")); // Case insensitive
    }

    #endregion

    #region AutoMapper PersonDto Mapping Tests

    [Fact]
    public async Task GetInstructorAsync_MapsVolunteerWosCorrectly()
    {
        // Arrange - VolunteerWos is byte? in entity, bool in DTO
        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME", EffortTitleCode = "1234", VolunteerWos = 1 });
        _context.Persons.Add(new EffortPerson { PersonId = 2, TermCode = 202410, FirstName = "Jane", LastName = "Smith", EffortDept = "VME", EffortTitleCode = "1234", VolunteerWos = 0 });
        await _context.SaveChangesAsync();

        // Act
        var instructor1 = await _instructorService.GetInstructorAsync(1, 202410);
        var instructor2 = await _instructorService.GetInstructorAsync(2, 202410);

        // Assert
        Assert.NotNull(instructor1);
        Assert.True(instructor1.VolunteerWos);
        Assert.NotNull(instructor2);
        Assert.False(instructor2.VolunteerWos);
    }

    #endregion
}
