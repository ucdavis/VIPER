using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for CourseService course management operations.
/// Note: Banner search tests are skipped as they require a real database with the
/// sp_search_banner_courses stored procedure. Use integration tests for Banner functionality.
/// </summary>
public sealed class CourseServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly AAUDContext _aaudContext;
    private readonly CoursesContext _coursesContext;
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly Mock<ILogger<CourseService>> _loggerMock;
    private readonly CourseService _courseService;

    public CourseServiceTests()
    {
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var viperOptions = new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var aaudOptions = new DbContextOptionsBuilder<AAUDContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var coursesOptions = new DbContextOptionsBuilder<CoursesContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EffortDbContext(effortOptions);
        _viperContext = new VIPERContext(viperOptions);
        _aaudContext = new AAUDContext(aaudOptions);
        _coursesContext = new CoursesContext(coursesOptions);
        _auditServiceMock = new Mock<IEffortAuditService>();
        _loggerMock = new Mock<ILogger<CourseService>>();

        // Setup synchronous audit methods used within transactions
        _auditServiceMock
            .Setup(s => s.AddCourseChangeAudit(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>()));

        _courseService = new CourseService(_context, _viperContext, _aaudContext, _coursesContext, _auditServiceMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _viperContext.Dispose();
        _aaudContext.Dispose();
        _coursesContext.Dispose();
    }

    #region GetCoursesAsync Tests

    [Fact]
    public async Task GetCoursesAsync_ReturnsAllCoursesForTerm_OrderedBySubjCodeCrseNumbSeqNumb()
    {
        // Arrange
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 4, CustDept = "VME" },
            new EffortCourse { Id = 2, TermCode = 202410, Crn = "12346", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 3, TermCode = 202410, Crn = "12347", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "002", Enrollment = 15, Units = 4, CustDept = "DVM" }
        );
        await _context.SaveChangesAsync();

        // Act
        var courses = await _courseService.GetCoursesAsync(202410);

        // Assert
        Assert.Equal(3, courses.Count);
        Assert.Equal("DVM", courses[0].SubjCode);
        Assert.Equal("001", courses[0].SeqNumb);
        Assert.Equal("DVM", courses[1].SubjCode);
        Assert.Equal("002", courses[1].SeqNumb);
        Assert.Equal("VME", courses[2].SubjCode);
    }

    [Fact]
    public async Task GetCoursesAsync_FiltersByDepartment_WhenDepartmentProvided()
    {
        // Arrange
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 4, CustDept = "VME" },
            new EffortCourse { Id = 2, TermCode = 202410, Crn = "12346", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" }
        );
        await _context.SaveChangesAsync();

        // Act
        var courses = await _courseService.GetCoursesAsync(202410, "DVM");

        // Assert
        Assert.Single(courses);
        Assert.Equal("DVM", courses[0].CustDept);
    }

    [Fact]
    public async Task GetCoursesAsync_ReturnsEmptyList_WhenNoCoursesExistForTerm()
    {
        // Act
        var courses = await _courseService.GetCoursesAsync(999999);

        // Assert
        Assert.Empty(courses);
    }

    #endregion

    #region GetCourseAsync Tests

    [Fact]
    public async Task GetCourseAsync_ReturnsCourse_WhenCourseExists()
    {
        // Arrange
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        // Act
        var course = await _courseService.GetCourseAsync(1);

        // Assert
        Assert.NotNull(course);
        Assert.Equal(1, course.Id);
        Assert.Equal("DVM", course.SubjCode);
        Assert.Equal("443", course.CrseNumb);
    }

    [Fact]
    public async Task GetCourseAsync_ReturnsNull_WhenCourseDoesNotExist()
    {
        // Act
        var course = await _courseService.GetCourseAsync(999);

        // Assert
        Assert.Null(course);
    }

    #endregion

    #region CreateCourseAsync Tests

    [Fact]
    public async Task CreateCourseAsync_CreatesCourse_WithValidData()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        await _context.SaveChangesAsync();

        var request = new CreateCourseRequest
        {
            TermCode = 202410,
            Crn = "99999",
            SubjCode = "TST",
            CrseNumb = "101",
            SeqNumb = "001",
            Enrollment = 25,
            Units = 4,
            CustDept = "DVM"
        };

        // Act
        var course = await _courseService.CreateCourseAsync(request);

        // Assert
        Assert.NotNull(course);
        Assert.Equal("99999", course.Crn);
        Assert.Equal("TST", course.SubjCode);
        Assert.Equal("101", course.CrseNumb);
        Assert.Equal(25, course.Enrollment);
        Assert.Equal(4, course.Units);
        Assert.Equal("DVM", course.CustDept);

        // Verify saved to database
        var savedCourse = await _context.Courses.FirstOrDefaultAsync(c => c.Crn == "99999");
        Assert.NotNull(savedCourse);
    }

    [Fact]
    public async Task CreateCourseAsync_TrimsAndUppercasesStringFields()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        await _context.SaveChangesAsync();

        var request = new CreateCourseRequest
        {
            TermCode = 202410,
            Crn = " 99999 ",
            SubjCode = " tst ",
            CrseNumb = " 101 ",
            SeqNumb = " 001 ",
            Enrollment = 25,
            Units = 4,
            CustDept = "DVM"
        };

        // Act
        var course = await _courseService.CreateCourseAsync(request);

        // Assert
        Assert.Equal("99999", course.Crn);
        Assert.Equal("TST", course.SubjCode);
        Assert.Equal("101", course.CrseNumb);
        Assert.Equal("001", course.SeqNumb);
        Assert.Equal("DVM", course.CustDept);
    }

    [Fact]
    public void IsValidCustodialDepartment_ReturnsFalse_ForInvalidDepartment()
    {
        // Act & Assert
        Assert.False(_courseService.IsValidCustodialDepartment("INVALID"));
    }

    [Fact]
    public void IsValidCustodialDepartment_ReturnsTrue_ForValidDepartment()
    {
        // Act & Assert
        Assert.True(_courseService.IsValidCustodialDepartment("DVM"));
        Assert.True(_courseService.IsValidCustodialDepartment("dvm")); // Case insensitive
    }

    [Fact]
    public async Task CourseExistsAsync_ReturnsTrue_WhenCourseExists()
    {
        // Arrange
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "99999", SubjCode = "TST", CrseNumb = "101", SeqNumb = "001", Enrollment = 25, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        // Act & Assert
        Assert.True(await _courseService.CourseExistsAsync(202410, "99999", 4));
    }

    [Fact]
    public async Task CourseExistsAsync_ReturnsFalse_WhenCourseDoesNotExist()
    {
        // Act & Assert
        Assert.False(await _courseService.CourseExistsAsync(202410, "99999", 4));
    }

    [Fact]
    public async Task CourseExistsAsync_ReturnsFalse_WhenSameCrnDifferentUnits()
    {
        // Arrange - course exists with 4 units
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "99999", SubjCode = "TST", CrseNumb = "101", SeqNumb = "001", Enrollment = 25, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        // Act & Assert - checking for 2 units should return false
        Assert.False(await _courseService.CourseExistsAsync(202410, "99999", 2));
    }

    [Fact]
    public async Task CreateCourseAsync_CreatesAuditEntry()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        await _context.SaveChangesAsync();

        var request = new CreateCourseRequest
        {
            TermCode = 202410,
            Crn = "99999",
            SubjCode = "TST",
            CrseNumb = "101",
            SeqNumb = "001",
            Enrollment = 25,
            Units = 4,
            CustDept = "DVM"
        };

        // Act
        await _courseService.CreateCourseAsync(request);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddCourseChangeAudit(
                It.IsAny<int>(),
                202410,
                "CreateCourse",
                null,
                It.IsAny<object>()),
            Times.Once);
    }

    #endregion

    #region UpdateCourseAsync Tests

    [Fact]
    public async Task UpdateCourseAsync_UpdatesCourse_WithValidData()
    {
        // Arrange
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        var request = new UpdateCourseRequest
        {
            Enrollment = 30,
            Units = 5,
            CustDept = "VME"
        };

        // Act
        var course = await _courseService.UpdateCourseAsync(1, request);

        // Assert
        Assert.NotNull(course);
        Assert.Equal(30, course.Enrollment);
        Assert.Equal(5, course.Units);
        Assert.Equal("VME", course.CustDept);
    }

    [Fact]
    public async Task UpdateCourseAsync_ReturnsNull_WhenCourseDoesNotExist()
    {
        // Arrange
        var request = new UpdateCourseRequest
        {
            Enrollment = 30,
            Units = 5,
            CustDept = "VME"
        };

        // Act
        var course = await _courseService.UpdateCourseAsync(999, request);

        // Assert
        Assert.Null(course);
    }

    [Fact]
    public async Task UpdateCourseAsync_ThrowsArgumentException_ForInvalidCustodialDepartment()
    {
        // Arrange
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        var request = new UpdateCourseRequest
        {
            Enrollment = 30,
            Units = 5,
            CustDept = "INVALID"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _courseService.UpdateCourseAsync(1, request)
        );
        Assert.Contains("Invalid custodial department", exception.Message);
    }

    [Fact]
    public async Task UpdateCourseAsync_CreatesAuditEntryWithOldAndNewValues()
    {
        // Arrange
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        var request = new UpdateCourseRequest
        {
            Enrollment = 30,
            Units = 5,
            CustDept = "VME"
        };

        // Act
        await _courseService.UpdateCourseAsync(1, request);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddCourseChangeAudit(
                1,
                202410,
                "UpdateCourse",
                It.IsAny<object>(), // Old values
                It.IsAny<object>()), // New values
            Times.Once);
    }

    #endregion

    #region UpdateCourseEnrollmentAsync Tests

    [Fact]
    public async Task UpdateCourseEnrollmentAsync_UpdatesEnrollment_ForRCourse()
    {
        // Arrange - R-course ends with 'R'
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443R", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        // Act
        var course = await _courseService.UpdateCourseEnrollmentAsync(1, 50);

        // Assert
        Assert.NotNull(course);
        Assert.Equal(50, course.Enrollment);
    }

    [Fact]
    public async Task UpdateCourseEnrollmentAsync_ThrowsInvalidOperationException_ForNonRCourse()
    {
        // Arrange - Non R-course (doesn't end with 'R')
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _courseService.UpdateCourseEnrollmentAsync(1, 50)
        );
        Assert.Contains("not an R-course", exception.Message);
    }

    [Fact]
    public async Task UpdateCourseEnrollmentAsync_ReturnsNull_WhenCourseDoesNotExist()
    {
        // Act
        var course = await _courseService.UpdateCourseEnrollmentAsync(999, 50);

        // Assert
        Assert.Null(course);
    }

    #endregion

    #region DeleteCourseAsync Tests

    [Fact]
    public async Task DeleteCourseAsync_DeletesCourse_WhenCourseExists()
    {
        // Arrange
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _courseService.DeleteCourseAsync(1);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.Courses.FindAsync(1));
    }

    [Fact]
    public async Task DeleteCourseAsync_ReturnsFalse_WhenCourseDoesNotExist()
    {
        // Act
        var result = await _courseService.DeleteCourseAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteCourseAsync_DeletesAssociatedRecords()
    {
        // Arrange
        var course = new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        _context.Records.AddRange(
            new EffortRecord { Id = 1, TermCode = 202410, CourseId = 1, PersonId = 100 },
            new EffortRecord { Id = 2, TermCode = 202410, CourseId = 1, PersonId = 101 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _courseService.DeleteCourseAsync(1);

        // Assert
        Assert.True(result);
        Assert.Empty(await _context.Records.Where(r => r.CourseId == 1).ToListAsync());
    }

    [Fact]
    public async Task DeleteCourseAsync_CreatesAuditEntry()
    {
        // Arrange
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        // Act
        await _courseService.DeleteCourseAsync(1);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddCourseChangeAudit(
                1,
                202410,
                "DeleteCourse",
                It.IsAny<object>(), // Course info
                null),
            Times.Once);
    }

    #endregion

    #region CanDeleteCourseAsync Tests

    [Fact]
    public async Task CanDeleteCourseAsync_ReturnsTrueAndZeroCount_WhenNoRecords()
    {
        // Arrange
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        // Act
        var (canDelete, recordCount) = await _courseService.CanDeleteCourseAsync(1);

        // Assert
        Assert.True(canDelete);
        Assert.Equal(0, recordCount);
    }

    [Fact]
    public async Task CanDeleteCourseAsync_ReturnsTrueWithRecordCount_WhenRecordsExist()
    {
        // Arrange
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" });
        _context.Records.AddRange(
            new EffortRecord { Id = 1, TermCode = 202410, CourseId = 1, PersonId = 100 },
            new EffortRecord { Id = 2, TermCode = 202410, CourseId = 1, PersonId = 101 },
            new EffortRecord { Id = 3, TermCode = 202410, CourseId = 1, PersonId = 102 }
        );
        await _context.SaveChangesAsync();

        // Act
        var (canDelete, recordCount) = await _courseService.CanDeleteCourseAsync(1);

        // Assert
        Assert.True(canDelete); // Always true - deletion is allowed
        Assert.Equal(3, recordCount);
    }

    #endregion

    #region GetValidCustodialDepartments Tests

    [Fact]
    public void GetValidCustodialDepartments_ReturnsAllValidDepartments()
    {
        // Act
        var departments = _courseService.GetValidCustodialDepartments();

        // Assert
        Assert.Contains("APC", departments);
        Assert.Contains("VMB", departments);
        Assert.Contains("VME", departments);
        Assert.Contains("VSR", departments);
        Assert.Contains("PMI", departments);
        Assert.Contains("PHR", departments);
        Assert.Contains("UNK", departments);
        Assert.Contains("DVM", departments);
        Assert.Contains("VET", departments);
        Assert.Equal(9, departments.Count);
    }

    #endregion

    #region SearchBannerCoursesAsync Tests

    // Note: SearchBannerCoursesAsync tests are skipped because the method now calls
    // the sp_search_banner_courses stored procedure via raw SQL, which requires
    // a real database connection. These tests should be run as integration tests.

    [Fact]
    public async Task SearchBannerCoursesAsync_ThrowsArgumentException_WhenNoParametersProvided()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _courseService.SearchBannerCoursesAsync(202410));
    }

    #endregion

    #region ImportCourseFromBannerAsync Tests

    [Fact]
    public async Task ImportCourseFromBannerAsync_ImportsCourse_WithCorrectDataMapping()
    {
        // Arrange
        var bannerCourse = new BannerCourseDto
        {
            Crn = "12345",
            SubjCode = "DVM",
            CrseNumb = "443",
            SeqNumb = "001",
            Enrollment = 20,
            UnitType = "F",
            UnitLow = 4,
            UnitHigh = 4,
            DeptCode = "72030"
        };

        var request = new ImportCourseRequest
        {
            TermCode = 202410,
            Crn = "12345"
        };

        // Act
        var course = await _courseService.ImportCourseFromBannerAsync(request, bannerCourse);

        // Assert
        Assert.NotNull(course);
        Assert.Equal("12345", course.Crn);
        Assert.Equal("DVM", course.SubjCode);
        Assert.Equal("443", course.CrseNumb);
        Assert.Equal("001", course.SeqNumb);
        Assert.Equal(20, course.Enrollment);
        Assert.Equal(4, course.Units);
        // DVM is a valid SVM subject code, so it becomes the custodial department directly
        Assert.Equal("DVM", course.CustDept);
    }

    // Note: GetBannerCourseAsync tests are skipped because the method now uses
    // SearchBannerCoursesAsync which calls the sp_search_banner_courses stored procedure.
    // These tests should be run as integration tests against a real database.

    [Fact]
    public async Task ImportCourseFromBannerAsync_UsesDeptCodeMapping_WhenSubjCodeNotValidDept()
    {
        // Arrange - BIS is not a valid SVM department, so it should fall back to DeptCode mapping
        var bannerCourse = new BannerCourseDto
        {
            Crn = "12345",
            SubjCode = "BIS",
            CrseNumb = "101",
            SeqNumb = "001",
            Enrollment = 20,
            UnitType = "F",
            UnitLow = 4,
            UnitHigh = 4,
            DeptCode = "72030" // Should map to VME
        };

        var request = new ImportCourseRequest
        {
            TermCode = 202410,
            Crn = "12345"
        };

        // Act
        var course = await _courseService.ImportCourseFromBannerAsync(request, bannerCourse);

        // Assert - DeptCode 72030 maps to VME when subject code is not a valid department
        Assert.Equal("VME", course.CustDept);
    }

    [Fact]
    public async Task ImportCourseFromBannerAsync_HandlesVariableUnitCourse_WithSpecifiedUnits()
    {
        // Arrange
        var bannerCourse = new BannerCourseDto
        {
            Crn = "12345",
            SubjCode = "DVM",
            CrseNumb = "443",
            SeqNumb = "001",
            Enrollment = 20,
            UnitType = "V",
            UnitLow = 1,
            UnitHigh = 4,
            DeptCode = "72030"
        };

        var request = new ImportCourseRequest
        {
            TermCode = 202410,
            Crn = "12345",
            Units = 3
        };

        // Act
        var course = await _courseService.ImportCourseFromBannerAsync(request, bannerCourse);

        // Assert
        Assert.Equal(3, course.Units);
    }

    [Fact]
    public async Task ImportCourseFromBannerAsync_UsesUnitLow_WhenNoUnitsSpecifiedForVariableCourse()
    {
        // Arrange
        var bannerCourse = new BannerCourseDto
        {
            Crn = "12345",
            SubjCode = "DVM",
            CrseNumb = "443",
            SeqNumb = "001",
            Enrollment = 20,
            UnitType = "V",
            UnitLow = 1,
            UnitHigh = 4,
            DeptCode = "72030"
        };

        var request = new ImportCourseRequest
        {
            TermCode = 202410,
            Crn = "12345"
            // Units not specified
        };

        // Act
        var course = await _courseService.ImportCourseFromBannerAsync(request, bannerCourse);

        // Assert - should use UnitLow
        Assert.Equal(1, course.Units);
    }

    #endregion
}
