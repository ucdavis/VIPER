using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Tests for DashboardService course counting and NoInstructors alert logic.
/// Focused on R-course exclusion, termCode filtering, and department scoping.
/// </summary>
public sealed class DashboardServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly Mock<ITermService> _termServiceMock;
    private readonly DashboardService _service;

    private const int TermCode = 202410;
    private const int OtherTermCode = 202504;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EffortDbContext(options);

        _termServiceMock = new Mock<ITermService>();
        _termServiceMock
            .Setup(s => s.GetTermAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TermDto { TermCode = TermCode, TermName = "Fall 2024" });

        _service = new DashboardService(_context, _termServiceMock.Object);

        SeedBasicData();
    }

    private void SeedBasicData()
    {
        _context.Terms.Add(new EffortTerm { TermCode = TermCode, OpenedDate = DateTime.Now });
        _context.SaveChanges();
    }

    /// <summary>
    /// Helper to add a course with sensible defaults.
    /// Use crseNumb ending in "R" to create an R-course.
    /// </summary>
    private void AddCourse(int id, string crseNumb, string custDept = "DVM", int termCode = TermCode)
    {
        _context.Courses.Add(new EffortCourse
        {
            Id = id,
            TermCode = termCode,
            Crn = $"{10000 + id}",
            SubjCode = custDept,
            CrseNumb = crseNumb,
            SeqNumb = "01",
            Enrollment = 20,
            Units = 4,
            CustDept = custDept
        });
    }

    private void AddRecord(int id, int courseId, int personId, int termCode = TermCode)
    {
        _context.Records.Add(new EffortRecord
        {
            Id = id,
            CourseId = courseId,
            PersonId = personId,
            TermCode = termCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 40,
            Crn = "12345"
        });
    }

    private void AddPerson(int personId, string dept = "DVM", int termCode = TermCode)
    {
        _context.Persons.Add(new EffortPerson
        {
            PersonId = personId,
            TermCode = termCode,
            FirstName = $"Person{personId}",
            LastName = "Test",
            EffortDept = dept
        });
    }

    // ── GetDashboardStatsAsync — course counting ──

    [Fact]
    public async Task TotalCourses_ExcludesRCourses()
    {
        AddCourse(1, "445");   // regular → counted
        AddCourse(2, "456R");  // R-course → excluded
        AddCourse(3, "410");   // regular → counted
        await _context.SaveChangesAsync();

        var stats = await _service.GetDashboardStatsAsync(TermCode);

        Assert.Equal(2, stats.TotalCourses);
    }

    [Fact]
    public async Task CoursesWithoutInstructors_ReturnsCorrectCount()
    {
        AddCourse(1, "445");
        AddCourse(2, "410");
        AddCourse(3, "420");
        AddRecord(1, courseId: 1, personId: 100);  // course 1 has a record
        AddPerson(100);
        await _context.SaveChangesAsync();

        var stats = await _service.GetDashboardStatsAsync(TermCode);

        Assert.Equal(2, stats.CoursesWithoutInstructors);
    }

    [Fact]
    public async Task CoursesWithoutInstructors_FiltersByTermCode()
    {
        AddCourse(1, "445");
        // Record exists but for a different term — should not count
        AddRecord(1, courseId: 1, personId: 100, termCode: OtherTermCode);
        await _context.SaveChangesAsync();

        var stats = await _service.GetDashboardStatsAsync(TermCode);

        // Course 1 has no records for TermCode, so it's "without instructors"
        Assert.Equal(1, stats.CoursesWithoutInstructors);
    }

    [Fact]
    public async Task CoursesWithoutInstructors_ExcludesRCourses()
    {
        AddCourse(1, "445");   // regular, no records → counted
        AddCourse(2, "456R");  // R-course, no records → NOT counted
        await _context.SaveChangesAsync();

        var stats = await _service.GetDashboardStatsAsync(TermCode);

        // Only the regular course counts as "without instructors"
        Assert.Equal(1, stats.CoursesWithoutInstructors);
    }

    [Fact]
    public async Task Stats_WithDepartmentFilter_OnlyCountsFilteredDept()
    {
        AddCourse(1, "445", custDept: "DVM");
        AddCourse(2, "410", custDept: "VME");
        AddPerson(100, dept: "DVM");
        await _context.SaveChangesAsync();

        var stats = await _service.GetDashboardStatsAsync(TermCode, ["DVM"]);

        Assert.Equal(1, stats.TotalCourses);
        Assert.Equal(1, stats.CoursesWithoutInstructors);
    }

    [Fact]
    public async Task Stats_WithEmptyDepartmentList_ReturnsEmptyDto()
    {
        AddCourse(1, "445");
        AddPerson(100);
        await _context.SaveChangesAsync();

        var stats = await _service.GetDashboardStatsAsync(TermCode, []);

        Assert.Equal(0, stats.TotalCourses);
        Assert.Equal(0, stats.TotalInstructors);
    }

    // ── GetDataHygieneAlertsAsync — NoInstructors alerts ──

    [Fact]
    public async Task NoInstructorsAlerts_ExcludeRCourses()
    {
        AddCourse(1, "456R");  // R-course with no records → no alert
        await _context.SaveChangesAsync();

        var alerts = await _service.GetDataHygieneAlertsAsync(TermCode);

        Assert.DoesNotContain(alerts, a => a.AlertType == "NoInstructors");
    }

    [Fact]
    public async Task NoInstructorsAlerts_IncludeNonRCourses()
    {
        AddCourse(1, "445");  // regular course with no records → alert
        await _context.SaveChangesAsync();

        var alerts = await _service.GetDataHygieneAlertsAsync(TermCode);

        var noInstructorAlerts = alerts.Where(a => a.AlertType == "NoInstructors").ToList();
        Assert.Single(noInstructorAlerts);
        Assert.Equal("1", noInstructorAlerts[0].EntityId);
    }

    [Fact]
    public async Task NoInstructorsAlerts_SkipCoursesWithRecords()
    {
        AddCourse(1, "445");
        AddRecord(1, courseId: 1, personId: 100);
        AddPerson(100);
        await _context.SaveChangesAsync();

        var alerts = await _service.GetDataHygieneAlertsAsync(TermCode);

        Assert.DoesNotContain(alerts, a => a.AlertType == "NoInstructors");
    }

    [Fact]
    public async Task NoInstructorsAlerts_FiltersByTermCode()
    {
        AddCourse(1, "445");
        // Record exists but for a different term
        AddRecord(1, courseId: 1, personId: 100, termCode: OtherTermCode);
        await _context.SaveChangesAsync();

        var alerts = await _service.GetDataHygieneAlertsAsync(TermCode);

        // Course 1 has no records for TermCode → alert should fire
        Assert.Contains(alerts, a => a.AlertType == "NoInstructors" && a.EntityId == "1");
    }

    [Fact]
    public async Task NoInstructorsAlerts_WithDepartmentFilter()
    {
        AddCourse(1, "445", custDept: "DVM");  // DVM course, no records
        AddCourse(2, "410", custDept: "VME");  // VME course, no records
        await _context.SaveChangesAsync();

        var alerts = await _service.GetDataHygieneAlertsAsync(TermCode, ["DVM"]);

        var noInstructorAlerts = alerts.Where(a => a.AlertType == "NoInstructors").ToList();
        Assert.Single(noInstructorAlerts);
        Assert.Equal("DVM", noInstructorAlerts[0].DepartmentCode);
    }

    [Fact]
    public async Task Stats_NoCourses_ReturnsZeroCounts()
    {
        // No courses seeded — empty term
        var stats = await _service.GetDashboardStatsAsync(TermCode);

        Assert.Equal(0, stats.TotalCourses);
        Assert.Equal(0, stats.CoursesWithoutInstructors);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
