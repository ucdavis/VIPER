using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;

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
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly Mock<ILogger<ClinicalImportService>> _loggerMock;
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

        _context = new EffortDbContext(effortOptions);
        _viperContext = new VIPERContext(viperOptions);
        _auditServiceMock = new Mock<IEffortAuditService>();
        _loggerMock = new Mock<ILogger<ClinicalImportService>>();

        _service = new ClinicalImportService(
            _context,
            _viperContext,
            _auditServiceMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _viperContext.Dispose();
    }

    #region GetPreviewAsync Validation Tests

    [Fact]
    public async Task GetPreviewAsync_ReturnsWarning_WhenTermNotFound()
    {
        // Act
        var result = await _service.GetPreviewAsync(999999, ClinicalImportMode.Sync);

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
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.Sync);

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
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.Sync);

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
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.Sync);

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
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.Sync);

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
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.Sync);

        // Assert
        var deleteAssignment = Assert.Single(result.Assignments, a => a.Status == "Delete");
        Assert.Equal(5, deleteAssignment.Weeks);
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
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.AddNewOnly);

        // Assert — existing record should be "Skip" with no ExistingWeeks
        var skipAssignment = Assert.Single(result.Assignments, a => a.Status == "Skip");
        Assert.Null(skipAssignment.ExistingWeeks);
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
            Id = 2, TermCode = TermCode, SubjCode = "DVM", CrseNumb = "491",
            SeqNumb = "001", Crn = "CRN002", Enrollment = 20, Units = 1, CustDept = "VME"
        });
        await _context.SaveChangesAsync();

        // Record 1: will be updated (weeks differ)
        await SeedExistingClinicalRecordAsync("INST0010", 10, courseId: 1, weeks: 4);
        await SeedSourceDataAsync("INST0010", "DVM", "453", weekCount: 6);

        // Record 2: will be deleted (no source)
        await SeedExistingClinicalRecordAsync("INST0011", 11, courseId: 2, weeks: 3);

        // Record 3: new (no existing record)
        await SeedSourceDataAsync("INST0012", "DVM", "491", weekCount: 2);

        // Act
        var result = await _service.GetPreviewAsync(TermCode, ClinicalImportMode.Sync);

        // Assert
        Assert.Equal(1, result.AddCount);
        Assert.Equal(1, result.UpdateCount);
        Assert.Equal(1, result.DeleteCount);
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
        string mothraId, string subjCode, string crseNumb, int weekCount)
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
                    DateStart = new DateTime(2024, 9, 1).AddDays(i * 7),
                    DateEnd = new DateTime(2024, 9, 7).AddDays(i * 7)
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
                DateStart = new DateTime(2024, 9, 1),
                DateEnd = new DateTime(2024, 9, 7)
            });
        }

        await _viperContext.SaveChangesAsync();
    }

    #endregion
}
