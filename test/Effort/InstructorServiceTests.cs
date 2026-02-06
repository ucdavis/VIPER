using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly DictionaryContext _dictionaryContext;
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly Mock<ICourseClassificationService> _classificationServiceMock;
    private readonly Mock<ILogger<InstructorService>> _loggerMock;
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

        var dictionaryOptions = new DbContextOptionsBuilder<DictionaryContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EffortDbContext(effortOptions);
        _viperContext = new VIPERContext(viperOptions);
        _aaudContext = new AAUDContext(aaudOptions);
        _dictionaryContext = new DictionaryContext(dictionaryOptions);
        _auditServiceMock = new Mock<IEffortAuditService>();
        _classificationServiceMock = new Mock<ICourseClassificationService>();
        _loggerMock = new Mock<ILogger<InstructorService>>();
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
            _dictionaryContext,
            _auditServiceMock.Object,
            _classificationServiceMock.Object,
            _mapper,
            _loggerMock.Object,
            _cache);
    }

    public void Dispose()
    {
        _context.Dispose();
        _viperContext.Dispose();
        _aaudContext.Dispose();
        _dictionaryContext.Dispose();
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

    #region CreateInstructorAsync Tests

    [Fact]
    public async Task CreateInstructorAsync_ThrowsInstructorAlreadyExistsException_WhenInstructorExists()
    {
        // Arrange - instructor already exists
        _context.Persons.Add(new EffortPerson
        {
            PersonId = 1,
            TermCode = 202410,
            FirstName = "John",
            LastName = "Doe",
            EffortDept = "VME",
            EffortTitleCode = "1234"
        });
        await _context.SaveChangesAsync();

        var request = new CreateInstructorRequest { PersonId = 1, TermCode = 202410 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Areas.Effort.Exceptions.InstructorAlreadyExistsException>(
            () => _instructorService.CreateInstructorAsync(request));

        Assert.Equal(1, ex.PersonId);
        Assert.Equal(202410, ex.TermCode);
        Assert.Contains("already exists", ex.Message);
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

    #region ResolveInstructorDepartmentAsync / DetermineDepartment Tests

    [Fact]
    public async Task ResolveInstructorDepartmentAsync_ReturnsDepartmentOverride_WhenMothraIdHasOverride()
    {
        // Arrange - Department override should return configured dept regardless of jobs/employee data
        // Using the MothraId from EffortConstants.DepartmentOverrides
        var deptOverrides = Areas.Effort.Constants.EffortConstants.DepartmentOverrides;
        var overrideMothraId = deptOverrides.Keys.First();
        var expectedDept = deptOverrides[overrideMothraId];

        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 100,
            ClientId = overrideMothraId,
            FirstName = "Test",
            LastName = "Override",
            FullName = "Override, Test",
            MothraId = overrideMothraId, // This MothraId has a department override configured
            CurrentEmployee = true
        });
        await _viperContext.SaveChangesAsync();

        // Add VMDO job (should be ignored due to override)
        _aaudContext.Ids.Add(new Viper.Models.AAUD.Id
        {
            IdsPKey = "OVERRIDE001",
            IdsTermCode = "202410",
            IdsMothraid = overrideMothraId,
            IdsClientid = overrideMothraId
        });
        _aaudContext.Employees.Add(new Viper.Models.AAUD.Employee
        {
            EmpPKey = "OVERRIDE001",
            EmpTermCode = "202410",
            EmpClientid = overrideMothraId,
            EmpHomeDept = "072000", // VMDO - should be ignored
            EmpAltDeptCode = "",
            EmpSchoolDivision = "VM",
            EmpCbuc = "99",
            EmpStatus = "A"
        });
        await _aaudContext.SaveChangesAsync();

        // Act
        var dept = await _instructorService.ResolveInstructorDepartmentAsync(100, 202410);

        // Assert - Should return the configured override department
        Assert.Equal(expectedDept, dept);
    }

    [Fact]
    public async Task ResolveInstructorDepartmentAsync_ReturnsAcademicDeptFromJobs_WhenAvailable()
    {
        // Arrange
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 1,
            ClientId = "12345678",
            FirstName = "Test",
            LastName = "User",
            FullName = "User, Test",
            MothraId = "12345678",
            CurrentEmployee = true
        });
        await _viperContext.SaveChangesAsync();

        _aaudContext.Ids.Add(new Viper.Models.AAUD.Id
        {
            IdsPKey = "TEST001",
            IdsTermCode = "202410",
            IdsMothraid = "12345678",
            IdsClientid = "12345678"
        });

        // Add employee with non-academic dept
        _aaudContext.Employees.Add(new Viper.Models.AAUD.Employee
        {
            EmpPKey = "TEST001",
            EmpTermCode = "202410",
            EmpClientid = "12345678",
            EmpHomeDept = "072000", // Non-academic
            EmpAltDeptCode = "",
            EmpSchoolDivision = "VM",
            EmpCbuc = "99",
            EmpStatus = "A"
        });

        // Add job with academic dept code (VME = 072030)
        _aaudContext.Jobs.Add(new Viper.Models.AAUD.Job
        {
            JobPKey = "TEST001",
            JobSeqNum = 1,
            JobTermCode = "202410",
            JobClientid = "12345678",
            JobDepartmentCode = "VME", // Academic dept - should be found
            JobPercentFulltime = 100,
            JobTitleCode = "1234",
            JobBargainingUnit = "99",
            JobSchoolDivision = "VM"
        });
        await _aaudContext.SaveChangesAsync();

        // Act
        var dept = await _instructorService.ResolveInstructorDepartmentAsync(1, 202410);

        // Assert - Should return VME from jobs table
        Assert.Equal("VME", dept);
    }

    [Fact]
    public async Task ResolveInstructorDepartmentAsync_FallsBackToEmployeeFields_WhenNoAcademicJobDept()
    {
        // Arrange
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 2,
            ClientId = "87654321",
            FirstName = "Test",
            LastName = "User2",
            FullName = "User2, Test",
            MothraId = "87654321",
            CurrentEmployee = true
        });
        await _viperContext.SaveChangesAsync();

        _aaudContext.Ids.Add(new Viper.Models.AAUD.Id
        {
            IdsPKey = "TEST002",
            IdsTermCode = "202410",
            IdsMothraid = "87654321",
            IdsClientid = "87654321"
        });

        // Add employee with academic effort dept
        _aaudContext.Employees.Add(new Viper.Models.AAUD.Employee
        {
            EmpPKey = "TEST002",
            EmpTermCode = "202410",
            EmpClientid = "87654321",
            EmpEffortHomeDept = "APC", // Academic dept in employee record
            EmpHomeDept = "072000",
            EmpAltDeptCode = "",
            EmpSchoolDivision = "VM",
            EmpCbuc = "99",
            EmpStatus = "A"
        });

        // No jobs or only non-academic jobs
        await _aaudContext.SaveChangesAsync();

        // Act
        var dept = await _instructorService.ResolveInstructorDepartmentAsync(2, 202410);

        // Assert - Should return APC from employee effort dept
        Assert.Equal("APC", dept);
    }

    [Fact]
    public async Task ResolveInstructorDepartmentAsync_ReturnsNull_WhenNoPersonFound()
    {
        // Act
        var dept = await _instructorService.ResolveInstructorDepartmentAsync(99999, 202410);

        // Assert - Returns null when person doesn't exist (distinct from "UNK" for person exists but no dept)
        Assert.Null(dept);
    }

    [Fact]
    public async Task ResolveInstructorDepartmentAsync_ReturnsUNK_WhenNoEmployeeOrJobsFound()
    {
        // Arrange - Person exists but no AAUD data
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 3,
            ClientId = "00000000",
            FirstName = "No",
            LastName = "Data",
            FullName = "Data, No",
            MothraId = "00000000",
            CurrentEmployee = true
        });
        await _viperContext.SaveChangesAsync();

        // Act
        var dept = await _instructorService.ResolveInstructorDepartmentAsync(3, 202410);

        // Assert
        Assert.Equal("UNK", dept);
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

    [Fact]
    public async Task GetInstructorAsync_MapsLastEmailedFieldsCorrectly()
    {
        // Arrange - LastEmailed maps to LastEmailedDate, LastEmailedBy resolved to sender name
        var emailedDate = new DateTime(2024, 10, 15, 14, 30, 0, DateTimeKind.Local);
        var senderId = 999;

        _context.Persons.Add(new EffortPerson
        {
            PersonId = 1,
            TermCode = 202410,
            FirstName = "John",
            LastName = "Doe",
            EffortDept = "VME",
            EffortTitleCode = "1234",
            LastEmailed = emailedDate,
            LastEmailedBy = senderId
        });
        _context.Persons.Add(new EffortPerson
        {
            PersonId = 2,
            TermCode = 202410,
            FirstName = "Jane",
            LastName = "Smith",
            EffortDept = "VME",
            EffortTitleCode = "1234",
            LastEmailed = null,
            LastEmailedBy = null
        });
        await _context.SaveChangesAsync();

        // Add sender to ViperPersons (cross-schema reference in EffortDbContext) for name lookup
        _context.ViperPersons.Add(new ViperPerson
        {
            PersonId = senderId,
            FirstName = "Admin",
            LastName = "User"
        });
        await _context.SaveChangesAsync();

        // Act
        var instructor1 = await _instructorService.GetInstructorAsync(1, 202410);
        var instructor2 = await _instructorService.GetInstructorAsync(2, 202410);

        // Assert - instructor with email history
        Assert.NotNull(instructor1);
        Assert.Equal(emailedDate, instructor1.LastEmailedDate);
        Assert.Equal("Admin User", instructor1.LastEmailedBy);

        // Assert - instructor without email history
        Assert.NotNull(instructor2);
        Assert.Null(instructor2.LastEmailedDate);
        Assert.Null(instructor2.LastEmailedBy);
    }

    #endregion

    #region GetTitleCodesAsync Tests

    [Fact]
    public async Task GetTitleCodesAsync_ReturnsEmptyList_WhenConnectionStringEmpty()
    {
        // The test setup uses empty connection strings, so the method should return empty list
        // This tests the graceful fallback behavior when the database is not available
        var titleCodes = await _instructorService.GetTitleCodesAsync();

        Assert.Empty(titleCodes);
    }

    #endregion

    #region GetJobGroupsAsync Tests

    [Fact]
    public async Task GetJobGroupsAsync_ReturnsEmptyList_WhenConnectionStringEmpty()
    {
        // The test setup uses empty connection strings, so the method should return empty list
        // This tests the graceful fallback behavior when the database is not available
        var jobGroups = await _instructorService.GetJobGroupsAsync();

        Assert.Empty(jobGroups);
    }

    #endregion
}
