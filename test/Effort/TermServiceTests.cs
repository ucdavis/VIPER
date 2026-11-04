using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for TermService term management operations.
/// </summary>
public sealed class TermServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly IEffortAuditService _auditServiceMock;
    private readonly TermService _termService;

    public TermServiceTests()
    {
        var options = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EffortDbContext(options);
        _viperContext = new VIPERContext(
            new DbContextOptionsBuilder<VIPERContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);
        _auditServiceMock = Substitute.For<IEffortAuditService>();
        // Setup synchronous audit methods used within transactions
        _auditServiceMock
            .AddTermChangeAudit(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<object?>());
        _auditServiceMock
            .AddImportAudit(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>());
        // Keep async methods for backward compatibility
        _auditServiceMock
            .LogTermChangeAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _auditServiceMock
            .LogImportAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        _termService = new TermService(_context, _viperContext, _auditServiceMock, new CourseClassificationService());
    }

    public void Dispose()
    {
        _context.Dispose();
        _viperContext.Dispose();
    }

    private const int TestTermCode = 202410;

    private void AddOpenTerm() =>
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode, OpenedDate = DateTime.Now.AddDays(-7) });

    private void AddPerson(int personId, bool verified, int termCode = TestTermCode) =>
        _context.Persons.Add(new EffortPerson
        {
            PersonId = personId,
            TermCode = termCode,
            FirstName = "Test",
            LastName = "Instructor",
            EffortTitleCode = "PROF",
            EffortDept = "VME",
            EffortVerified = verified ? DateTime.Now.AddDays(-1) : null
        });

    private void AddCourse(int id, int enrollment, string crn, string subjCode = "DVM", string crseNumb = "443", int termCode = TestTermCode) =>
        _context.Courses.Add(new EffortCourse
        {
            Id = id,
            TermCode = termCode,
            Enrollment = enrollment,
            Crn = crn,
            SubjCode = subjCode,
            CrseNumb = crseNumb,
            SeqNumb = "001"
        });

    private void AddRecord(int id, int personId, int courseId, int? hours, int? weeks = null, int termCode = TestTermCode) =>
        _context.Records.Add(new EffortRecord
        {
            Id = id,
            PersonId = personId,
            CourseId = courseId,
            TermCode = termCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Crn = "12345",
            Hours = hours,
            Weeks = weeks
        });

    #region GetTermsAsync Tests

    [Fact]
    public async Task GetTermsAsync_ReturnsAllTerms_OrderedByTermCodeDescending()
    {
        // Arrange - Status is computed from dates:
        // Closed = ClosedDate set, Opened = OpenedDate set (no ClosedDate), Created = no dates
        _context.Terms.AddRange(
            new EffortTerm { TermCode = 202310, OpenedDate = DateTime.Now.AddDays(-30), ClosedDate = DateTime.Now.AddDays(-1) },
            new EffortTerm { TermCode = 202410, OpenedDate = DateTime.Now.AddDays(-7) },
            new EffortTerm { TermCode = 202510 }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var terms = await _termService.GetTermsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, terms.Count);
        Assert.Equal(202510, terms[0].TermCode);
        Assert.Equal(202410, terms[1].TermCode);
        Assert.Equal(202310, terms[2].TermCode);
    }

    [Fact]
    public async Task GetTermsAsync_ReturnsEmptyList_WhenNoTermsExist()
    {
        // Act
        var terms = await _termService.GetTermsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(terms);
    }

    #endregion

    #region GetTermAsync Tests

    [Fact]
    public async Task GetTermAsync_ReturnsTerm_WhenTermExists()
    {
        // Arrange - OpenedDate makes status "Opened"
        _context.Terms.Add(new EffortTerm { TermCode = 202410, OpenedDate = DateTime.Now });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var term = await _termService.GetTermAsync(202410, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Equal(202410, term.TermCode);
        Assert.Equal("Opened", term.Status);
    }

    [Fact]
    public async Task GetTermAsync_ReturnsNull_WhenTermDoesNotExist()
    {
        // Act
        var term = await _termService.GetTermAsync(999999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(term);
    }

    #endregion

    #region GetCurrentTermAsync Tests

    [Fact]
    public async Task GetCurrentTermAsync_ReturnsMostRecentOpenedTerm()
    {
        // Arrange - OpenedDate makes status "Opened", no dates = "Created"
        _context.Terms.AddRange(
            new EffortTerm { TermCode = 202310, OpenedDate = DateTime.Now.AddDays(-30) },
            new EffortTerm { TermCode = 202410, OpenedDate = DateTime.Now.AddDays(-7) },
            new EffortTerm { TermCode = 202510 }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var term = await _termService.GetCurrentTermAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Equal(202410, term.TermCode);
    }

    [Fact]
    public async Task GetCurrentTermAsync_ReturnsNull_WhenNoOpenedTerms()
    {
        // Arrange - ClosedDate makes status "Closed"
        _context.Terms.Add(new EffortTerm { TermCode = 202410, OpenedDate = DateTime.Now.AddDays(-30), ClosedDate = DateTime.Now.AddDays(-1) });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var term = await _termService.GetCurrentTermAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(term);
    }

    #endregion

    #region CreateTermAsync Tests

    [Fact]
    public async Task CreateTermAsync_CreatesNewTerm_WithCreatedStatus()
    {
        // Act
        var term = await _termService.CreateTermAsync(202510, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Equal(202510, term.TermCode);
        Assert.Equal("Created", term.Status);

        var savedTerm = await _context.Terms.FindAsync(new object?[] { 202510 }, TestContext.Current.CancellationToken);
        Assert.NotNull(savedTerm);
    }

    [Fact]
    public async Task CreateTermAsync_ThrowsException_WhenTermAlreadyExists()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _termService.CreateTermAsync(202410, ct: TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task CreateTermAsync_StoresExpectedCloseDate_WhenProvided()
    {
        // Arrange
        var expectedDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Local);

        // Act
        var term = await _termService.CreateTermAsync(202510, expectedCloseDate: expectedDate, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Equal(202510, term.TermCode);
        Assert.Equal(expectedDate, term.ExpectedCloseDate);

        var savedTerm = await _context.Terms.FindAsync(new object?[] { 202510 }, TestContext.Current.CancellationToken);
        Assert.NotNull(savedTerm);
        Assert.Equal(expectedDate, savedTerm.ExpectedCloseDate);
    }

    [Fact]
    public async Task CreateTermAsync_LeavesExpectedCloseDateNull_WhenNotProvided()
    {
        // Act
        var term = await _termService.CreateTermAsync(202510, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Null(term.ExpectedCloseDate);

        var savedTerm = await _context.Terms.FindAsync(new object?[] { 202510 }, TestContext.Current.CancellationToken);
        Assert.NotNull(savedTerm);
        Assert.Null(savedTerm.ExpectedCloseDate);
    }

    #endregion

    #region UpdateExpectedCloseDateAsync Tests

    [Fact]
    public async Task UpdateExpectedCloseDateAsync_UpdatesDate_WhenTermIsOpen()
    {
        // Arrange - OpenedDate makes status "Opened"
        _context.Terms.Add(new EffortTerm { TermCode = 202410, OpenedDate = DateTime.Now.AddDays(-7) });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var newDate = new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Local);

        // Act
        var term = await _termService.UpdateExpectedCloseDateAsync(202410, newDate, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Equal(newDate, term.ExpectedCloseDate);

        var savedTerm = await _context.Terms.FindAsync(new object?[] { 202410 }, TestContext.Current.CancellationToken);
        Assert.NotNull(savedTerm);
        Assert.Equal(newDate, savedTerm.ExpectedCloseDate);
    }

    [Fact]
    public async Task UpdateExpectedCloseDateAsync_ThrowsException_WhenTermIsClosed()
    {
        // Arrange - ClosedDate makes status "Closed"
        _context.Terms.Add(new EffortTerm
        {
            TermCode = 202410,
            OpenedDate = DateTime.Now.AddDays(-30),
            ClosedDate = DateTime.Now.AddDays(-1)
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _termService.UpdateExpectedCloseDateAsync(202410, new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Local), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateExpectedCloseDateAsync_ClearsDate_WhenSetToNull()
    {
        // Arrange - Term with existing ExpectedCloseDate
        _context.Terms.Add(new EffortTerm
        {
            TermCode = 202410,
            OpenedDate = DateTime.Now.AddDays(-7),
            ExpectedCloseDate = new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Local)
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var term = await _termService.UpdateExpectedCloseDateAsync(202410, null, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Null(term.ExpectedCloseDate);

        var savedTerm = await _context.Terms.FindAsync(new object?[] { 202410 }, TestContext.Current.CancellationToken);
        Assert.NotNull(savedTerm);
        Assert.Null(savedTerm.ExpectedCloseDate);
    }

    [Fact]
    public async Task UpdateExpectedCloseDateAsync_ReturnsNull_WhenTermNotFound()
    {
        // Act
        var term = await _termService.UpdateExpectedCloseDateAsync(999999, new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Local), TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(term);
    }

    #endregion

    #region Expected Close Date Validation Tests

    [Fact]
    public async Task CreateTermAsync_ThrowsException_WhenExpectedCloseDateBeforeTermEnd()
    {
        // Arrange - Term ends 2025-06-15, expected close is before that
        _viperContext.Terms.Add(new Term
        {
            TermCode = 202510,
            Description = "Fall 2025",
            StartDate = new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Local),
            EndDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Local),
            TermType = "Q"
        });
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _termService.CreateTermAsync(202510, expectedCloseDate: new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Local), TestContext.Current.CancellationToken));
        Assert.Contains("after the term end date", ex.Message);
    }

    [Fact]
    public async Task CreateTermAsync_ThrowsException_WhenExpectedCloseDateOnTermEndDate()
    {
        // Arrange - Term ends 2025-06-15, expected close is the same day
        _viperContext.Terms.Add(new Term
        {
            TermCode = 202510,
            Description = "Fall 2025",
            StartDate = new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Local),
            EndDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Local),
            TermType = "Q"
        });
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _termService.CreateTermAsync(202510, expectedCloseDate: new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Local), TestContext.Current.CancellationToken));
        Assert.Contains("after the term end date", ex.Message);
    }

    [Fact]
    public async Task CreateTermAsync_ThrowsException_WhenExpectedCloseDateMoreThanOneYearAfterTermEnd()
    {
        // Arrange - Term ends 2025-06-15, expected close is more than 1 year later
        _viperContext.Terms.Add(new Term
        {
            TermCode = 202510,
            Description = "Fall 2025",
            StartDate = new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Local),
            EndDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Local),
            TermType = "Q"
        });
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _termService.CreateTermAsync(202510, expectedCloseDate: new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Local), TestContext.Current.CancellationToken));
        Assert.Contains("more than 1 year", ex.Message);
    }

    [Fact]
    public async Task CreateTermAsync_AcceptsExpectedCloseDate_WhenDayAfterTermEnd()
    {
        // Arrange - Term ends 2025-06-15, expected close is 2025-06-16 (day after)
        _viperContext.Terms.Add(new Term
        {
            TermCode = 202510,
            Description = "Fall 2025",
            StartDate = new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Local),
            EndDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Local),
            TermType = "Q"
        });
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var term = await _termService.CreateTermAsync(202510, expectedCloseDate: new DateTime(2025, 6, 16, 0, 0, 0, DateTimeKind.Local), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Equal(new DateTime(2025, 6, 16, 0, 0, 0, DateTimeKind.Local), term.ExpectedCloseDate);
    }

    [Fact]
    public async Task CreateTermAsync_AcceptsExpectedCloseDate_WhenExactlyOneYearAfterTermEnd()
    {
        // Arrange - Term ends 2025-06-15, expected close is exactly 1 year later
        _viperContext.Terms.Add(new Term
        {
            TermCode = 202510,
            Description = "Fall 2025",
            StartDate = new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Local),
            EndDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Local),
            TermType = "Q"
        });
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var term = await _termService.CreateTermAsync(202510, expectedCloseDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Local), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Equal(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Local), term.ExpectedCloseDate);
    }

    [Fact]
    public async Task UpdateExpectedCloseDateAsync_ThrowsException_WhenDateBeforeTermEnd()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410, OpenedDate = DateTime.Now.AddDays(-7) });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _viperContext.Terms.Add(new Term
        {
            TermCode = 202410,
            Description = "Fall 2024",
            StartDate = new DateTime(2024, 9, 25, 0, 0, 0, DateTimeKind.Local),
            EndDate = new DateTime(2024, 12, 13, 0, 0, 0, DateTimeKind.Local),
            TermType = "Q"
        });
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _termService.UpdateExpectedCloseDateAsync(202410, new DateTime(2024, 11, 1, 0, 0, 0, DateTimeKind.Local), TestContext.Current.CancellationToken));
        Assert.Contains("after the term end date", ex.Message);
    }

    [Fact]
    public async Task UpdateExpectedCloseDateAsync_ThrowsException_WhenDateMoreThanOneYearAfterTermEnd()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410, OpenedDate = DateTime.Now.AddDays(-7) });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _viperContext.Terms.Add(new Term
        {
            TermCode = 202410,
            Description = "Fall 2024",
            StartDate = new DateTime(2024, 9, 25, 0, 0, 0, DateTimeKind.Local),
            EndDate = new DateTime(2024, 12, 13, 0, 0, 0, DateTimeKind.Local),
            TermType = "Q"
        });
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _termService.UpdateExpectedCloseDateAsync(202410, new DateTime(2025, 12, 14, 0, 0, 0, DateTimeKind.Local), TestContext.Current.CancellationToken));
        Assert.Contains("more than 1 year", ex.Message);
    }

    [Fact]
    public async Task UpdateExpectedCloseDateAsync_AcceptsDate_WhenWithinValidRange()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410, OpenedDate = DateTime.Now.AddDays(-7) });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _viperContext.Terms.Add(new Term
        {
            TermCode = 202410,
            Description = "Fall 2024",
            StartDate = new DateTime(2024, 9, 25, 0, 0, 0, DateTimeKind.Local),
            EndDate = new DateTime(2024, 12, 13, 0, 0, 0, DateTimeKind.Local),
            TermType = "Q"
        });
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var term = await _termService.UpdateExpectedCloseDateAsync(202410, new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Local), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Equal(new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Local), term.ExpectedCloseDate);
    }

    [Fact]
    public async Task UpdateExpectedCloseDateAsync_SkipsValidation_WhenClearingDate()
    {
        // Arrange - Setting to null should always succeed
        _context.Terms.Add(new EffortTerm
        {
            TermCode = 202410,
            OpenedDate = DateTime.Now.AddDays(-7),
            ExpectedCloseDate = new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Local)
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var term = await _termService.UpdateExpectedCloseDateAsync(202410, null, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Null(term.ExpectedCloseDate);
    }

    [Fact]
    public async Task CreateTermAsync_SkipsValidation_WhenTermNotInViperTerms()
    {
        // Arrange - No vwTerms entry for this term code
        // Validation should be skipped, not throw

        // Act
        var term = await _termService.CreateTermAsync(202510, expectedCloseDate: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local), TestContext.Current.CancellationToken);

        // Assert - Accepts any date when term not found in vwTerms
        Assert.NotNull(term);
        Assert.Equal(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local), term.ExpectedCloseDate);
    }

    #endregion

    #region OpenTermAsync Tests

    [Fact]
    public async Task OpenTermAsync_SetsStatusToOpened_AndSetsOpenedDate()
    {
        // Arrange - HarvestedDate makes status "Harvested"
        _context.Terms.Add(new EffortTerm { TermCode = 202410, HarvestedDate = DateTime.Now.AddDays(-1) });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var term = await _termService.OpenTermAsync(202410, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Equal("Opened", term.Status);
        Assert.NotNull(term.OpenedDate);

        var savedTerm = await _context.Terms.FindAsync(new object?[] { 202410 }, TestContext.Current.CancellationToken);
        Assert.NotNull(savedTerm);
    }

    [Fact]
    public async Task OpenTermAsync_ReturnsNull_WhenTermDoesNotExist()
    {
        // Act
        var term = await _termService.OpenTermAsync(999999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(term);
    }

    #endregion

    #region CloseTermAsync Tests

    [Fact]
    public async Task CloseTermAsync_SetsStatusToClosed_WhenNoZeroEnrollmentCourses()
    {
        // Arrange - OpenedDate makes status "Opened"
        _context.Terms.Add(new EffortTerm { TermCode = 202410, OpenedDate = DateTime.Now.AddDays(-7) });
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Enrollment = 10, Crn = "12345" });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (success, errorMessage) = await _termService.CloseTermAsync(202410, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);

        var savedTerm = await _context.Terms.FindAsync(new object?[] { 202410 }, TestContext.Current.CancellationToken);
        Assert.NotNull(savedTerm);
        Assert.Equal("Closed", savedTerm.Status);
        Assert.NotNull(savedTerm.ClosedDate);
    }

    [Fact]
    public async Task CloseTermAsync_FailsAndChangesNothing_WhenZeroEnrollmentCourseKeepsEffort()
    {
        // Arrange
        AddOpenTerm();
        AddPerson(100, verified: true);
        AddPerson(101, verified: false);
        AddCourse(1, enrollment: 0, crn: "12345", subjCode: "VME", crseNumb: "200");
        AddCourse(2, enrollment: 0, crn: "12346");
        AddCourse(3, enrollment: 10, crn: "12347");
        AddRecord(1, personId: 100, courseId: 1, hours: 5);
        AddRecord(2, personId: 101, courseId: 3, hours: 0);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (success, errorMessage) = await _termService.CloseTermAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(success);
        Assert.Contains("1 course(s) have zero enrollment and effort records: VME 200-001", errorMessage);
        Assert.Equal(2, await _context.Records.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(3, await _context.Courses.CountAsync(TestContext.Current.CancellationToken));
        var term = await _context.Terms.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(term.ClosedDate);
    }

    [Fact]
    public async Task CloseTermAsync_FailsAndDeletesNothing_WhenTermNotOpen()
    {
        // Arrange - harvested but never opened, with data that cleanup would otherwise remove
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode });
        AddPerson(100, verified: false);
        AddCourse(1, enrollment: 0, crn: "12345");
        AddRecord(1, personId: 100, courseId: 1, hours: 0);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (success, errorMessage) = await _termService.CloseTermAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(success);
        Assert.Equal("Term must be opened before it can be closed", errorMessage);
        Assert.Equal(1, await _context.Records.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await _context.Courses.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CloseTermAsync_RemovesOnlyZeroEffortRecordsOfUnverifiedInstructors()
    {
        // Arrange
        AddOpenTerm();
        AddPerson(100, verified: false);
        AddPerson(101, verified: true);
        AddCourse(1, enrollment: 10, crn: "12345");
        AddCourse(2, enrollment: 10, crn: "12346");
        AddRecord(1, personId: 100, courseId: 1, hours: 0);
        AddRecord(2, personId: 100, courseId: 2, hours: null, weeks: null);
        AddRecord(3, personId: 100, courseId: 2, hours: 4);
        AddRecord(4, personId: 100, courseId: 1, hours: null, weeks: 2);
        AddRecord(5, personId: 101, courseId: 1, hours: 0);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (success, _) = await _termService.CloseTermAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(success);
        var remainingIds = await _context.Records.Select(r => r.Id).OrderBy(id => id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal([3, 4, 5], remainingIds);
        // The unverified instructor's report is kept, and closing is still allowed
        Assert.Equal(2, await _context.Persons.CountAsync(TestContext.Current.CancellationToken));
        _auditServiceMock.Received(2).AddRecordChangeAudit(
            Arg.Any<int>(), Arg.Is(TestTermCode), EffortAuditActions.DeleteEffort, Arg.Any<object?>(), Arg.Is<object?>(v => v == null));
    }

    [Fact]
    public async Task CloseTermAsync_RemovesZeroEnrollmentCourse_LeftEmptyByCleanup()
    {
        // Arrange
        AddOpenTerm();
        AddPerson(100, verified: false);
        AddCourse(1, enrollment: 0, crn: "12345");
        AddCourse(2, enrollment: 0, crn: "12346");
        AddCourse(3, enrollment: 10, crn: "12347");
        AddRecord(1, personId: 100, courseId: 1, hours: 0);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (success, _) = await _termService.CloseTermAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(success);
        var course = await _context.Courses.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, course.Id);
        _auditServiceMock.Received(2).AddCourseChangeAudit(
            Arg.Any<int>(), Arg.Is(TestTermCode), EffortAuditActions.DeleteCourse, Arg.Any<object?>(), Arg.Is<object?>(v => v == null));
    }

    [Fact]
    public async Task CloseTermAsync_RemovesEmptyChildCourseAndItsRelationship()
    {
        // Arrange
        AddOpenTerm();
        AddCourse(1, enrollment: 10, crn: "12345");
        AddCourse(2, enrollment: 0, crn: "12346");
        _context.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = 1, ChildCourseId = 2, RelationshipType = "CrossList" });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (success, _) = await _termService.CloseTermAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(success);
        Assert.Equal(1, (await _context.Courses.SingleAsync(TestContext.Current.CancellationToken)).Id);
        Assert.Empty(await _context.CourseRelationships.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CloseTermAsync_KeepsEmptyParentCourse_WhenChildSurvives()
    {
        // Arrange
        AddOpenTerm();
        AddCourse(1, enrollment: 0, crn: "12345");
        AddCourse(2, enrollment: 10, crn: "12346");
        _context.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = 1, ChildCourseId = 2, RelationshipType = "CrossList" });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (success, _) = await _termService.CloseTermAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(success);
        Assert.Equal(2, await _context.Courses.CountAsync(TestContext.Current.CancellationToken));
        Assert.Single(await _context.CourseRelationships.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CloseTermAsync_RemovesEmptyParentCourse_WhenAllChildrenAreRemoved()
    {
        // Arrange
        AddOpenTerm();
        AddCourse(1, enrollment: 0, crn: "12345");
        AddCourse(2, enrollment: 0, crn: "12346");
        _context.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = 1, ChildCourseId = 2, RelationshipType = "CrossList" });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (success, _) = await _termService.CloseTermAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(success);
        Assert.Empty(await _context.Courses.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await _context.CourseRelationships.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CloseTermAsync_GenericRCourseWithEffort_DoesNotBlockAndIsKept()
    {
        // Arrange - weeks-based generic R-course record has Hours = null
        AddOpenTerm();
        AddPerson(100, verified: false);
        AddCourse(1, enrollment: 0, crn: "RESID");
        AddRecord(1, personId: 100, courseId: 1, hours: null, weeks: 1);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (success, errorMessage) = await _termService.CloseTermAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(success, errorMessage);
        Assert.Equal(1, await _context.Courses.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await _context.Records.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CloseTermAsync_GenericRCourseWithNoEffort_IsRemoved()
    {
        // Arrange
        AddOpenTerm();
        AddPerson(100, verified: false);
        AddCourse(1, enrollment: 0, crn: "RESID");
        AddRecord(1, personId: 100, courseId: 1, hours: 0);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (success, _) = await _termService.CloseTermAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(success);
        Assert.Empty(await _context.Courses.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await _context.Records.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CloseTermAsync_LeavesOtherTermsUntouched()
    {
        // Arrange
        AddOpenTerm();
        const int otherTerm = 202501;
        _context.Terms.Add(new EffortTerm { TermCode = otherTerm });
        AddPerson(100, verified: false, termCode: otherTerm);
        AddCourse(1, enrollment: 0, crn: "12345", termCode: otherTerm);
        AddRecord(1, personId: 100, courseId: 1, hours: 0, termCode: otherTerm);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (success, _) = await _termService.CloseTermAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(success);
        Assert.Equal(1, await _context.Courses.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await _context.Records.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CloseTermAsync_Fails_WhenTermNotFound()
    {
        // Act
        var (success, errorMessage) = await _termService.CloseTermAsync(999999, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(success);
        Assert.Equal("Term not found", errorMessage);
    }

    #endregion

    #region ReopenTermAsync Tests

    [Fact]
    public async Task ReopenTermAsync_SetsStatusToOpened_AndClearsClosedDate()
    {
        // Arrange - ClosedDate makes status "Closed"
        _context.Terms.Add(new EffortTerm
        {
            TermCode = 202410,
            OpenedDate = DateTime.Now.AddDays(-30),
            ClosedDate = DateTime.Now.AddDays(-1)
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var term = await _termService.ReopenTermAsync(202410, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Equal("Opened", term.Status);
        Assert.Null(term.ClosedDate);
    }

    #endregion

    #region UnopenTermAsync Tests

    [Fact]
    public async Task UnopenTermAsync_SetsStatusToHarvested_WhenHarvestedDateExists()
    {
        // Arrange - OpenedDate makes status "Opened"
        _context.Terms.Add(new EffortTerm
        {
            TermCode = 202410,
            HarvestedDate = DateTime.Now.AddDays(-5),
            OpenedDate = DateTime.Now
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var term = await _termService.UnopenTermAsync(202410, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Equal("Harvested", term.Status);
        Assert.Null(term.OpenedDate);
    }

    [Fact]
    public async Task UnopenTermAsync_SetsStatusToCreated_WhenNoHarvestedDate()
    {
        // Arrange - OpenedDate makes status "Opened", no HarvestedDate
        _context.Terms.Add(new EffortTerm
        {
            TermCode = 202410,
            OpenedDate = DateTime.Now
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var term = await _termService.UnopenTermAsync(202410, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(term);
        Assert.Equal("Created", term.Status);
        Assert.Null(term.OpenedDate);
    }

    #endregion

    #region DeleteTermAsync Tests

    [Fact]
    public async Task DeleteTermAsync_DeletesTerm_WhenNoRelatedData()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _termService.DeleteTermAsync(202410, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.Terms.FindAsync(new object?[] { 202410 }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteTermAsync_ReturnsFalse_WhenCoursesExist()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345" });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _termService.DeleteTermAsync(202410, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
        Assert.NotNull(await _context.Terms.FindAsync(new object?[] { 202410 }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteTermAsync_ReturnsFalse_WhenPersonsExist()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = 202410 });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _termService.DeleteTermAsync(202410, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteTermAsync_ReturnsFalse_WhenRecordsExist()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        _context.Records.Add(new EffortRecord { Id = 1, TermCode = 202410, PersonId = 1, CourseId = 1 });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _termService.DeleteTermAsync(202410, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CanDeleteTermAsync Tests

    [Fact]
    public async Task CanDeleteTermAsync_ReturnsTrue_WhenNoRelatedData()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var canDelete = await _termService.CanDeleteTermAsync(202410, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(canDelete);
    }

    [Fact]
    public async Task CanDeleteTermAsync_ReturnsFalse_WhenCoursesExist()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345" });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var canDelete = await _termService.CanDeleteTermAsync(202410, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(canDelete);
    }

    #endregion

    #region CanCloseTermAsync Tests

    [Fact]
    public async Task CanCloseTermAsync_ReturnsCanCloseTrue_WhenNoZeroEnrollmentCourses()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Enrollment = 10, Crn = "12345" });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (canClose, blockingCourseCount) = await _termService.CanCloseTermAsync(202410, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(canClose);
        Assert.Equal(0, blockingCourseCount);
    }

    [Fact]
    public async Task CanCloseTermAsync_CountsOnlyZeroEnrollmentCoursesThatKeepEffort()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode });
        AddPerson(100, verified: true);
        AddPerson(101, verified: false);
        AddCourse(1, enrollment: 0, crn: "12345");
        AddCourse(2, enrollment: 0, crn: "12346");
        AddCourse(3, enrollment: 0, crn: "12347");
        AddCourse(4, enrollment: 5, crn: "12348");
        AddRecord(1, personId: 100, courseId: 1, hours: 5);
        AddRecord(2, personId: 101, courseId: 2, hours: 0);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (canClose, blockingCourseCount) = await _termService.CanCloseTermAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(canClose);
        Assert.Equal(1, blockingCourseCount);
    }

    [Fact]
    public async Task CanCloseTermAsync_DoesNotTrackEntities()
    {
        // Arrange
        AddOpenTerm();
        AddPerson(100, verified: false);
        AddCourse(1, enrollment: 0, crn: "12345");
        AddCourse(2, enrollment: 10, crn: "12346");
        AddRecord(1, personId: 100, courseId: 1, hours: 0);
        _context.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = 2, ChildCourseId = 1, RelationshipType = "CrossList" });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        // Act
        await _termService.CanCloseTermAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(_context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task CanCloseTermAsync_VerifiedZeroEffortRecordStillBlocks()
    {
        // Arrange - cleanup only touches unverified instructors
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode });
        AddPerson(100, verified: true);
        AddCourse(1, enrollment: 0, crn: "12345");
        AddRecord(1, personId: 100, courseId: 1, hours: 0);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (canClose, blockingCourseCount) = await _termService.CanCloseTermAsync(TestTermCode, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(canClose);
        Assert.Equal(1, blockingCourseCount);
    }

    #endregion

    #region GetAvailableTermsAsync Tests

    [Fact]
    public async Task GetAvailableTermsAsync_ReturnsFutureTerms_NotAlreadyInEffort()
    {
        // Arrange - Add existing effort term
        _context.Terms.Add(new EffortTerm { TermCode = 202510 });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add terms to VIPER context (simulating vwTerms)
        _viperContext.Terms.AddRange(
            new Term { TermCode = 202510, Description = "Fall 2025", StartDate = DateTime.Today.AddMonths(3), TermType = "Q" },
            new Term { TermCode = 202520, Description = "Winter 2026", StartDate = DateTime.Today.AddMonths(6), TermType = "Q" },
            new Term { TermCode = 202530, Description = "Spring 2026", StartDate = DateTime.Today.AddMonths(9), TermType = "Q" }
        );
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var available = await _termService.GetAvailableTermsAsync(TestContext.Current.CancellationToken);

        // Assert - Should exclude 202510 (already in Effort)
        Assert.Equal(2, available.Count);
        Assert.DoesNotContain(available, t => t.TermCode == 202510);
        Assert.Contains(available, t => t.TermCode == 202520);
        Assert.Contains(available, t => t.TermCode == 202530);
    }

    [Fact]
    public async Task GetAvailableTermsAsync_ExcludesFacilityScheduleTerm()
    {
        // Arrange - Add facility schedule term to VIPER context
        _viperContext.Terms.AddRange(
            new Term { TermCode = 202520, Description = "Winter 2026", StartDate = DateTime.Today.AddMonths(6), TermType = "Q" },
            new Term { TermCode = Term.FacilityScheduleTermCode, Description = "(DO NOT USE) Facility Schedule", StartDate = DateTime.Today.AddMonths(12), TermType = "Q" }
        );
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var available = await _termService.GetAvailableTermsAsync(TestContext.Current.CancellationToken);

        // Assert - Should exclude facility schedule term
        Assert.Single(available);
        Assert.DoesNotContain(available, t => t.TermCode == Term.FacilityScheduleTermCode);
    }

    [Fact]
    public async Task GetAvailableTermsAsync_ExcludesPastTerms()
    {
        // Arrange - Add past and future terms
        _viperContext.Terms.AddRange(
            new Term { TermCode = 202310, Description = "Fall 2023", StartDate = DateTime.Today.AddMonths(-12), TermType = "Q" },
            new Term { TermCode = 202520, Description = "Winter 2026", StartDate = DateTime.Today.AddMonths(6), TermType = "Q" }
        );
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var available = await _termService.GetAvailableTermsAsync(TestContext.Current.CancellationToken);

        // Assert - Should exclude past term
        Assert.Single(available);
        Assert.DoesNotContain(available, t => t.TermCode == 202310);
        Assert.Contains(available, t => t.TermCode == 202520);
    }

    [Fact]
    public async Task GetAvailableTermsAsync_ReturnsEmptyList_WhenAllTermsAlreadyExist()
    {
        // Arrange - Add all future terms to Effort
        _context.Terms.Add(new EffortTerm { TermCode = 202520 });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _viperContext.Terms.Add(new Term { TermCode = 202520, Description = "Winter 2026", StartDate = DateTime.Today.AddMonths(6), TermType = "Q" });
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var available = await _termService.GetAvailableTermsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(available);
    }

    #endregion
}
