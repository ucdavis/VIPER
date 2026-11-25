using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.Courses;
using Viper.Models.CTS;
using Person = Viper.Models.VIPER.Person;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for ClinicalImportService preview and import operations.
/// </summary>
public sealed class ClinicalImportServiceTests : IDisposable
{
    private const int TermCode = 202409; // Semester term (ends in 09)
    private const string TermCodeStr = "202409";

    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly AAUDContext _aaudContext;
    private readonly CoursesContext _coursesContext;
    private readonly IInstructorService _instructorServiceMock;
    private readonly IEffortAuditService _auditServiceMock;
    private readonly ILogger<ClinicalImportService> _loggerMock;
    private readonly ClinicalImportService _service;

    public ClinicalImportServiceTests()
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

        var coursesOptions = new DbContextOptionsBuilder<CoursesContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EffortDbContext(effortOptions);
        _viperContext = new VIPERContext(viperOptions);
        _aaudContext = new AAUDContext(aaudOptions);
        _coursesContext = new CoursesContext(coursesOptions);
        _instructorServiceMock = Substitute.For<IInstructorService>();
        _auditServiceMock = Substitute.For<IEffortAuditService>();
        _loggerMock = Substitute.For<ILogger<ClinicalImportService>>();

        // Default instructor service behavior
        _instructorServiceMock
            .GetTitleCodesAsync(Arg.Any<CancellationToken>()).Returns(new List<TitleCodeDto>
            {
                new() { Code = "1234", Name = "Test Title" }
            });

        _instructorServiceMock
            .BatchResolveDepartmentsAsync(Arg.Any<List<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<List<string>>().ToDictionary(id => id, _ => "VME"));

        _instructorServiceMock
            .GetExcludedTitleCodesAsync(Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        _service = new ClinicalImportService(
            _context,
            _viperContext,
            _aaudContext,
            _coursesContext,
            _instructorServiceMock,
            _auditServiceMock,
            _loggerMock);
    }

    public void Dispose()
    {
        _context.Dispose();
        _viperContext.Dispose();
        _aaudContext.Dispose();
        _coursesContext.Dispose();
    }

    #region GetPriorityCourse Tests

    [Theory]
    [InlineData("DVM 463", "DVM 478", "DVM 463")] // neither in the priority table: lowest code
    [InlineData("DVM 478", "DVM 463", "DVM 463")] // same answer whatever the list order
    [InlineData("DVM 492", "DVM 491", "DVM 491")] // table priority still wins
    [InlineData("DVM 400", "DVM 447", "DVM 447")] // a listed course beats an unlisted one
    public void GetPriorityCourse_IsIndependentOfListOrder(string first, string second, string expected)
    {
        Assert.Equal(expected, ClinicalImportService.GetPriorityCourse([first, second]));
    }

    #endregion

    #region GetPreviewAsync Validation Tests

