using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;
using Viper.Areas.Effort.Services.Harvest;
using Viper.Classes.SQLContext;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for HarvestService harvest operations.
/// Tests focus on preview generation, harvest execution, and error handling.
/// Note: Clinical data tests are excluded as VIPERContext.Weeks/InstructorSchedules
/// require cross-database access not available in unit tests.
/// </summary>
public sealed class HarvestServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly CoursesContext _coursesContext;
    private readonly CrestContext _crestContext;
    private readonly AAUDContext _aaudContext;
    private readonly DictionaryContext _dictionaryContext;
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly Mock<ITermService> _termServiceMock;
    private readonly Mock<IInstructorService> _instructorServiceMock;
    private readonly Mock<ILogger<HarvestService>> _loggerMock;
    private readonly HarvestService _harvestService;

    private const int TestTermCode = 202410;

    public HarvestServiceTests()
    {
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var viperOptions = new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var coursesOptions = new DbContextOptionsBuilder<CoursesContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var crestOptions = new DbContextOptionsBuilder<CrestContext>()
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
        _coursesContext = new CoursesContext(coursesOptions);
        _crestContext = new CrestContext(crestOptions);
        _aaudContext = new AAUDContext(aaudOptions);
        _dictionaryContext = new DictionaryContext(dictionaryOptions);

        _auditServiceMock = new Mock<IEffortAuditService>();
        _termServiceMock = new Mock<ITermService>();
        _instructorServiceMock = new Mock<IInstructorService>();
        _loggerMock = new Mock<ILogger<HarvestService>>();

        // Setup default term service behavior
        _termServiceMock
            .Setup(s => s.GetTermName(It.IsAny<int>()))
            .Returns((int tc) => $"Term {tc}");

        // Setup default instructor service behavior
        _instructorServiceMock
            .Setup(s => s.ResolveInstructorDepartmentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("VET");

        _instructorServiceMock
            .Setup(s => s.GetTitleCodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Viper.Areas.Effort.Models.DTOs.Responses.TitleCodeDto>());

        _instructorServiceMock
            .Setup(s => s.GetDepartmentSimpleNameLookupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        // Setup audit service mock for harvest operations
        _auditServiceMock
            .Setup(s => s.ClearAuditForTermAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Create harvest phases
        var phases = new List<IHarvestPhase>
        {
            new CrestHarvestPhase(),
            new NonCrestHarvestPhase(),
            new ClinicalHarvestPhase(),
            new GuestAccountPhase()
        };

        _harvestService = new HarvestService(
            phases,
            _context,
            _viperContext,
            _coursesContext,
            _crestContext,
            _aaudContext,
            _dictionaryContext,
            _auditServiceMock.Object,
            _termServiceMock.Object,
            _instructorServiceMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _viperContext.Dispose();
        _coursesContext.Dispose();
        _crestContext.Dispose();
        _aaudContext.Dispose();
        _dictionaryContext.Dispose();
    }

    #region GeneratePreviewAsync Tests

    [Fact]
    public async Task GeneratePreviewAsync_WithNoData_ReturnsEmptyPreview()
    {
        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestTermCode, result.TermCode);
        Assert.Empty(result.CrestInstructors);
        Assert.Empty(result.CrestCourses);
        Assert.Empty(result.CrestEffort);
        Assert.Empty(result.NonCrestInstructors);
        Assert.Empty(result.NonCrestCourses);
        Assert.Empty(result.NonCrestEffort);
        Assert.NotNull(result.Summary);
        Assert.Equal(0, result.Summary.TotalInstructors);
        Assert.Equal(0, result.Summary.TotalCourses);
        Assert.Equal(0, result.Summary.TotalEffortRecords);
    }

    [Fact]
    public async Task GeneratePreviewAsync_WithExistingData_AddsWarning()
    {
        // Arrange - Add existing data that will be replaced
        // HarvestedDate makes status "Harvested"
        _context.Terms.Add(new EffortTerm
        {
            TermCode = TestTermCode,
            HarvestedDate = DateTime.Now.AddDays(-1)
        });
        _context.Persons.Add(new EffortPerson
        {
            PersonId = 1,
            TermCode = TestTermCode,
            FirstName = "EXISTING",
            LastName = "PERSON"
        });
        await _context.SaveChangesAsync();

        // Also add the person to VIPERContext so MothraId lookup works
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 1,
            ClientId = "EXIST001",
            MothraId = "EXIST001",
            FirstName = "EXISTING",
            LastName = "PERSON",
            FullName = "PERSON, EXISTING"
        });
        await _viperContext.SaveChangesAsync();

        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Warnings);
        Assert.Contains("Existing data will be replaced", result.Warnings[0].Message);
        Assert.Contains("1 instructors", result.Warnings[0].Details);
    }

    [Fact]
    public async Task GeneratePreviewAsync_CallsTermServiceForTermName()
    {
        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.Equal($"Term {TestTermCode}", result.TermName);
        _termServiceMock.Verify(s => s.GetTermName(TestTermCode), Times.Once);
    }

    #endregion

    #region ExecuteHarvestAsync Tests

    [Fact]
    public async Task ExecuteHarvestAsync_WithNoData_ReturnsSuccessWithEmptySummary()
    {
        // Arrange - Create term (no dates = "Created" status)
        _context.Terms.Add(new EffortTerm
        {
            TermCode = TestTermCode
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _harvestService.ExecuteHarvestAsync(TestTermCode, modifiedBy: 123);

        // Assert
        Assert.True(result.Success, $"Harvest failed with error: {result.ErrorMessage}");
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Summary);
        Assert.Equal(0, result.Summary.TotalInstructors);
        Assert.Equal(0, result.Summary.TotalCourses);
        Assert.Equal(0, result.Summary.TotalEffortRecords);
    }

    [Fact]
    public async Task ExecuteHarvestAsync_UpdatesTermStatus_ToHarvested()
    {
        // Arrange - no dates = "Created" status
        _context.Terms.Add(new EffortTerm
        {
            TermCode = TestTermCode
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _harvestService.ExecuteHarvestAsync(TestTermCode, modifiedBy: 123);

        // Assert
        Assert.True(result.Success);

        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == TestTermCode);
        Assert.NotNull(term);
        Assert.Equal("Harvested", term.Status); // Status computed from HarvestedDate
        Assert.NotNull(term.HarvestedDate);
    }

    [Fact]
    public async Task ExecuteHarvestAsync_ClearsExistingData_BeforeImporting()
    {
        // Arrange - Add existing data (HarvestedDate makes status "Harvested")
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode, HarvestedDate = DateTime.Now.AddDays(-7) });
        _context.Persons.Add(new EffortPerson
        {
            PersonId = 999,
            TermCode = TestTermCode,
            FirstName = "OLD",
            LastName = "PERSON"
        });
        _context.Courses.Add(new EffortCourse
        {
            TermCode = TestTermCode,
            Crn = "99999",
            SubjCode = "OLD",
            CrseNumb = "100",
            SeqNumb = "001"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _harvestService.ExecuteHarvestAsync(TestTermCode, modifiedBy: 123);

        // Assert
        Assert.True(result.Success);

        // Verify old data was cleared
        var remainingPersons = await _context.Persons.Where(p => p.TermCode == TestTermCode).ToListAsync();
        var remainingCourses = await _context.Courses.Where(c => c.TermCode == TestTermCode).ToListAsync();

        // With no new data to import, should be empty
        Assert.Empty(remainingPersons);
        Assert.Empty(remainingCourses);
    }

    [Fact]
    public async Task ExecuteHarvestAsync_CreatesAuditTrail()
    {
        // Arrange - no dates = "Created" status
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode });
        await _context.SaveChangesAsync();

        // Act
        var result = await _harvestService.ExecuteHarvestAsync(TestTermCode, modifiedBy: 123);

        // Assert
        Assert.True(result.Success);
        _auditServiceMock.Verify(
            s => s.AddTermChangeAudit(TestTermCode, It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>()),
            Times.Once);
        _auditServiceMock.Verify(
            s => s.AddImportAudit(TestTermCode, It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteHarvestAsync_SetsHarvestedDateInResult()
    {
        // Arrange - no dates = "Created" status
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode });
        await _context.SaveChangesAsync();

        var beforeHarvest = DateTime.Now;

        // Act
        var result = await _harvestService.ExecuteHarvestAsync(TestTermCode, modifiedBy: 123);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.HarvestedDate);
        Assert.True(result.HarvestedDate >= beforeHarvest);
    }

    #endregion

    #region Guest Account Tests

    [Fact]
    public async Task GeneratePreviewAsync_IncludesGuestAccounts_WhenFoundInViperPeople()
    {
        // Arrange - Add guest account to VIPER People
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 1001,
            ClientId = "APCGUEST",
            MothraId = "APCGUEST",
            FirstName = "GUEST",
            LastName = "APC",
            FullName = "APC, GUEST"
        });
        await _viperContext.SaveChangesAsync();

        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.Single(result.GuestAccounts);
        Assert.Equal("APCGUEST", result.GuestAccounts[0].MothraId);
        Assert.Equal(1001, result.GuestAccounts[0].PersonId);
        Assert.Equal("Guest", result.GuestAccounts[0].Source);
    }

    [Fact]
    public async Task GeneratePreviewAsync_SkipsMissingGuestAccounts()
    {
        // Arrange - No guest accounts in VIPER People

        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.Empty(result.GuestAccounts);
    }

    #endregion

    #region Summary Calculation Tests

    [Fact]
    public async Task GeneratePreviewAsync_CalculatesSummary_WithGuestAccountsOnly()
    {
        // Arrange - Add guest account
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 1001,
            ClientId = "APCGUEST",
            MothraId = "APCGUEST",
            FirstName = "GUEST",
            LastName = "APC",
            FullName = "APC, GUEST"
        });
        await _viperContext.SaveChangesAsync();

        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.NotNull(result.Summary);
        Assert.Equal(1, result.Summary.GuestAccounts);
        Assert.Equal(1, result.Summary.TotalInstructors); // Guest counts as instructor
    }

    [Fact]
    public async Task GeneratePreviewAsync_MultipleGuestAccounts_CountsCorrectly()
    {
        // Arrange - Add 3 guest accounts
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 1001,
            ClientId = "APCGUEST",
            MothraId = "APCGUEST",
            FirstName = "GUEST",
            LastName = "APC",
            FullName = "APC, GUEST"
        });
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 1002,
            ClientId = "VMEGUEST",
            MothraId = "VMEGUEST",
            FirstName = "GUEST",
            LastName = "VME",
            FullName = "VME, GUEST"
        });
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 1003,
            ClientId = "VSRGUEST",
            MothraId = "VSRGUEST",
            FirstName = "GUEST",
            LastName = "VSR",
            FullName = "VSR, GUEST"
        });
        await _viperContext.SaveChangesAsync();

        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.Equal(3, result.GuestAccounts.Count);
        Assert.Equal(3, result.Summary.GuestAccounts);
        Assert.Equal(3, result.Summary.TotalInstructors);
    }

    #endregion

    #region IsNew Flag Tests

    [Fact]
    public async Task GeneratePreviewAsync_NewGuestAccount_SetsIsNewTrue()
    {
        // Arrange - Add guest account to VIPER but NOT to EffortPerson
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 1001,
            ClientId = "APCGUEST",
            MothraId = "APCGUEST",
            FirstName = "GUEST",
            LastName = "APC",
            FullName = "APC, GUEST"
        });
        await _viperContext.SaveChangesAsync();

        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.Single(result.GuestAccounts);
        Assert.True(result.GuestAccounts[0].IsNew);
    }

    [Fact]
    public async Task GeneratePreviewAsync_ExistingGuestAccount_SetsIsNewFalse()
    {
        // Arrange - Add guest account to both VIPER and EffortPerson
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 1001,
            ClientId = "APCGUEST",
            MothraId = "APCGUEST",
            FirstName = "GUEST",
            LastName = "APC",
            FullName = "APC, GUEST"
        });
        await _viperContext.SaveChangesAsync();

        _context.Persons.Add(new EffortPerson
        {
            PersonId = 1001,
            TermCode = TestTermCode,
            FirstName = "GUEST",
            LastName = "APC"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.Single(result.GuestAccounts);
        Assert.False(result.GuestAccounts[0].IsNew);
    }

    #endregion

    #region Removed Items Detection Tests

    [Fact]
    public async Task GeneratePreviewAsync_InstructorNotInHarvest_AddsToRemovedList()
    {
        // Arrange - Add existing instructor that won't be in harvest sources
        _context.Persons.Add(new EffortPerson
        {
            PersonId = 999,
            TermCode = TestTermCode,
            FirstName = "OLD",
            LastName = "INSTRUCTOR"
        });
        await _context.SaveChangesAsync();

        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 999,
            ClientId = "OLDINST",
            MothraId = "OLDINST",
            FirstName = "OLD",
            LastName = "INSTRUCTOR",
            FullName = "INSTRUCTOR, OLD"
        });
        await _viperContext.SaveChangesAsync();

        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.Single(result.RemovedInstructors);
        Assert.Equal("INSTRUCTOR, OLD", result.RemovedInstructors[0].FullName);
    }

    [Fact]
    public async Task GeneratePreviewAsync_CourseNotInHarvest_AddsToRemovedList()
    {
        // Arrange - Add existing course that won't be in harvest sources
        _context.Courses.Add(new EffortCourse
        {
            TermCode = TestTermCode,
            Crn = "99999",
            SubjCode = "OLD",
            CrseNumb = "100",
            SeqNumb = "001"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.Single(result.RemovedCourses);
        Assert.Equal("99999", result.RemovedCourses[0].Crn);
        Assert.Equal("OLD", result.RemovedCourses[0].SubjCode);
    }

    [Fact]
    public async Task GeneratePreviewAsync_NoExistingData_EmptyRemovedLists()
    {
        // Arrange - No existing data

        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.Empty(result.RemovedInstructors);
        Assert.Empty(result.RemovedCourses);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteHarvestAsync_WithMissingTerm_ReturnsError()
    {
        // Arrange - No term in database

        // Act
        var result = await _harvestService.ExecuteHarvestAsync(999999, modifiedBy: 123);

        // Assert - When term not found, harvest still runs but fails during preview generation
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteHarvestAsync_ReturnsTermCodeInResult()
    {
        // Arrange - no dates = "Created" status
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode });
        await _context.SaveChangesAsync();

        // Act
        var result = await _harvestService.ExecuteHarvestAsync(TestTermCode, modifiedBy: 123);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TestTermCode, result.TermCode);
    }

    #endregion

    #region Warning Details Tests

    [Fact]
    public async Task GeneratePreviewAsync_WithExistingData_WarningIncludesCorrectCounts()
    {
        // Arrange - Add 2 instructors, 3 courses, and records (HarvestedDate makes status "Harvested")
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode, HarvestedDate = DateTime.Now.AddDays(-7) });

        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = TestTermCode, FirstName = "A", LastName = "B" });
        _context.Persons.Add(new EffortPerson { PersonId = 2, TermCode = TestTermCode, FirstName = "C", LastName = "D" });

        _context.Courses.Add(new EffortCourse { TermCode = TestTermCode, Crn = "11111", SubjCode = "T", CrseNumb = "1", SeqNumb = "1" });
        _context.Courses.Add(new EffortCourse { TermCode = TestTermCode, Crn = "22222", SubjCode = "T", CrseNumb = "2", SeqNumb = "1" });
        _context.Courses.Add(new EffortCourse { TermCode = TestTermCode, Crn = "33333", SubjCode = "T", CrseNumb = "3", SeqNumb = "1" });

        await _context.SaveChangesAsync();

        // Also add the persons to VIPERContext so MothraId lookup works
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 1,
            ClientId = "USER001",
            MothraId = "USER001",
            FirstName = "A",
            LastName = "B",
            FullName = "B, A"
        });
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 2,
            ClientId = "USER002",
            MothraId = "USER002",
            FirstName = "C",
            LastName = "D",
            FullName = "D, C"
        });
        await _viperContext.SaveChangesAsync();

        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.Single(result.Warnings);
        Assert.Contains("2 instructors", result.Warnings[0].Details);
        Assert.Contains("3 courses", result.Warnings[0].Details);
    }

    #endregion
}
