using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for RCourseService - the shared R-course creation logic.
/// </summary>
public sealed class RCourseServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly IEffortAuditService _auditServiceMock;
    private readonly ILogger<RCourseService> _loggerMock;
    private readonly RCourseService _service;

    private const int TestTermCode = 202410;
    private const int TestPersonId = 100;
    private const int TestModifiedBy = 999;

    public RCourseServiceTests()
    {
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EffortDbContext(effortOptions);
        _auditServiceMock = Substitute.For<IEffortAuditService>();
        _loggerMock = Substitute.For<ILogger<RCourseService>>();

        _service = new RCourseService(
            _context,
            _auditServiceMock,
            _loggerMock);

        SeedTestData();
    }

    private void SeedTestData()
    {
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode, OpenedDate = DateTime.Now });

        // Use real effort types from production database schema
        _context.EffortTypes.AddRange(
            new EffortType { Id = "CLI", Description = "Clinical", IsActive = true, UsesWeeks = true, AllowedOnRCourses = true },
            new EffortType { Id = "LEC", Description = "Lecture", IsActive = true, UsesWeeks = false, AllowedOnRCourses = true },
            new EffortType { Id = "VAR", Description = "Variable", IsActive = true, UsesWeeks = false, AllowedOnRCourses = false }
        );

        _context.Roles.Add(new EffortRole { Id = 1, Description = "Instructor of Record", IsActive = true, SortOrder = 1 });

        _context.Persons.Add(new EffortPerson
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            FirstName = "Test",
            LastName = "Instructor",
            EffortDept = "VME"
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetOrCreateGenericRCourseAsync Tests

    [Fact]
    public async Task GetOrCreateGenericRCourseAsync_CreatesNewCourse_WhenNotExists()
    {
        // Act
        var course = await _service.GetOrCreateGenericRCourseAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(course);
        Assert.Equal(TestTermCode, course.TermCode);
        Assert.Equal("RESID", course.Crn);
        Assert.Equal("RES", course.SubjCode);
        Assert.Equal("000R", course.CrseNumb);
        Assert.Equal("001", course.SeqNumb);
        Assert.Equal(0, course.Units);
        Assert.Equal(0, course.Enrollment);

        // Verify course was persisted
        var persisted = await _context.Courses.FirstOrDefaultAsync(c => c.Crn == "RESID" && c.TermCode == TestTermCode, TestContext.Current.CancellationToken);
        Assert.NotNull(persisted);
        Assert.Equal(course.Id, persisted.Id);

        // Verify audit was logged
        _auditServiceMock.Received(1).AddCourseChangeAudit(
            course.Id,
            TestTermCode,
            EffortAuditActions.CreateCourse,
            Arg.Is<object?>(x => x == null),
            Arg.Any<object>());
    }

    [Fact]
    public async Task GetOrCreateGenericRCourseAsync_ReturnsExistingCourse_WhenAlreadyExists()
    {
        // Arrange - Create the course first
        var existingCourse = new EffortCourse
        {
            TermCode = TestTermCode,
            Crn = "RESID",
            SubjCode = "RES",
            CrseNumb = "000R",
            SeqNumb = "001",
            Units = 0,
            Enrollment = 0,
            CustDept = "VME"
        };
        _context.Courses.Add(existingCourse);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var course = await _service.GetOrCreateGenericRCourseAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(course);
        Assert.Equal(existingCourse.Id, course.Id);

        // Verify audit was NOT logged (no new course created)
        _auditServiceMock.DidNotReceive().AddCourseChangeAudit(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<object?>(),
            Arg.Any<object?>());
    }

    [Fact]
    public async Task GetOrCreateGenericRCourseAsync_CreatesSeparateCourses_ForDifferentTerms()
    {
        // Arrange
        var termCode1 = 202410;
        var termCode2 = 202420;
        _context.Terms.Add(new EffortTerm { TermCode = termCode2 });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var course1 = await _service.GetOrCreateGenericRCourseAsync(termCode1, TestContext.Current.CancellationToken);
        var course2 = await _service.GetOrCreateGenericRCourseAsync(termCode2, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(course1);
        Assert.NotNull(course2);
        Assert.NotEqual(course1.Id, course2.Id);
        Assert.Equal(termCode1, course1.TermCode);
        Assert.Equal(termCode2, course2.TermCode);
    }

    #endregion

    #region CreateRCourseEffortRecordAsync Tests

    [Fact]
    public async Task CreateRCourseEffortRecordAsync_CreatesRecord_WhenNotExists()
    {
        // Act
        await _service.CreateRCourseEffortRecordAsync(TestPersonId, TestTermCode, TestModifiedBy, RCourseCreationContext.Harvest, TestContext.Current.CancellationToken);

        // Assert - Verify effort record was created
        var record = await _context.Records
            .Include(r => r.Course)
            .FirstOrDefaultAsync(r => r.PersonId == TestPersonId && r.Course.Crn == "RESID", TestContext.Current.CancellationToken);

        Assert.NotNull(record);
        Assert.Equal(TestPersonId, record.PersonId);
        Assert.Equal(TestTermCode, record.TermCode);
        Assert.Equal("CLI", record.EffortTypeId); // First alphabetically among allowed R-course types (CLI < LEC)
        Assert.Equal(EffortConstants.InstructorRoleId, record.RoleId);
        // CLI uses weeks, so Hours should be null and Weeks should be 1 (minimum valid value)
        Assert.Null(record.Hours);
        Assert.Equal(1, record.Weeks);
        Assert.Equal("RESID", record.Crn);
        Assert.Equal(TestModifiedBy, record.ModifiedBy);
    }

    [Fact]
    public async Task CreateRCourseEffortRecordAsync_IsIdempotent_WhenRecordExists()
    {
        // Arrange - Create the R-course and effort record first
        var genericRCourse = new EffortCourse
        {
            TermCode = TestTermCode,
            Crn = "RESID",
            SubjCode = "RES",
            CrseNumb = "000R",
            SeqNumb = "001",
            Units = 0,
            Enrollment = 0,
            CustDept = "VME"
        };
        _context.Courses.Add(genericRCourse);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var existingRecord = new EffortRecord
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = genericRCourse.Id,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 5, // Non-zero to distinguish from auto-created (LEC uses hours)
            Weeks = null,
            Crn = "RESID"
        };
        _context.Records.Add(existingRecord);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var countBefore = await _context.Records.CountAsync(TestContext.Current.CancellationToken);

        // Act
        await _service.CreateRCourseEffortRecordAsync(TestPersonId, TestTermCode, TestModifiedBy, RCourseCreationContext.OnDemand, TestContext.Current.CancellationToken);

        // Assert - No new record created
        var countAfter = await _context.Records.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(countBefore, countAfter);

        // Original record unchanged
        var record = await _context.Records.FirstAsync(r => r.Id == existingRecord.Id, TestContext.Current.CancellationToken);
        Assert.Equal(5, record.Hours);
        Assert.Null(record.Weeks);
    }

    [Fact]
    public async Task CreateRCourseEffortRecordAsync_DoesNotCreateRecord_WhenNoEligibleEffortType()
    {
        // Arrange - Remove all R-course allowed effort types
        var allowedTypes = await _context.EffortTypes.Where(t => t.AllowedOnRCourses).ToListAsync(TestContext.Current.CancellationToken);
        _context.EffortTypes.RemoveRange(allowedTypes);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _service.CreateRCourseEffortRecordAsync(TestPersonId, TestTermCode, TestModifiedBy, RCourseCreationContext.Harvest, TestContext.Current.CancellationToken);

        // Assert - No effort record created
        var record = await _context.Records
            .Include(r => r.Course)
            .FirstOrDefaultAsync(r => r.PersonId == TestPersonId && r.Course.Crn == "RESID", TestContext.Current.CancellationToken);

        Assert.Null(record);

        // Course may still be created, but no effort record
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Crn == "RESID", TestContext.Current.CancellationToken);
        Assert.NotNull(course); // Course is created first, then effort type check happens
    }

    [Fact]
    public async Task CreateRCourseEffortRecordAsync_SelectsFirstAlphabeticalEffortType()
    {
        // The seeded data has CLI and LEC as allowed R-course types
        // CLI comes first alphabetically

        // Act
        await _service.CreateRCourseEffortRecordAsync(TestPersonId, TestTermCode, TestModifiedBy, RCourseCreationContext.Harvest, TestContext.Current.CancellationToken);

        // Assert
        var record = await _context.Records.FirstOrDefaultAsync(r => r.PersonId == TestPersonId, TestContext.Current.CancellationToken);
        Assert.NotNull(record);
        Assert.Equal("CLI", record.EffortTypeId);
    }

    [Fact]
    public async Task CreateRCourseEffortRecordAsync_SetsHoursOnly_WhenEffortTypeUsesHours()
    {
        // Arrange - Remove week-based types so LEC (UsesWeeks=false) is selected
        var weekBasedTypes = await _context.EffortTypes
            .Where(t => t.AllowedOnRCourses && t.UsesWeeks)
            .ToListAsync(TestContext.Current.CancellationToken);
        _context.EffortTypes.RemoveRange(weekBasedTypes);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _service.CreateRCourseEffortRecordAsync(TestPersonId, TestTermCode, TestModifiedBy, RCourseCreationContext.Harvest, TestContext.Current.CancellationToken);

        // Assert - Hours set, Weeks null (XOR constraint: CK_Records_HoursOrWeeks)
        var record = await _context.Records.FirstOrDefaultAsync(r => r.PersonId == TestPersonId, TestContext.Current.CancellationToken);
        Assert.NotNull(record);
        Assert.Equal(0, record.Hours);
        Assert.Null(record.Weeks);
        Assert.Equal("LEC", record.EffortTypeId);
    }

    [Fact]
    public async Task CreateRCourseEffortRecordAsync_SetsWeeksOnly_WhenEffortTypeUsesWeeks()
    {
        // Arrange - Remove hour-based types so CLI (UsesWeeks=true) is selected
        var hourBasedTypes = await _context.EffortTypes
            .Where(t => t.AllowedOnRCourses && !t.UsesWeeks)
            .ToListAsync(TestContext.Current.CancellationToken);
        _context.EffortTypes.RemoveRange(hourBasedTypes);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _service.CreateRCourseEffortRecordAsync(TestPersonId, TestTermCode, TestModifiedBy, RCourseCreationContext.Harvest, TestContext.Current.CancellationToken);

        // Assert - Weeks set to 1 (min valid value), Hours null (XOR constraint: CK_Records_HoursOrWeeks)
        var record = await _context.Records.FirstOrDefaultAsync(r => r.PersonId == TestPersonId, TestContext.Current.CancellationToken);
        Assert.NotNull(record);
        Assert.Null(record.Hours);
        Assert.Equal(1, record.Weeks); // Weeks must be > 0 per CK_Records_Weeks constraint
        Assert.Equal("CLI", record.EffortTypeId);
    }

    [Fact]
    public async Task CreateRCourseEffortRecordAsync_CreatesAuditEntry_WithHarvestContext()
    {
        // Act
        await _service.CreateRCourseEffortRecordAsync(TestPersonId, TestTermCode, TestModifiedBy, RCourseCreationContext.Harvest, TestContext.Current.CancellationToken);

        // Assert - Verify audit entry was created (checking via context, not mock)
        var auditEntry = await _context.Audits
            .FirstOrDefaultAsync(a => a.Action == EffortAuditActions.RCourseAutoCreated, TestContext.Current.CancellationToken);

        Assert.NotNull(auditEntry);
        Assert.Equal(EffortAuditTables.Records, auditEntry.TableName);
        Assert.Equal(TestTermCode, auditEntry.TermCode);
        Assert.Equal(TestModifiedBy, auditEntry.ChangedBy);
        Assert.Contains("during harvest", auditEntry.Changes);
    }

    [Fact]
    public async Task CreateRCourseEffortRecordAsync_CreatesAuditEntry_WithOnDemandContext()
    {
        // Act
        await _service.CreateRCourseEffortRecordAsync(TestPersonId, TestTermCode, TestModifiedBy, RCourseCreationContext.OnDemand, TestContext.Current.CancellationToken);

        // Assert - Verify audit entry was created with correct context
        var auditEntry = await _context.Audits
            .FirstOrDefaultAsync(a => a.Action == EffortAuditActions.RCourseAutoCreated, TestContext.Current.CancellationToken);

        Assert.NotNull(auditEntry);
        Assert.Contains("when first non-R-course added", auditEntry.Changes);
    }

    [Fact]
    public async Task CreateRCourseEffortRecordAsync_CreatesSeparateRecords_ForDifferentInstructors()
    {
        // Arrange - Add another instructor
        var personId2 = 200;
        _context.Persons.Add(new EffortPerson
        {
            PersonId = personId2,
            TermCode = TestTermCode,
            FirstName = "Another",
            LastName = "Instructor",
            EffortDept = "VME"
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _service.CreateRCourseEffortRecordAsync(TestPersonId, TestTermCode, TestModifiedBy, RCourseCreationContext.Harvest, TestContext.Current.CancellationToken);
        await _service.CreateRCourseEffortRecordAsync(personId2, TestTermCode, TestModifiedBy, RCourseCreationContext.Harvest, TestContext.Current.CancellationToken);

        // Assert
        var records = await _context.Records
            .Include(r => r.Course)
            .Where(r => r.Course.Crn == "RESID" && r.TermCode == TestTermCode)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, records.Count);
        Assert.Contains(records, r => r.PersonId == TestPersonId);
        Assert.Contains(records, r => r.PersonId == personId2);

        // Both share the same course
        var courseIds = records.Select(r => r.CourseId).Distinct().ToList();
        Assert.Single(courseIds);
    }

    #endregion
}
