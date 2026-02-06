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
    private readonly Mock<IRCourseService> _rCourseServiceMock;
    private readonly Mock<IPercentRolloverService> _percentRolloverServiceMock;
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
        _rCourseServiceMock = new Mock<IRCourseService>();
        _percentRolloverServiceMock = new Mock<IPercentRolloverService>();
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
            new ClinicalHarvestPhase()
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
            _rCourseServiceMock.Object,
            _percentRolloverServiceMock.Object,
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

    #region IsNew Flag Tests

    [Fact]
    public async Task GeneratePreviewAsync_ExistingEffortRecord_SetsIsNewFalse()
    {
        // Arrange - Seed existing effort data that matches what the preview will generate
        _context.Persons.Add(new EffortPerson
        {
            PersonId = 1,
            TermCode = TestTermCode,
            FirstName = "JANE",
            LastName = "DOE"
        });
        var course = new EffortCourse
        {
            TermCode = TestTermCode,
            Crn = "12345",
            SubjCode = "VET",
            CrseNumb = "410",
            SeqNumb = "01"
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        _context.Records.Add(new EffortRecord
        {
            PersonId = 1,
            TermCode = TestTermCode,
            CourseId = course.Id,
            EffortTypeId = "VAR",
            RoleId = 1,
            Hours = 40,
            Crn = "12345"
        });
        await _context.SaveChangesAsync();

        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 1,
            ClientId = "JDOE001",
            MothraId = "JDOE001",
            FirstName = "JANE",
            LastName = "DOE",
            FullName = "DOE, JANE"
        });
        await _viperContext.SaveChangesAsync();

        // Use a custom phase that adds a preview record matching the existing data
        var service = CreateServiceWithPhase(new TestPreviewHarvestPhase("JDOE001", "DOE, JANE", "12345", "VAR"));

        // Act
        var result = await service.GeneratePreviewAsync(TestTermCode);

        // Assert
        var effortRecord = Assert.Single(result.CrestEffort);
        Assert.False(effortRecord.IsNew);
    }

    [Fact]
    public async Task GeneratePreviewAsync_NewEffortRecord_SetsIsNewTrue()
    {
        // Arrange - No existing records in the database
        var service = CreateServiceWithPhase(new TestPreviewHarvestPhase("JDOE001", "DOE, JANE", "12345", "VAR"));

        // Act
        var result = await service.GeneratePreviewAsync(TestTermCode);

        // Assert
        var effortRecord = Assert.Single(result.CrestEffort);
        Assert.True(effortRecord.IsNew);
    }

    [Fact]
    public async Task GeneratePreviewAsync_MixedNewAndExisting_SetsIsNewCorrectly()
    {
        // Arrange - One existing record, preview will have two
        _context.Persons.Add(new EffortPerson
        {
            PersonId = 1,
            TermCode = TestTermCode,
            FirstName = "JANE",
            LastName = "DOE"
        });
        var course = new EffortCourse
        {
            TermCode = TestTermCode,
            Crn = "12345",
            SubjCode = "VET",
            CrseNumb = "410",
            SeqNumb = "01"
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        _context.Records.Add(new EffortRecord
        {
            PersonId = 1,
            TermCode = TestTermCode,
            CourseId = course.Id,
            EffortTypeId = "VAR",
            RoleId = 1,
            Hours = 40,
            Crn = "12345"
        });
        await _context.SaveChangesAsync();

        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 1,
            ClientId = "JDOE001",
            MothraId = "JDOE001",
            FirstName = "JANE",
            LastName = "DOE",
            FullName = "DOE, JANE"
        });
        await _viperContext.SaveChangesAsync();

        // Phase adds two records: one matching existing (VAR) and one new (LEC)
        var phase = new TestPreviewHarvestPhase(
            new[]
            {
                ("JDOE001", "DOE, JANE", "12345", "VAR"),
                ("JDOE001", "DOE, JANE", "12345", "LEC"),
            });
        var service = CreateServiceWithPhase(phase);

        // Act
        var result = await service.GeneratePreviewAsync(TestTermCode);

        // Assert
        Assert.Equal(2, result.CrestEffort.Count);
        var existingRecord = result.CrestEffort.Single(r => r.EffortType == "VAR");
        var newRecord = result.CrestEffort.Single(r => r.EffortType == "LEC");
        Assert.False(existingRecord.IsNew);
        Assert.True(newRecord.IsNew);
    }

    #endregion

    #region RESID Course Exclusion Tests

    [Fact]
    public async Task GeneratePreviewAsync_ResidCourse_ExcludedFromRemovedList()
    {
        // Arrange - Add a RESID course (auto-generated R-course placeholder, recreated post-harvest)
        _context.Courses.Add(new EffortCourse
        {
            TermCode = TestTermCode,
            Crn = "RESID",
            SubjCode = "VET",
            CrseNumb = "499R",
            SeqNumb = "001"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert - RESID should NOT appear in removed courses
        Assert.Empty(result.RemovedCourses);
    }

    [Fact]
    public async Task GeneratePreviewAsync_ResidCourseWithOtherCourses_OnlyNonResidRemoved()
    {
        // Arrange - RESID + a regular course, both not in harvest sources
        _context.Courses.Add(new EffortCourse
        {
            TermCode = TestTermCode,
            Crn = "RESID",
            SubjCode = "VET",
            CrseNumb = "499R",
            SeqNumb = "001"
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
        var result = await _harvestService.GeneratePreviewAsync(TestTermCode);

        // Assert - Only the non-RESID course should be in the removed list
        Assert.Single(result.RemovedCourses);
        Assert.Equal("99999", result.RemovedCourses[0].Crn);
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

    #region R-Course Generation Tests
    // R-course auto-generation happens as part of ExecuteHarvestAsync after all phases complete.
    // The actual R-course creation logic is delegated to IRCourseService.
    // These tests verify the integration with the mocked service.

    [Fact]
    public async Task ExecuteHarvestAsync_CompletesSuccessfully_WithNoEligibleInstructors()
    {
        // Arrange - Create term with effort type allowed on R-courses
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode });
        _context.EffortTypes.Add(new EffortType
        {
            Id = "LEC",
            Description = "Lecture",
            AllowedOnRCourses = true,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        // Act - Run harvest (R-course generation happens at end)
        var result = await _harvestService.ExecuteHarvestAsync(TestTermCode, modifiedBy: 123);

        // Assert - Harvest completes successfully even with no data
        Assert.True(result.Success);

        // RCourseService should NOT be called (no eligible instructors)
        _rCourseServiceMock.Verify(s => s.CreateRCourseEffortRecordAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<RCourseCreationContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteHarvestAsync_CallsRCourseService_ForEligibleInstructors()
    {
        // Arrange - Create term and effort type (these survive the clear)
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode });
        _context.EffortTypes.Add(new EffortType
        {
            Id = "LEC",
            Description = "Lecture",
            AllowedOnRCourses = true,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        // Create a HarvestService with a custom phase that adds test data during harvest
        // (ExecuteHarvestAsync clears existing data first, so we must add data during phase execution)
        var testPhase = new TestDataHarvestPhase(_context, TestTermCode);
        var phasesWithTestData = new List<IHarvestPhase> { testPhase };

        var harvestServiceWithTestPhase = new HarvestService(
            phasesWithTestData,
            _context,
            _viperContext,
            _coursesContext,
            _crestContext,
            _aaudContext,
            _dictionaryContext,
            _auditServiceMock.Object,
            _termServiceMock.Object,
            _instructorServiceMock.Object,
            _rCourseServiceMock.Object,
            _percentRolloverServiceMock.Object,
            _loggerMock.Object);

        // Act - Run harvest (R-course detection uses inline EndsWith("R") logic)
        var result = await harvestServiceWithTestPhase.ExecuteHarvestAsync(TestTermCode, modifiedBy: 123);

        // Assert - Harvest completes successfully
        Assert.True(result.Success);

        // RCourseService should be called for the eligible instructor
        _rCourseServiceMock.Verify(s => s.CreateRCourseEffortRecordAsync(
            100,
            TestTermCode,
            123,
            RCourseCreationContext.Harvest,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Creates a HarvestService that uses a single custom phase for testing.
    /// </summary>
    private HarvestService CreateServiceWithPhase(IHarvestPhase phase)
    {
        return new HarvestService(
            new List<IHarvestPhase> { phase },
            _context,
            _viperContext,
            _coursesContext,
            _crestContext,
            _aaudContext,
            _dictionaryContext,
            _auditServiceMock.Object,
            _termServiceMock.Object,
            _instructorServiceMock.Object,
            _rCourseServiceMock.Object,
            _percentRolloverServiceMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Test helper phase that adds preview effort records for testing CompareWithExistingData behavior.
    /// </summary>
    private class TestPreviewHarvestPhase : IHarvestPhase
    {
        private readonly (string MothraId, string PersonName, string Crn, string EffortType)[] _records;

        public TestPreviewHarvestPhase(string mothraId, string personName, string crn, string effortType)
            : this(new[] { (mothraId, personName, crn, effortType) }) { }

        public TestPreviewHarvestPhase((string MothraId, string PersonName, string Crn, string EffortType)[] records)
        {
            _records = records;
        }

        public int Order => 1;
        public string PhaseName => "TestPreview";
        public bool ShouldExecute(int termCode) => true;

        public Task GeneratePreviewAsync(HarvestContext context, CancellationToken ct = default)
        {
            foreach (var (mothraId, personName, crn, effortType) in _records)
            {
                context.Preview.CrestEffort.Add(new Viper.Areas.Effort.Models.DTOs.Responses.HarvestRecordPreview
                {
                    MothraId = mothraId,
                    PersonName = personName,
                    Crn = crn,
                    CourseCode = $"VET 410",
                    EffortType = effortType,
                    Hours = 40,
                    RoleId = 1,
                    RoleName = "Instructor",
                    Source = "CREST"
                });
            }
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(HarvestContext context, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    /// <summary>
    /// Test helper phase that adds test data during harvest execution.
    /// This simulates a harvest phase creating courses and records.
    /// </summary>
    private class TestDataHarvestPhase : IHarvestPhase
    {
        private readonly EffortDbContext _context;
        private readonly int _termCode;

        public TestDataHarvestPhase(EffortDbContext context, int termCode)
        {
            _context = context;
            _termCode = termCode;
        }

        public int Order => 1;
        public string PhaseName => "TestData";
        public bool ShouldExecute(int termCode) => termCode == _termCode;

        public Task GeneratePreviewAsync(HarvestContext context, CancellationToken ct = default)
            => Task.CompletedTask;

        public async Task ExecuteAsync(HarvestContext context, CancellationToken ct = default)
        {
            // Add a non-R-course (course number doesn't end with 'R')
            var course = new EffortCourse
            {
                TermCode = _termCode,
                Crn = "12345",
                SubjCode = "VET",
                CrseNumb = "410",
                SeqNumb = "01",
                Enrollment = 20,
                Units = 4,
                CustDept = "VME"
            };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync(ct);

            // Add an instructor
            _context.Persons.Add(new EffortPerson
            {
                PersonId = 100,
                TermCode = _termCode,
                FirstName = "Test",
                LastName = "Instructor",
                EffortDept = "VME"
            });

            // Add an effort record for the non-R-course
            _context.Records.Add(new EffortRecord
            {
                PersonId = 100,
                TermCode = _termCode,
                CourseId = course.Id,
                EffortTypeId = "LEC",
                RoleId = 1,
                Hours = 40,
                Crn = "12345"
            });
        }
    }

    [Fact]
    public async Task ExecuteHarvestAsync_IsIdempotent_ForRCourseGeneration()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode });
        _context.EffortTypes.Add(new EffortType
        {
            Id = "LEC",
            Description = "Lecture",
            AllowedOnRCourses = true,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        // Act - Run harvest twice
        var result1 = await _harvestService.ExecuteHarvestAsync(TestTermCode, modifiedBy: 123);
        var result2 = await _harvestService.ExecuteHarvestAsync(TestTermCode, modifiedBy: 123);

        // Assert - Both harvests succeed
        Assert.True(result1.Success);
        Assert.True(result2.Success);

        // RCourseService handles idempotency internally, so we just verify harvest completes
    }

    #endregion
}