    [Fact]
    public async Task GetPreviewAsync_ReturnsWarning_WhenTermNotFound()
    {
        // Act
        var result = await _service.GetPreviewAsync(999999, ClinicalImportMode.Sync, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(result.Warnings, w => w.Contains("not found"));
        Assert.Empty(result.Assignments);
    }

    [Fact]
    public async Task GetPreviewAsync_ReturnsWarning_WhenTermNotEligible()
    {
        // Arrange - Closed term is not eligible
        _context.Terms.Add(new EffortTerm
        {
            TermCode = TermCode,
            HarvestedDate = DateTime.Now.AddDays(-2),
            OpenedDate = DateTime.Now.AddDays(-1),
            ClosedDate = DateTime.Now
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.Sync, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(result.Warnings, w => w.Contains("not available"));
    }

    #endregion

    #region GetPreviewAsync Sync Mode - ExistingWeeks Tests

    [Fact]
    public async Task GetPreviewAsync_SyncMode_SetsExistingWeeks_WhenWeeksChanged()
    {
        // Arrange
        await SeedTermAndCourseAsync();
        await SeedExistingClinicalRecordAsync("INST0001", 1, courseId: 1, weeks: 5);
        await SeedSourceDataAsync("INST0001", "DVM", "453", weekCount: 8);

        // Act
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.Sync, TestContext.Current.CancellationToken);

        // Assert
        var updateAssignment = Assert.Single(result.Assignments, a => a.Status == "Update");
        Assert.Equal(8, updateAssignment.Weeks);
        Assert.Equal(5, updateAssignment.ExistingWeeks);
    }

    [Fact]
    public async Task GetPreviewAsync_SyncMode_SetsExistingWeeksNull_WhenWeeksUnchanged()
    {
        // Arrange
        await SeedTermAndCourseAsync();
        await SeedExistingClinicalRecordAsync("INST0002", 2, courseId: 1, weeks: 5);
        await SeedSourceDataAsync("INST0002", "DVM", "453", weekCount: 5);

        // Act
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.Sync, TestContext.Current.CancellationToken);

        // Assert
        var skipAssignment = Assert.Single(result.Assignments, a => a.Status == "Skip");
        Assert.Null(skipAssignment.ExistingWeeks);
    }

    [Fact]
    public async Task GetPreviewAsync_SyncMode_SetsExistingWeeksNull_ForNewRecords()
    {
        // Arrange
        await SeedTermAndCourseAsync();
        // No existing record — only source data
        await SeedSourceDataAsync("INST0003", "DVM", "453", weekCount: 3);

        // Act
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.Sync, TestContext.Current.CancellationToken);

        // Assert
        var newAssignment = Assert.Single(result.Assignments, a => a.Status == "New");
        Assert.Null(newAssignment.ExistingWeeks);
        Assert.Equal(3, newAssignment.Weeks);
    }

    [Fact]
    public async Task GetPreviewAsync_SyncMode_MarksOrphanedRecords_AsDelete()
    {
        // Arrange — existing record with no matching source data
        await SeedTermAndCourseAsync();
        await SeedExistingClinicalRecordAsync("INST0004", 4, courseId: 1, weeks: 5);
        // No source data seeded

        // Act
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.Sync, TestContext.Current.CancellationToken);

