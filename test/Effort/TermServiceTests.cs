using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Models;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for TermService term management operations.
/// </summary>
public sealed class TermServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly IMapper _mapper;
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
        _auditServiceMock = new Mock<IEffortAuditService>();
        // Setup synchronous audit methods used within transactions
        _auditServiceMock
            .Setup(s => s.AddTermChangeAudit(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>()));
        _auditServiceMock
            .Setup(s => s.AddImportAudit(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()));
        // Keep async methods for backward compatibility
        _auditServiceMock
            .Setup(s => s.LogTermChangeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _auditServiceMock
            .Setup(s => s.LogImportAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfileEffort>());
        _mapper = mapperConfig.CreateMapper();

        _termService = new TermService(_context, _viperContext, _auditServiceMock.Object, _mapper);
    }

    public void Dispose()
    {
        _context.Dispose();
        _viperContext.Dispose();
    }

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
        await _context.SaveChangesAsync();

        // Act
        var terms = await _termService.GetTermsAsync();

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
        var terms = await _termService.GetTermsAsync();

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
        await _context.SaveChangesAsync();

        // Act
        var term = await _termService.GetTermAsync(202410);

        // Assert
        Assert.NotNull(term);
        Assert.Equal(202410, term.TermCode);
        Assert.Equal("Opened", term.Status);
    }

    [Fact]
    public async Task GetTermAsync_ReturnsNull_WhenTermDoesNotExist()
    {
        // Act
        var term = await _termService.GetTermAsync(999999);

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
        await _context.SaveChangesAsync();

        // Act
        var term = await _termService.GetCurrentTermAsync();

        // Assert
        Assert.NotNull(term);
        Assert.Equal(202410, term.TermCode);
    }

    [Fact]
    public async Task GetCurrentTermAsync_ReturnsNull_WhenNoOpenedTerms()
    {
        // Arrange - ClosedDate makes status "Closed"
        _context.Terms.Add(new EffortTerm { TermCode = 202410, OpenedDate = DateTime.Now.AddDays(-30), ClosedDate = DateTime.Now.AddDays(-1) });
        await _context.SaveChangesAsync();

        // Act
        var term = await _termService.GetCurrentTermAsync();

        // Assert
        Assert.Null(term);
    }

    #endregion

    #region CreateTermAsync Tests

    [Fact]
    public async Task CreateTermAsync_CreatesNewTerm_WithCreatedStatus()
    {
        // Act
        var term = await _termService.CreateTermAsync(202510);

        // Assert
        Assert.NotNull(term);
        Assert.Equal(202510, term.TermCode);
        Assert.Equal("Created", term.Status);

        var savedTerm = await _context.Terms.FindAsync(202510);
        Assert.NotNull(savedTerm);
    }

    [Fact]
    public async Task CreateTermAsync_ThrowsException_WhenTermAlreadyExists()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _termService.CreateTermAsync(202410)
        );
    }

    #endregion

    #region OpenTermAsync Tests

    [Fact]
    public async Task OpenTermAsync_SetsStatusToOpened_AndSetsOpenedDate()
    {
        // Arrange - HarvestedDate makes status "Harvested"
        _context.Terms.Add(new EffortTerm { TermCode = 202410, HarvestedDate = DateTime.Now.AddDays(-1) });
        await _context.SaveChangesAsync();

        // Act
        var term = await _termService.OpenTermAsync(202410);

        // Assert
        Assert.NotNull(term);
        Assert.Equal("Opened", term.Status);
        Assert.NotNull(term.OpenedDate);

        var savedTerm = await _context.Terms.FindAsync(202410);
        Assert.NotNull(savedTerm);
    }

    [Fact]
    public async Task OpenTermAsync_ReturnsNull_WhenTermDoesNotExist()
    {
        // Act
        var term = await _termService.OpenTermAsync(999999);

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
        await _context.SaveChangesAsync();

        // Act
        var (success, errorMessage) = await _termService.CloseTermAsync(202410);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);

        var savedTerm = await _context.Terms.FindAsync(202410);
        Assert.NotNull(savedTerm);
        Assert.Equal("Closed", savedTerm.Status);
        Assert.NotNull(savedTerm.ClosedDate);
    }

    [Fact]
    public async Task CloseTermAsync_Fails_WhenCoursesHaveZeroEnrollment()
    {
        // Arrange - OpenedDate makes status "Opened"
        _context.Terms.Add(new EffortTerm { TermCode = 202410, OpenedDate = DateTime.Now.AddDays(-7) });
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Enrollment = 0, Crn = "12345" });
        _context.Courses.Add(new EffortCourse { Id = 2, TermCode = 202410, Enrollment = 0, Crn = "12346" });
        await _context.SaveChangesAsync();

        // Act
        var (success, errorMessage) = await _termService.CloseTermAsync(202410);

        // Assert
        Assert.False(success);
        Assert.Contains("2 course(s) have zero enrollment", errorMessage);
    }

    [Fact]
    public async Task CloseTermAsync_Fails_WhenTermNotFound()
    {
        // Act
        var (success, errorMessage) = await _termService.CloseTermAsync(999999);

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
        await _context.SaveChangesAsync();

        // Act
        var term = await _termService.ReopenTermAsync(202410);

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
        await _context.SaveChangesAsync();

        // Act
        var term = await _termService.UnopenTermAsync(202410);

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
        await _context.SaveChangesAsync();

        // Act
        var term = await _termService.UnopenTermAsync(202410);

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
        await _context.SaveChangesAsync();

        // Act
        var result = await _termService.DeleteTermAsync(202410);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.Terms.FindAsync(202410));
    }

    [Fact]
    public async Task DeleteTermAsync_ReturnsFalse_WhenCoursesExist()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _termService.DeleteTermAsync(202410);

        // Assert
        Assert.False(result);
        Assert.NotNull(await _context.Terms.FindAsync(202410));
    }

    [Fact]
    public async Task DeleteTermAsync_ReturnsFalse_WhenPersonsExist()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        _context.Persons.Add(new EffortPerson { PersonId = 1, TermCode = 202410 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _termService.DeleteTermAsync(202410);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteTermAsync_ReturnsFalse_WhenRecordsExist()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        _context.Records.Add(new EffortRecord { Id = 1, TermCode = 202410, PersonId = 1, CourseId = 1 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _termService.DeleteTermAsync(202410);

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
        await _context.SaveChangesAsync();

        // Act
        var canDelete = await _termService.CanDeleteTermAsync(202410);

        // Assert
        Assert.True(canDelete);
    }

    [Fact]
    public async Task CanDeleteTermAsync_ReturnsFalse_WhenCoursesExist()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Crn = "12345" });
        await _context.SaveChangesAsync();

        // Act
        var canDelete = await _termService.CanDeleteTermAsync(202410);

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
        await _context.SaveChangesAsync();

        // Act
        var (canClose, zeroEnrollmentCount) = await _termService.CanCloseTermAsync(202410);

        // Assert
        Assert.True(canClose);
        Assert.Equal(0, zeroEnrollmentCount);
    }

    [Fact]
    public async Task CanCloseTermAsync_ReturnsCanCloseFalse_WithZeroEnrollmentCount()
    {
        // Arrange
        _context.Terms.Add(new EffortTerm { TermCode = 202410 });
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = 202410, Enrollment = 0, Crn = "12345" });
        _context.Courses.Add(new EffortCourse { Id = 2, TermCode = 202410, Enrollment = 0, Crn = "12346" });
        _context.Courses.Add(new EffortCourse { Id = 3, TermCode = 202410, Enrollment = 5, Crn = "12347" });
        await _context.SaveChangesAsync();

        // Act
        var (canClose, zeroEnrollmentCount) = await _termService.CanCloseTermAsync(202410);

        // Assert
        Assert.False(canClose);
        Assert.Equal(2, zeroEnrollmentCount);
    }

    #endregion

    #region GetAvailableTermsAsync Tests

    [Fact]
    public async Task GetAvailableTermsAsync_ReturnsFutureTerms_NotAlreadyInEffort()
    {
        // Arrange - Add existing effort term
        _context.Terms.Add(new EffortTerm { TermCode = 202510 });
        await _context.SaveChangesAsync();

        // Add terms to VIPER context (simulating vwTerms)
        _viperContext.Terms.AddRange(
            new Viper.Models.VIPER.Term { TermCode = 202510, Description = "Fall 2025", StartDate = DateTime.Today.AddMonths(3), TermType = "Q" },
            new Viper.Models.VIPER.Term { TermCode = 202520, Description = "Winter 2026", StartDate = DateTime.Today.AddMonths(6), TermType = "Q" },
            new Viper.Models.VIPER.Term { TermCode = 202530, Description = "Spring 2026", StartDate = DateTime.Today.AddMonths(9), TermType = "Q" }
        );
        await _viperContext.SaveChangesAsync();

        // Act
        var available = await _termService.GetAvailableTermsAsync();

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
            new Viper.Models.VIPER.Term { TermCode = 202520, Description = "Winter 2026", StartDate = DateTime.Today.AddMonths(6), TermType = "Q" },
            new Viper.Models.VIPER.Term { TermCode = Viper.Models.VIPER.Term.FacilityScheduleTermCode, Description = "(DO NOT USE) Facility Schedule", StartDate = DateTime.Today.AddMonths(12), TermType = "Q" }
        );
        await _viperContext.SaveChangesAsync();

        // Act
        var available = await _termService.GetAvailableTermsAsync();

        // Assert - Should exclude facility schedule term
        Assert.Single(available);
        Assert.DoesNotContain(available, t => t.TermCode == Viper.Models.VIPER.Term.FacilityScheduleTermCode);
    }

    [Fact]
    public async Task GetAvailableTermsAsync_ExcludesPastTerms()
    {
        // Arrange - Add past and future terms
        _viperContext.Terms.AddRange(
            new Viper.Models.VIPER.Term { TermCode = 202310, Description = "Fall 2023", StartDate = DateTime.Today.AddMonths(-12), TermType = "Q" },
            new Viper.Models.VIPER.Term { TermCode = 202520, Description = "Winter 2026", StartDate = DateTime.Today.AddMonths(6), TermType = "Q" }
        );
        await _viperContext.SaveChangesAsync();

        // Act
        var available = await _termService.GetAvailableTermsAsync();

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
        await _context.SaveChangesAsync();

        _viperContext.Terms.Add(new Viper.Models.VIPER.Term { TermCode = 202520, Description = "Winter 2026", StartDate = DateTime.Today.AddMonths(6), TermType = "Q" });
        await _viperContext.SaveChangesAsync();

        // Act
        var available = await _termService.GetAvailableTermsAsync();

        // Assert
        Assert.Empty(available);
    }

    #endregion
}