        // Assert
        var deleteAssignment = Assert.Single(result.Assignments, a => a.Status == "Delete");
        Assert.Equal(5, deleteAssignment.Weeks);
    }

    [Fact]
    public async Task GetPreviewAsync_SyncMode_HandlesNullViperPerson_WithEmptyMothraId()
    {
        // Arrange — existing clinical record where ViperPerson has empty MothraId.
        // GetExistingClinicalRecordsAsync resolves MothraId via:
        //   r.ViperPerson != null ? r.ViperPerson.MothraId : ""
        // When MothraId is empty (either from null ViperPerson or empty string), the
        // existing record can't match any source data, causing it to appear as "Delete"
        // while the source data appears as "New".
        //
        // Note: We seed a ViperPerson with empty MothraId rather than omitting it
        // entirely because EF Core InMemory doesn't resolve Include+Select projections
        // for records with truly null optional navigations. In production (SQL Server),
        // the null ViperPerson case also produces MothraId="" via the ternary fallback.
        await SeedTermAndCourseAsync();

        // Seed ViperPerson with empty MothraId — simulates the null ViperPerson fallback
        _context.ViperPersons.Add(new ViperPerson
        {
            PersonId = 50,
            FirstName = "Null",
            LastName = "Viper",
            MothraId = ""
        });

        // Seed EffortPerson directly
        _context.Persons.Add(new EffortPerson
        {
            PersonId = 50,
            TermCode = TermCode,
            FirstName = "Null",
            LastName = "Viper",
            EffortDept = "VME",
            EffortTitleCode = "1234"
        });

        // Seed EffortRecord linked to PersonId=50 and courseId=1
        _context.Records.Add(new EffortRecord
        {
            CourseId = 1,
            PersonId = 50,
            TermCode = TermCode,
            EffortTypeId = ClinicalEffortTypes.Clinical,
            RoleId = 1,
            Weeks = 4,
            Crn = "CRN001"
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Seed source data that would match by MothraId if ViperPerson had one
        await SeedSourceDataAsync("INST0050", "DVM", "453", weekCount: 4);

        // Act
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.Sync, TestContext.Current.CancellationToken);

        // Assert — existing record's MothraId resolved to "" so it doesn't match source
        // data keyed by "INST0050". The source appears as New and the existing as Delete.
        var newAssignment = Assert.Single(result.Assignments, a => a.Status == "New");
        Assert.Equal("INST0050", newAssignment.MothraId);
        Assert.Equal(4, newAssignment.Weeks);

        var deleteAssignment = Assert.Single(result.Assignments, a => a.Status == "Delete");
        Assert.Equal(4, deleteAssignment.Weeks);
    }

    #endregion

    #region GetPreviewAsync AddNewOnly Mode Tests

    [Fact]
    public async Task GetPreviewAsync_AddNewOnlyMode_DoesNotSetExistingWeeks()
    {
        // Arrange
        await SeedTermAndCourseAsync();
        await SeedExistingClinicalRecordAsync("INST0005", 5, courseId: 1, weeks: 3);
        await SeedSourceDataAsync("INST0005", "DVM", "453", weekCount: 8);

        // Act
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.AddNewOnly, TestContext.Current.CancellationToken);

        // Assert — existing record should be "Skip" with no ExistingWeeks
        var skipAssignment = Assert.Single(result.Assignments, a => a.Status == "Skip");
        Assert.Null(skipAssignment.ExistingWeeks);
    }

    #endregion

    #region AddNewOnly Displaced Record Tests

    // An earlier import credited DVM 478 for weeks that DVM 463 now wins (the tie-break changed).
    // soloWeeks are DVM 478 weeks with no competing course, so they stay with DVM 478.
    [Theory]
    [InlineData(0, "Delete", null, false)] // every DVM 478 week went to DVM 463: the record is removed
    [InlineData(1, "Update", 1, false)]    // one week is still DVM 478's alone: the record drops to it
    [InlineData(0, "Delete", null, true)]
    [InlineData(1, "Update", 1, true)]
    public async Task AddNewOnly_ReconcilesRecord_WhenAddedCourseTookItsWeeks(
        int soloWeeks, string expectedStatus, int? expectedWeeks, bool withProgress)
    {
        var ct = TestContext.Current.CancellationToken;
        const int sharedWeeks = 2;
        var staleRecordId = await SeedDisplacedScenarioAsync(sharedWeeks, soloWeeks);

        var preview = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.AddNewOnly, ct);

        var added = Assert.Single(preview.Assignments, a => a.Status == "New");
        Assert.Equal("DVM 463", added.CourseNumber);
        Assert.Equal(sharedWeeks, added.Weeks);
        var stale = Assert.Single(preview.Assignments, a => a.ExistingRecordId == staleRecordId);
        Assert.Equal(expectedStatus, stale.Status);

        var result = await ExecuteAddNewOnlyAsync(withProgress, ct);

        Assert.True(result.Success);
        Assert.Equal(1, result.RecordsAdded);
        Assert.Equal(expectedWeeks == null ? 1 : 0, result.RecordsDeleted);
        Assert.Equal(expectedWeeks == null ? 0 : 1, result.RecordsUpdated);
        Assert.Equal(0, result.RecordsSkipped);

        var records = await _context.Records.AsNoTracking()
            .Where(r => r.TermCode == TermCode)
            .ToDictionaryAsync(r => r.CourseId, r => r.Weeks, ct);
        Assert.Equal(sharedWeeks, records[Dvm463CourseId]);
        Assert.Equal(expectedWeeks, records.GetValueOrDefault(Dvm478CourseId));
    }

    [Fact]
    public async Task AddNewOnly_LeavesExistingRecordAlone_WhenNoCourseIsAdded()
    {
        var ct = TestContext.Current.CancellationToken;
        var staleRecordId = await SeedDisplacedScenarioAsync(sharedWeeks: 2, soloWeeks: 1);
        // DVM 463 already has its record, so nothing is added and nothing may be reconciled
        await SeedExistingClinicalRecordAsync(DisplacedMothraId, DisplacedPersonId, Dvm463CourseId, weeks: 2);

        var result = await _service.ExecuteImportAsync(TermCode, ClinicalImportMode.AddNewOnly, modifiedBy: 1, ct);

        Assert.Equal(0, result.RecordsAdded);
        Assert.Equal(0, result.RecordsUpdated);
        Assert.Equal(0, result.RecordsDeleted);
        var stale = await _context.Records.AsNoTracking().SingleAsync(r => r.Id == staleRecordId, ct);
        Assert.Equal(3, stale.Weeks);
    }

    // Execution imports for anyone already in the term even with no valid AAUD data, so the
    // preview must show the same additions and removals for them.
    [Fact]
    public async Task AddNewOnly_PreviewMatchesExecution_ForExistingPersonWithoutAaudData()
    {
        var ct = TestContext.Current.CancellationToken;
        var staleRecordId = await SeedDisplacedScenarioAsync(sharedWeeks: 2, soloWeeks: 0, seedAaud: false);

        var preview = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.AddNewOnly, ct);
        var result = await _service.ExecuteImportAsync(TermCode, ClinicalImportMode.AddNewOnly, modifiedBy: 1, ct);

        Assert.Equal("DVM 463", Assert.Single(preview.Assignments, a => a.Status == "New").CourseNumber);
        Assert.Equal(staleRecordId, Assert.Single(preview.Assignments, a => a.Status == "Delete").ExistingRecordId);
        Assert.Equal(preview.AddCount, result.RecordsAdded);
        Assert.Equal(preview.DeleteCount, result.RecordsDeleted);
    }

    // A fresh import credits each shared week to the priority course only, and the losing
    // course having no record yet must not stop the reconcile step.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddNewOnly_FreshImport_CreditsSharedWeeksToPriorityCourseOnly(bool withProgress)
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedDisplacedScenarioAsync(sharedWeeks: 2, soloWeeks: 1);
        _context.Records.RemoveRange(_context.Records.Where(r => r.CourseId == Dvm478CourseId));
        await _context.SaveChangesAsync(ct);

        var result = await ExecuteAddNewOnlyAsync(withProgress, ct);

        Assert.Equal((2, 0, 0), (result.RecordsAdded, result.RecordsUpdated, result.RecordsDeleted));
        var records = await _context.Records.AsNoTracking()
            .Where(r => r.TermCode == TermCode)
            .ToDictionaryAsync(r => r.CourseId, r => r.Weeks, ct);
        Assert.Equal(2, records[Dvm463CourseId]);
        Assert.Equal(1, records[Dvm478CourseId]);
    }

    private Task<ClinicalImportResultDto> ExecuteAddNewOnlyAsync(bool withProgress, CancellationToken ct) =>
        withProgress
            ? _service.ExecuteImportWithProgressAsync(TermCode, ClinicalImportMode.AddNewOnly, modifiedBy: 1,
                Channel.CreateUnbounded<ClinicalImportProgressEvent>().Writer, ct)
            : _service.ExecuteImportAsync(TermCode, ClinicalImportMode.AddNewOnly, modifiedBy: 1, ct);

    private const string DisplacedMothraId = "INST0030";
    private const int DisplacedPersonId = 30;
    private const int Dvm463CourseId = 2;
    private const int Dvm478CourseId = 3;

    /// <summary>
    /// One instructor on DVM 463 and DVM 478 in the same weeks (plus DVM 478 alone for
    /// soloWeeks), holding a DVM 478 record that counts every one of those weeks.
    /// </summary>
    /// <returns>The ID of the existing DVM 478 record.</returns>
    private async Task<int> SeedDisplacedScenarioAsync(int sharedWeeks, int soloWeeks, bool seedAaud = true)
    {
        await SeedTermAndCourseAsync();
        foreach (var (id, crseNumb) in new[] { (Dvm463CourseId, "463"), (Dvm478CourseId, "478") })
        {
            _context.Courses.Add(new EffortCourse
            {
                Id = id,
                TermCode = TermCode,
                SubjCode = "DVM",
                CrseNumb = crseNumb,
                SeqNumb = "001",
                Crn = $"CRN{crseNumb}",
                Enrollment = 20,
                Units = 1,
                CustDept = "DVM"
            });
        }
        await _context.SaveChangesAsync();

        await SeedSourceDataAsync(DisplacedMothraId, "DVM", "478", weekCount: sharedWeeks + soloWeeks, seedAaud);

        // Put DVM 463 on the first sharedWeeks of those same weeks
        var rows = await _viperContext.InstructorSchedules.AsNoTracking()
            .Where(s => s.MothraId == DisplacedMothraId)
            .OrderBy(s => s.WeekId)
            .Take(sharedWeeks)
            .ToListAsync();
        foreach (var row in rows)
        {
            row.InstructorScheduleId = 0;
            row.CrseNumb = "463";
            _viperContext.InstructorSchedules.Add(row);
        }
        await _viperContext.SaveChangesAsync();

        await SeedExistingClinicalRecordAsync(DisplacedMothraId, DisplacedPersonId, Dvm478CourseId, weeks: sharedWeeks + soloWeeks);
        return await _context.Records.Where(r => r.CourseId == Dvm478CourseId).Select(r => r.Id).SingleAsync();
    }

    #endregion

    #region GetPreviewAsync Count Tests

    [Fact]
    public async Task GetPreviewAsync_SyncMode_CalculatesCountsCorrectly()
    {
        // Arrange
        await SeedTermAndCourseAsync();

        // Add a second course
        _context.Courses.Add(new EffortCourse
        {
            Id = 2,
            TermCode = TermCode,
            SubjCode = "DVM",
            CrseNumb = "491",
            SeqNumb = "001",
            Crn = "CRN002",
            Enrollment = 20,
            Units = 1,
            CustDept = "VME"
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Record 1: will be updated (weeks differ)
        await SeedExistingClinicalRecordAsync("INST0010", 10, courseId: 1, weeks: 4);
        await SeedSourceDataAsync("INST0010", "DVM", "453", weekCount: 6);

        // Record 2: will be deleted (no source)
        await SeedExistingClinicalRecordAsync("INST0011", 11, courseId: 2, weeks: 3);

        // Record 3: new (no existing record)
        await SeedSourceDataAsync("INST0012", "DVM", "491", weekCount: 2);

        // Act
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.Sync, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.AddCount);
        Assert.Equal(1, result.UpdateCount);
        Assert.Equal(1, result.DeleteCount);
    }

    #endregion

    #region ExecuteImportAsync - EnsureEffortPerson/Course Tests

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, EffortConstants.CahfsReportUnit)]
    public async Task ExecuteImportAsync_CreatesEffortPerson_WhenMissing(bool hasCahfsJob, string? expectedReportUnit)
    {
        // Arrange - term, source data, AAUD record, VIPER person — but NO EffortPerson
        await SeedTermAndCourseAsync();
        await SeedSourceDataAsync("INST0020", "DVM", "453", weekCount: 3);

        // ViperPerson for CreateClinicalRecordAsync lookup
        _context.ViperPersons.Add(new ViperPerson
        {
            PersonId = 20,
            FirstName = "New",
            LastName = "Clinical",
            MothraId = "INST0020"
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // VIPER People for EnsureEffortPersonAsync name lookup
        _viperContext.People.Add(new Person
        {
            PersonId = 20,
            ClientId = "C020",
            FirstName = "New",
            LastName = "Clinical",
            FullName = "Clinical, New",
            MothraId = "INST0020"
        });
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // AAUD data for title code
        _aaudContext.Ids.Add(new Id
        {
            IdsPKey = "PK020",
            IdsTermCode = TermCodeStr,
            IdsClientid = "C020",
            IdsMothraid = "INST0020"
        });
        _aaudContext.Employees.Add(new Employee
        {
            EmpPKey = "PK020",
            EmpTermCode = TermCodeStr,
            EmpEffortTitleCode = "1234",
            EmpClientid = "C020",
            EmpHomeDept = "069001",
            EmpAltDeptCode = "069001",
            EmpSchoolDivision = "VM",
            EmpCbuc = "VETMED",
            EmpStatus = "A"
        });
        await _aaudContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _instructorServiceMock
            .GetCahfsMothraIdsAsync(Arg.Any<IReadOnlyCollection<string>>(), TermCode, Arg.Any<CancellationToken>())
            .Returns(hasCahfsJob ? new HashSet<string> { "INST0020" } : new HashSet<string>());

        // Act
        var result = await _service.ExecuteImportAsync(TermCode, ClinicalImportMode.AddNewOnly, modifiedBy: 1, TestContext.Current.CancellationToken);

        // Assert — EffortPerson should have been created
        Assert.True(result.Success);
        Assert.True(result.RecordsAdded > 0);
        var effortPerson = await _context.Persons.FirstOrDefaultAsync(p => p.PersonId == 20 && p.TermCode == TermCode, TestContext.Current.CancellationToken);
        Assert.NotNull(effortPerson);
        Assert.Equal("NEW", effortPerson.FirstName);
        Assert.Equal("CLINICAL", effortPerson.LastName);
        Assert.Equal("VME", effortPerson.EffortDept);
        Assert.Equal("1234", effortPerson.EffortTitleCode);
        Assert.Equal(expectedReportUnit, effortPerson.ReportUnit);
        Assert.Equal((byte)0, effortPerson.VolunteerWos);
        await _instructorServiceMock.Received(1).GetCahfsMothraIdsAsync(
            Arg.Any<IReadOnlyCollection<string>>(), TermCode, Arg.Any<CancellationToken>());
        await _instructorServiceMock.DidNotReceiveWithAnyArgs().HasCahfsAppointmentAsync(default!, default, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(null, true, false, false, EffortConstants.CahfsReportUnit)]
    [InlineData("", true, false, false, EffortConstants.CahfsReportUnit)]
    [InlineData(null, false, false, false, null)]
    [InlineData("PMI", true, false, false, "PMI")]
    [InlineData(null, true, true, false, EffortConstants.CahfsReportUnit)]
    [InlineData(null, true, true, true, EffortConstants.CahfsReportUnit)]
    public async Task ExecuteImportAsync_TagsExistingPersonCahfs_OnlyWhenReportUnitUnset(
        string? existingReportUnit, bool hasCahfsJob, bool hasExistingRecord, bool withProgress, string? expectedReportUnit)
    {
        // Arrange - instructor already exists for the term (e.g. added manually)
        await SeedTermAndCourseAsync();
        await SeedSourceDataAsync("INST0022", "DVM", "453", weekCount: 2);
        _context.ViperPersons.Add(new ViperPerson
        {
            PersonId = 22,
            FirstName = "Existing",
            LastName = "Clinical",
            MothraId = "INST0022"
        });
        _context.Persons.Add(new EffortPerson
        {
            PersonId = 22,
            TermCode = TermCode,
            FirstName = "EXISTING",
            LastName = "CLINICAL",
            EffortDept = "VME",
            EffortTitleCode = "1234",
            ReportUnit = existingReportUnit
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        if (hasExistingRecord)
        {
            // First import creates the clinical record, so the repeat import skips it
            await _service.ExecuteImportAsync(TermCode, ClinicalImportMode.AddNewOnly, modifiedBy: 1, TestContext.Current.CancellationToken);
            _auditServiceMock.ClearReceivedCalls();
        }

        _instructorServiceMock
            .GetCahfsMothraIdsAsync(Arg.Any<IReadOnlyCollection<string>>(), TermCode, Arg.Any<CancellationToken>())
            .Returns(hasCahfsJob ? new HashSet<string> { "INST0022" } : new HashSet<string>());

        // Act
        var result = withProgress
            ? await _service.ExecuteImportWithProgressAsync(TermCode, ClinicalImportMode.AddNewOnly, modifiedBy: 1,
                Channel.CreateUnbounded<ClinicalImportProgressEvent>().Writer, TestContext.Current.CancellationToken)
            : await _service.ExecuteImportAsync(TermCode, ClinicalImportMode.AddNewOnly, modifiedBy: 1, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.Success);
        var effortPerson = await _context.Persons.AsNoTracking().SingleAsync(p => p.PersonId == 22 && p.TermCode == TermCode, TestContext.Current.CancellationToken);
        Assert.Equal(expectedReportUnit, effortPerson.ReportUnit);
        _auditServiceMock.Received(expectedReportUnit != existingReportUnit ? 1 : 0).AddPersonChangeAudit(
            22, TermCode, EffortAuditActions.UpdatePerson, Arg.Any<object>(), Arg.Any<object>());
    }

    [Fact]
    public async Task ExecuteImportAsync_CreatesEffortCourse_WhenMissing()
    {
        // Arrange - term, source data, person — but NO EffortCourse for the source course
        _context.Terms.Add(new EffortTerm { TermCode = TermCode });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // No course seeded — EnsureEffortCourseAsync should create it

        await SeedSourceDataAsync("INST0021", "DVM", "491", weekCount: 2);

        _context.ViperPersons.Add(new ViperPerson
        {
            PersonId = 21,
            FirstName = "Course",
            LastName = "Test",
            MothraId = "INST0021"
        });
        _context.Persons.Add(new EffortPerson
        {
            PersonId = 21,
            TermCode = TermCode,
            FirstName = "COURSE",
            LastName = "TEST",
            EffortDept = "VME",
            EffortTitleCode = "1234"
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Banner CRN lookup
        _coursesContext.Baseinfos.Add(new Baseinfo
        {
            BaseinfoPkey = "BK001",
            BaseinfoTermCode = TermCodeStr,
            BaseinfoSubjCode = "DVM",
            BaseinfoCrseNumb = "491",
            BaseinfoSeqNumb = "001",
            BaseinfoCrn = "CRN491",
            BaseinfoTitle = "SA Emergency",
            BaseinfoEnrollment = 20,
            BaseinfoUnitType = "CR",
            BaseinfoUnitLow = 1,
            BaseinfoUnitHigh = 1,
            BaseinfoCollCode = "VM",
            BaseinfoDeptCode = "VME",
            BaseinfoXlistFlag = "N",
            BaseinfoXlistGroup = ""
        });
        await _coursesContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _service.ExecuteImportAsync(TermCode, ClinicalImportMode.AddNewOnly, modifiedBy: 1, TestContext.Current.CancellationToken);

        // Assert — EffortCourse should have been created with Banner CRN
        Assert.True(result.Success);
        Assert.True(result.RecordsAdded > 0);
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.TermCode == TermCode && c.SubjCode == "DVM" && c.CrseNumb == "491", TestContext.Current.CancellationToken);
        Assert.NotNull(course);
        Assert.Equal("CRN491", course.Crn);
        Assert.Equal(EffortConstants.DefaultClinicalEnrollment, course.Enrollment);
    }

    [Fact]
    public async Task ExecuteImportAsync_SkipsRecord_WhenNoAaudTitleCode()
    {
        // Arrange - source data, VIPER person, but NO AAUD record → should skip
        await SeedTermAndCourseAsync();
        await SeedSourceDataAsync("INST0022", "DVM", "453", weekCount: 2, seedAaud: false);

        _context.ViperPersons.Add(new ViperPerson
        {
            PersonId = 22,
            FirstName = "No",
            LastName = "Aaud",
            MothraId = "INST0022"
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // No AAUD data seeded — EnsureEffortPersonAsync should return false

        // Act
        var result = await _service.ExecuteImportAsync(TermCode, ClinicalImportMode.AddNewOnly, modifiedBy: 1, TestContext.Current.CancellationToken);

        // Assert — record should be skipped, not added
        Assert.True(result.Success);
        Assert.Equal(0, result.RecordsAdded);
        Assert.True(result.RecordsSkipped > 0);
    }

    #endregion

    #region Helper Methods

    private async Task SeedTermAndCourseAsync()
    {
        if (!await _context.Terms.AnyAsync(t => t.TermCode == TermCode))
        {
            _context.Terms.Add(new EffortTerm { TermCode = TermCode });
            await _context.SaveChangesAsync();
        }

        if (!await _context.Courses.AnyAsync(c => c.Id == 1))
        {
            _context.Courses.Add(new EffortCourse
            {
                Id = 1,
                TermCode = TermCode,
                SubjCode = "DVM",
                CrseNumb = "453",
                SeqNumb = "001",
                Crn = "CRN001",
                Enrollment = 20,
                Units = 1,
                CustDept = "VME"
            });
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedExistingClinicalRecordAsync(
        string mothraId, int personId, int courseId, int weeks)
    {
        // ViperPerson provides MothraId lookup for existing record matching
        if (!await _context.ViperPersons.AnyAsync(p => p.PersonId == personId))
        {
            _context.ViperPersons.Add(new ViperPerson
            {
                PersonId = personId,
                FirstName = "Test",
                LastName = "Instructor",
                MothraId = mothraId
            });
        }

        // EffortPerson provides the person reference for the record
        if (!await _context.Persons.AnyAsync(p => p.PersonId == personId && p.TermCode == TermCode))
        {
            _context.Persons.Add(new EffortPerson
            {
                PersonId = personId,
                TermCode = TermCode,
                FirstName = "Test",
                LastName = "Instructor",
                EffortDept = "VME",
                EffortTitleCode = "1234"
            });
        }

        _context.Records.Add(new EffortRecord
        {
            CourseId = courseId,
            PersonId = personId,
            TermCode = TermCode,
            EffortTypeId = ClinicalEffortTypes.Clinical,
            RoleId = 1,
            Weeks = weeks,
            Crn = "CRN001"
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedSourceDataAsync(
        string mothraId, string subjCode, string crseNumb, int weekCount, bool seedAaud = true)
    {
        // Ensure weeks exist
        var existingWeekIds = await _viperContext.Weeks
            .Where(w => w.TermCode == TermCode)
            .Select(w => w.WeekId)
            .ToListAsync();

        var nextWeekId = existingWeekIds.Count > 0 ? existingWeekIds.Max() + 1 : 1;
        var weekIds = new List<int>();

        for (var i = 0; i < weekCount; i++)
        {
            var weekId = nextWeekId + i;
            if (!existingWeekIds.Contains(weekId))
            {
                _viperContext.Weeks.Add(new Week
                {
                    WeekId = weekId,
                    TermCode = TermCode,
                    DateStart = new DateTime(2024, 9, 1, 0, 0, 0, DateTimeKind.Local).AddDays(i * 7.0),
                    DateEnd = new DateTime(2024, 9, 7, 0, 0, 0, DateTimeKind.Local).AddDays(i * 7.0)
                });
            }
            weekIds.Add(weekId);
        }

        // Ensure service and rotation exist for FK constraints
        if (!await _viperContext.Set<Service>().AnyAsync(s => s.ServiceId == 1))
        {
            _viperContext.Set<Service>().Add(new Service
            {
                ServiceId = 1,
                ServiceName = "Test Service",
                ShortName = "TS"
            });
        }

        if (!await _viperContext.Set<Rotation>().AnyAsync(r => r.RotId == 1))
        {
            _viperContext.Set<Rotation>().Add(new Rotation
            {
                RotId = 1,
                ServiceId = 1,
                Name = "Test Rotation",
                Abbreviation = "TR",
                SubjectCode = subjCode,
                CourseNumber = crseNumb
            });
        }

        // Create one InstructorSchedule per week
        foreach (var weekId in weekIds)
        {
            _viperContext.InstructorSchedules.Add(new InstructorSchedule
            {
                MothraId = mothraId,
                WeekId = weekId,
                SubjCode = subjCode,
                CrseNumb = crseNumb,
                FirstName = "Test",
                LastName = "Instructor",
                FullName = "Instructor, Test",
                RotationId = 1,
                RotationName = "Test Rotation",
                Abbreviation = "TR",
                ServiceId = 1,
                ServiceName = "Test Service",
                DateStart = new DateTime(2024, 9, 1, 0, 0, 0, DateTimeKind.Local),
                DateEnd = new DateTime(2024, 9, 7, 0, 0, 0, DateTimeKind.Local)
            });
        }

        await _viperContext.SaveChangesAsync();

        // Seed AAUD data so ValidateImportableInstructorsAsync finds this instructor importable
        if (seedAaud)
        {
            var pkey = $"PK-{mothraId}";
            if (!await _aaudContext.Ids.AnyAsync(i => i.IdsPKey == pkey))
            {
                _aaudContext.Ids.Add(new Id
                {
                    IdsPKey = pkey,
                    IdsTermCode = TermCodeStr,
                    IdsClientid = "CLI",
                    IdsMothraid = mothraId
                });
                _aaudContext.Employees.Add(new Employee
                {
                    EmpPKey = pkey,
                    EmpTermCode = TermCodeStr,
                    EmpClientid = "CLI",
                    EmpHomeDept = "VME",
                    EmpAltDeptCode = "",
                    EmpSchoolDivision = "SVM",
                    EmpCbuc = "",
                    EmpStatus = "A",
                    EmpEffortTitleCode = "1234"
                });
                await _aaudContext.SaveChangesAsync();
            }
        }
    }

    #endregion
}
