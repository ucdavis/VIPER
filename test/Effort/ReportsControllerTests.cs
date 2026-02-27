using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort.Controllers;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for ReportsController API endpoints.
/// </summary>
public sealed class ReportsControllerTests
{
    private readonly Mock<ITeachingActivityService> _teachingActivityServiceMock;
    private readonly Mock<IDeptSummaryService> _deptSummaryServiceMock;
    private readonly Mock<ISchoolSummaryService> _schoolSummaryServiceMock;
    private readonly Mock<IMeritReportService> _meritReportServiceMock;
    private readonly Mock<IMeritSummaryService> _meritSummaryServiceMock;
    private readonly Mock<IClinicalEffortService> _clinicalEffortServiceMock;
    private readonly Mock<IClinicalScheduleService> _clinicalScheduleServiceMock;
    private readonly Mock<IZeroEffortService> _zeroEffortServiceMock;
    private readonly Mock<IEvaluationReportService> _evaluationReportServiceMock;
    private readonly Mock<IYearStatisticsService> _yearStatisticsServiceMock;
    private readonly Mock<IMeritMultiYearService> _meritMultiYearServiceMock;
    private readonly Mock<ISabbaticalService> _sabbaticalServiceMock;
    private readonly Mock<IEffortPermissionService> _permissionServiceMock;
    private readonly Mock<ILogger<ReportsController>> _loggerMock;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _teachingActivityServiceMock = new Mock<ITeachingActivityService>();
        _deptSummaryServiceMock = new Mock<IDeptSummaryService>();
        _schoolSummaryServiceMock = new Mock<ISchoolSummaryService>();
        _meritReportServiceMock = new Mock<IMeritReportService>();
        _meritSummaryServiceMock = new Mock<IMeritSummaryService>();
        _clinicalEffortServiceMock = new Mock<IClinicalEffortService>();
        _clinicalScheduleServiceMock = new Mock<IClinicalScheduleService>();
        _zeroEffortServiceMock = new Mock<IZeroEffortService>();
        _evaluationReportServiceMock = new Mock<IEvaluationReportService>();
        _yearStatisticsServiceMock = new Mock<IYearStatisticsService>();
        _meritMultiYearServiceMock = new Mock<IMeritMultiYearService>();
        _sabbaticalServiceMock = new Mock<ISabbaticalService>();
        _permissionServiceMock = new Mock<IEffortPermissionService>();
        _loggerMock = new Mock<ILogger<ReportsController>>();

        _controller = new ReportsController(
            _teachingActivityServiceMock.Object,
            _deptSummaryServiceMock.Object,
            _schoolSummaryServiceMock.Object,
            _meritReportServiceMock.Object,
            _meritSummaryServiceMock.Object,
            _clinicalEffortServiceMock.Object,
            _clinicalScheduleServiceMock.Object,
            _zeroEffortServiceMock.Object,
            _evaluationReportServiceMock.Object,
            _yearStatisticsServiceMock.Object,
            _meritMultiYearServiceMock.Object,
            _sabbaticalServiceMock.Object,
            _permissionServiceMock.Object,
            _loggerMock.Object);

        SetupControllerContext();
    }

    private void SetupControllerContext()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            }
        };
    }

    /// <summary>
    /// Moq matcher for IReadOnlyList&lt;string&gt; containing exactly the expected departments.
    /// </summary>
    private static bool IsDepts(IReadOnlyList<string>? actual, params string[] expected) =>
        actual != null && actual.Count == expected.Length
        && expected.All(e => actual.Contains(e, StringComparer.OrdinalIgnoreCase));

    private static TeachingActivityReport CreateTestReport(int termCode = 202410, string termName = "Fall Quarter 2024")
    {
        return new TeachingActivityReport
        {
            TermCode = termCode,
            TermName = termName,
            EffortTypes = ["CLI", "LEC"],
            Departments =
            [
                new TeachingActivityDepartmentGroup
                {
                    Department = "VME",
                    Instructors =
                    [
                        new TeachingActivityInstructorGroup
                        {
                            MothraId = "A12345678",
                            Instructor = "Smith, John",
                            JobGroupId = "REG",
                            Courses =
                            [
                                new TeachingActivityCourseRow
                                {
                                    CourseId = 1,
                                    Course = "VME 400-001",
                                    Crn = "40076",
                                    Units = 4.0m,
                                    Enrollment = 25,
                                    RoleId = "Instructor",
                                    EffortByType = new Dictionary<string, decimal> { ["LEC"] = 30.0m, ["CLI"] = 10.0m }
                                }
                            ],
                            InstructorTotals = new Dictionary<string, decimal> { ["LEC"] = 30.0m, ["CLI"] = 10.0m }
                        }
                    ],
                    DepartmentTotals = new Dictionary<string, decimal> { ["LEC"] = 30.0m, ["CLI"] = 10.0m }
                }
            ]
        };
    }

    #region Validation Tests

    [Fact]
    public async Task GetTeachingActivityGrouped_ReturnsBadRequest_WhenTermCodeIsZero()
    {
        // Act
        var result = await _controller.GetTeachingActivityGrouped(0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ReturnsBadRequest_WhenTermCodeIsNegative()
    {
        // Act
        var result = await _controller.GetTeachingActivityGrouped(-1);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ReturnsBadRequest_WhenAcademicYearNotConsecutive()
    {
        // Act
        var result = await _controller.GetTeachingActivityGrouped(academicYear: "2024-2099");

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("consecutive", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ReturnsBadRequest_WhenDepartmentTooLong()
    {
        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, department: "TOOLONG7");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ReturnsBadRequest_WhenPersonIdIsZero()
    {
        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, personId: 0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ReturnsBadRequest_WhenPersonIdIsNegative()
    {
        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, personId: -5);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ReturnsBadRequest_WhenRoleTooLong()
    {
        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, role: "IF");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ReturnsBadRequest_WhenTitleTooLong()
    {
        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, title: "LONG");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityIndividual_ReturnsBadRequest_WhenTermCodeIsZero()
    {
        // Act
        var result = await _controller.GetTeachingActivityIndividual(0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityIndividual_ReturnsBadRequest_WhenDepartmentTooLong()
    {
        // Act
        var result = await _controller.GetTeachingActivityIndividual(202410, department: "TOOLONG7");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityIndividual_ReturnsBadRequest_WhenRoleTooLong()
    {
        // Act
        var result = await _controller.GetTeachingActivityIndividual(202410, role: "IF");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityIndividual_ReturnsBadRequest_WhenTitleTooLong()
    {
        // Act
        var result = await _controller.GetTeachingActivityIndividual(202410, title: "LONG");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region Success Tests (Admin User)

    [Fact]
    public async Task GetTeachingActivityGrouped_ReturnsOk_WithReport()
    {
        // Arrange
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<TeachingActivityReport>(okResult.Value);
        Assert.Equal(202410, returnedReport.TermCode);
        Assert.Single(returnedReport.Departments);
        Assert.Equal("VME", returnedReport.Departments[0].Department);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_PassesDepartmentFilter_ForAdminUser()
    {
        // Arrange
        var report = CreateTestReport();
        report.FilterDepartment = "VME";
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, department: "VME");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<TeachingActivityReport>(okResult.Value);
        Assert.Equal("VME", returnedReport.FilterDepartment);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_PassesAllFilters()
    {
        // Arrange
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), 123, "I", "REG", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, department: "VME", personId: 123, role: "I", title: "REG");

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
        _teachingActivityServiceMock.Verify(
            s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), 123, "I", "REG", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTeachingActivityIndividual_ReturnsOk_WithReport()
    {
        // Arrange
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityIndividual(202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<TeachingActivityReport>(okResult.Value);
    }

    #endregion

    #region Department Permission Filtering Tests

    [Fact]
    public async Task GetTeachingActivityGrouped_AdminUser_PassesRequestedDepartment()
    {
        // Arrange - admin has full access, no department restriction
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "APC")), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, department: "APC");

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
        _teachingActivityServiceMock.Verify(
            s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "APC")), null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ViewDeptUser_SingleDept_UsesThatDept()
    {
        // Arrange - ViewDept user with one authorized department
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act - no department filter requested
        var result = await _controller.GetTeachingActivityGrouped(202410);

        // Assert - should auto-select the single authorized dept
        Assert.IsType<OkObjectResult>(result.Result);
        _teachingActivityServiceMock.Verify(
            s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ViewDeptUser_MultipleDepts_PassesAllAuthorized()
    {
        // Arrange - ViewDept user with multiple authorized departments
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME", "APC" });
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME", "APC")), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act - no department filter requested
        var result = await _controller.GetTeachingActivityGrouped(202410);

        // Assert - should pass all authorized departments so SP filters per-dept
        Assert.IsType<OkObjectResult>(result.Result);
        _teachingActivityServiceMock.Verify(
            s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME", "APC")), null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ViewDeptUser_RequestsAuthorizedDept_AllowsIt()
    {
        // Arrange - ViewDept user requests their own authorized department
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME", "APC" });
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, department: "VME");

        // Assert - authorized dept is allowed through
        Assert.IsType<OkObjectResult>(result.Result);
        _teachingActivityServiceMock.Verify(
            s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ViewDeptUser_RequestsUnauthorizedDept_ReturnsEmptyReport()
    {
        // Arrange - ViewDept user requests a department they don't have access to
        var emptyReport = new TeachingActivityReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            EffortTypes = [],
            Departments = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => d != null && d.Count == 0), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        // Act - requests "PHR" but only authorized for "VME"
        var result = await _controller.GetTeachingActivityGrouped(202410, department: "PHR");

        // Assert - returns empty report instead of substituting a different department
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<TeachingActivityReport>(okResult.Value);
        Assert.Empty(returnedReport.Departments);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ViewDeptUser_NoDepts_ReturnsForbid()
    {
        // Arrange - ViewDept user with no authorized departments
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410);

        // Assert - should return Forbid, not leak data
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ViewDeptUser_MultipleDepts_FiltersResponseDepartments()
    {
        // Arrange - ViewDept user with VME,APC; SP returns VME,APC,PMI
        var report = new TeachingActivityReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            EffortTypes = ["LEC"],
            Departments =
            [
                new TeachingActivityDepartmentGroup { Department = "VME", Instructors = [], DepartmentTotals = new() },
                new TeachingActivityDepartmentGroup { Department = "APC", Instructors = [], DepartmentTotals = new() },
                new TeachingActivityDepartmentGroup { Department = "PMI", Instructors = [], DepartmentTotals = new() }
            ]
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME", "APC" });
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME", "APC")), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410);

        // Assert - PMI should be filtered out
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<TeachingActivityReport>(okResult.Value);
        Assert.Equal(2, returnedReport.Departments.Count);
        Assert.DoesNotContain(returnedReport.Departments, d => d.Department == "PMI");
    }

    #endregion

    #region Empty Results Tests

    [Fact]
    public async Task GetTeachingActivityGrouped_ReturnsOk_WithEmptyReport()
    {
        // Arrange
        var emptyReport = new TeachingActivityReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            EffortTypes = [],
            Departments = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<TeachingActivityReport>(okResult.Value);
        Assert.Empty(returnedReport.Departments);
        Assert.Empty(returnedReport.EffortTypes);
    }

    #endregion

    #region Test Data Helpers

    private static DeptSummaryReport CreateTestDeptSummaryReport(int termCode = 202410, string termName = "Fall Quarter 2024")
    {
        return new DeptSummaryReport
        {
            TermCode = termCode,
            TermName = termName,
            EffortTypes = ["CLI", "LEC"],
            Departments =
            [
                new DeptSummaryDepartmentGroup
                {
                    Department = "VME",
                    FacultyCount = 1,
                    FacultyWithCliCount = 1,
                    Instructors =
                    [
                        new DeptSummaryInstructorRow
                        {
                            MothraId = "A12345678",
                            Instructor = "Smith, John",
                            JobGroupId = "REG",
                            EffortByType = new Dictionary<string, decimal> { ["CLI"] = 10.0m, ["LEC"] = 30.0m }
                        }
                    ],
                    DepartmentTotals = new Dictionary<string, decimal> { ["CLI"] = 10.0m, ["LEC"] = 30.0m },
                    DepartmentAverages = new Dictionary<string, decimal> { ["CLI"] = 10.0m, ["LEC"] = 30.0m }
                }
            ]
        };
    }

    private static SchoolSummaryReport CreateTestSchoolSummaryReport(int termCode = 202410, string termName = "Fall Quarter 2024")
    {
        return new SchoolSummaryReport
        {
            TermCode = termCode,
            TermName = termName,
            EffortTypes = ["CLI", "LEC"],
            Departments =
            [
                new SchoolSummaryDepartmentRow
                {
                    Department = "VME",
                    FacultyCount = 5,
                    FacultyWithCliCount = 3,
                    EffortTotals = new Dictionary<string, decimal> { ["CLI"] = 50.0m, ["LEC"] = 120.0m },
                    Averages = new Dictionary<string, decimal> { ["CLI"] = 16.67m, ["LEC"] = 24.0m }
                },
                new SchoolSummaryDepartmentRow
                {
                    Department = "APC",
                    FacultyCount = 3,
                    FacultyWithCliCount = 2,
                    EffortTotals = new Dictionary<string, decimal> { ["CLI"] = 30.0m, ["LEC"] = 80.0m },
                    Averages = new Dictionary<string, decimal> { ["CLI"] = 15.0m, ["LEC"] = 26.67m }
                }
            ],
            GrandTotals = new SchoolSummaryTotalsRow
            {
                FacultyCount = 8,
                FacultyWithCliCount = 5,
                EffortTotals = new Dictionary<string, decimal> { ["CLI"] = 80.0m, ["LEC"] = 200.0m },
                Averages = new Dictionary<string, decimal> { ["CLI"] = 16.0m, ["LEC"] = 25.0m }
            }
        };
    }

    private static MeritDetailReport CreateTestMeritDetailReport(int termCode = 202410, string termName = "Fall Quarter 2024")
    {
        return new MeritDetailReport
        {
            TermCode = termCode,
            TermName = termName,
            EffortTypes = ["CLI", "LEC"],
            Departments =
            [
                new MeritDetailDepartmentGroup
                {
                    Department = "VME",
                    Instructors =
                    [
                        new MeritDetailInstructorGroup
                        {
                            MothraId = "A12345678",
                            Instructor = "Smith, John",
                            JobGroupId = "REG",
                            JobGroupDescription = "Regular Rank",
                            Courses =
                            [
                                new MeritDetailCourseRow
                                {
                                    TermCode = termCode,
                                    CourseId = 1,
                                    Course = "VME 400-001",
                                    Units = 4.0m,
                                    Enrollment = 25,
                                    RoleId = "I",
                                    EffortByType = new Dictionary<string, decimal> { ["CLI"] = 10.0m, ["LEC"] = 30.0m }
                                }
                            ],
                            InstructorTotals = new Dictionary<string, decimal> { ["CLI"] = 10.0m, ["LEC"] = 30.0m }
                        }
                    ],
                    DepartmentTotals = new Dictionary<string, decimal> { ["CLI"] = 10.0m, ["LEC"] = 30.0m }
                }
            ]
        };
    }

    private static MeritAverageReport CreateTestMeritAverageReport(int termCode = 202410, string termName = "Fall Quarter 2024")
    {
        return new MeritAverageReport
        {
            TermCode = termCode,
            TermName = termName,
            EffortTypes = ["CLI", "LEC"],
            JobGroups =
            [
                new MeritAverageJobGroup
                {
                    JobGroupDescription = "Regular Rank",
                    Departments =
                    [
                        new MeritAverageDepartmentGroup
                        {
                            Department = "VME",
                            FacultyCount = 2,
                            FacultyWithCliCount = 1,
                            Instructors =
                            [
                                new MeritAverageInstructorRow
                                {
                                    MothraId = "A12345678",
                                    Instructor = "Smith, John",
                                    JobGroupId = "REG",
                                    JobGroupDescription = "Regular Rank",
                                    PercentAdmin = 25.0m,
                                    EffortByType = new Dictionary<string, decimal> { ["CLI"] = 10.0m, ["LEC"] = 30.0m }
                                }
                            ],
                            GroupTotals = new Dictionary<string, decimal> { ["CLI"] = 10.0m, ["LEC"] = 30.0m },
                            GroupAverages = new Dictionary<string, decimal> { ["CLI"] = 10.0m, ["LEC"] = 15.0m }
                        }
                    ]
                }
            ]
        };
    }

    private static ZeroEffortReport CreateTestZeroEffortReport(int termCode = 202410, string termName = "Fall Quarter 2024")
    {
        return new ZeroEffortReport
        {
            TermCode = termCode,
            TermName = termName,
            Instructors =
            [
                new ZeroEffortInstructorRow
                {
                    MothraId = "A12345678",
                    Instructor = "Smith, John",
                    Department = "VME",
                    JobGroupId = "REG"
                },
                new ZeroEffortInstructorRow
                {
                    MothraId = "B98765432",
                    Instructor = "Doe, Jane",
                    Department = "APC",
                    JobGroupId = "REG"
                }
            ]
        };
    }

    #endregion

    #region Boundary Value Tests

    [Fact]
    public async Task GetTeachingActivityGrouped_AcceptsDepartmentAt6Chars()
    {
        // Arrange
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "ABCDEF")), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, department: "ABCDEF");

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_AcceptsSingleCharRole()
    {
        // Arrange
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, null, null, "I", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, role: "I");

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_AcceptsTitleAt3Chars()
    {
        // Arrange
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, null, null, null, "REG", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, title: "REG");

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_AcceptsPersonIdOf1()
    {
        // Arrange
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, null, 1, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, personId: 1);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    #endregion

    // ========================================================================
    // R2 Report Tests
    // ========================================================================

    #region Dept Summary Tests

    [Fact]
    public async Task GetDeptSummary_ReturnsBadRequest_WhenNoTermOrYear()
    {
        var result = await _controller.GetDeptSummary(0);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetDeptSummary_ReturnsBadRequest_WhenAcademicYearBadFormat()
    {
        var result = await _controller.GetDeptSummary(academicYear: "2024");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetDeptSummary_ReturnsBadRequest_WhenAcademicYearNotConsecutive()
    {
        var result = await _controller.GetDeptSummary(academicYear: "2024-2099");
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("consecutive", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task GetDeptSummary_ReturnsBadRequest_WhenDepartmentTooLong()
    {
        var result = await _controller.GetDeptSummary(202410, department: "TOOLONG7");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetDeptSummary_ReturnsBadRequest_WhenTitleTooLong()
    {
        var result = await _controller.GetDeptSummary(202410, title: "LONG");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetDeptSummary_ReturnsOk_WithReport()
    {
        var report = CreateTestDeptSummaryReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _deptSummaryServiceMock
            .Setup(s => s.GetDeptSummaryReportAsync(202410, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetDeptSummary(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<DeptSummaryReport>(okResult.Value);
        Assert.Equal(202410, returnedReport.TermCode);
        Assert.Single(returnedReport.Departments);
    }

    [Fact]
    public async Task GetDeptSummary_PassesDepartmentAndTitleFilters()
    {
        var report = CreateTestDeptSummaryReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _deptSummaryServiceMock
            .Setup(s => s.GetDeptSummaryReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, "REG", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetDeptSummary(202410, department: "VME", title: "REG");

        Assert.IsType<OkObjectResult>(result.Result);
        _deptSummaryServiceMock.Verify(
            s => s.GetDeptSummaryReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, "REG", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDeptSummary_AcademicYear_ReturnsOk()
    {
        var report = CreateTestDeptSummaryReport();
        report.AcademicYear = "2024-2025";
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _deptSummaryServiceMock
            .Setup(s => s.GetDeptSummaryReportByYearAsync("2024-2025", null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetDeptSummary(academicYear: "2024-2025");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<DeptSummaryReport>(okResult.Value);
        Assert.Equal("2024-2025", returnedReport.AcademicYear);
    }

    [Fact]
    public async Task GetDeptSummary_ViewDeptUser_NoDepts_ReturnsForbid()
    {
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var result = await _controller.GetDeptSummary(202410);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetDeptSummary_ViewDeptUser_FiltersResponseDepartments()
    {
        var report = CreateTestDeptSummaryReport();
        report.Departments.Add(new DeptSummaryDepartmentGroup
        {
            Department = "PMI",
            Instructors = [],
            DepartmentTotals = new(),
            DepartmentAverages = new()
        });
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _deptSummaryServiceMock
            .Setup(s => s.GetDeptSummaryReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetDeptSummary(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<DeptSummaryReport>(okResult.Value);
        Assert.Single(returnedReport.Departments);
        Assert.Equal("VME", returnedReport.Departments[0].Department);
    }

    [Fact]
    public async Task GetDeptSummary_ReturnsOk_WithEmptyReport()
    {
        var emptyReport = new DeptSummaryReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            EffortTypes = [],
            Departments = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _deptSummaryServiceMock
            .Setup(s => s.GetDeptSummaryReportAsync(202410, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var result = await _controller.GetDeptSummary(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<DeptSummaryReport>(okResult.Value);
        Assert.Empty(returnedReport.Departments);
    }

    #endregion

    #region School Summary Tests

    [Fact]
    public async Task GetSchoolSummary_ReturnsBadRequest_WhenNoTermOrYear()
    {
        var result = await _controller.GetSchoolSummary(0);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSchoolSummary_ReturnsBadRequest_WhenAcademicYearBadFormat()
    {
        var result = await _controller.GetSchoolSummary(academicYear: "2024");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSchoolSummary_ReturnsBadRequest_WhenAcademicYearNotConsecutive()
    {
        var result = await _controller.GetSchoolSummary(academicYear: "2024-2099");
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("consecutive", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task GetSchoolSummary_ReturnsOk_WithReport()
    {
        var report = CreateTestSchoolSummaryReport();
        _permissionServiceMock.Setup(s => s.HasPermissionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _schoolSummaryServiceMock
            .Setup(s => s.GetSchoolSummaryReportAsync(202410, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetSchoolSummary(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<SchoolSummaryReport>(okResult.Value);
        Assert.Equal(2, returnedReport.Departments.Count);
        Assert.NotNull(returnedReport.GrandTotals);
    }

    [Fact]
    public async Task GetSchoolSummary_AcademicYear_ReturnsOk()
    {
        var report = CreateTestSchoolSummaryReport();
        report.AcademicYear = "2024-2025";
        _permissionServiceMock.Setup(s => s.HasPermissionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _schoolSummaryServiceMock
            .Setup(s => s.GetSchoolSummaryReportByYearAsync("2024-2025", null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetSchoolSummary(academicYear: "2024-2025");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<SchoolSummaryReport>(okResult.Value);
        Assert.Equal("2024-2025", returnedReport.AcademicYear);
    }

    [Fact]
    public async Task GetSchoolSummary_SchoolSummaryPermission_BypassesDeptFilter()
    {
        // Arrange - user has SchoolSummary but NOT ViewAllDepartments or ViewDept
        var report = CreateTestSchoolSummaryReport();
        _permissionServiceMock.Setup(s => s.HasPermissionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _schoolSummaryServiceMock
            .Setup(s => s.GetSchoolSummaryReportAsync(202410, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetSchoolSummary(202410);

        // Assert - all departments returned, no filtering applied
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<SchoolSummaryReport>(okResult.Value);
        Assert.Equal(2, returnedReport.Departments.Count);
    }

    [Fact]
    public async Task GetSchoolSummary_ViewDeptUser_WithoutSchoolSummary_FiltersResponseDepartments()
    {
        var report = CreateTestSchoolSummaryReport();
        _permissionServiceMock.Setup(s => s.HasPermissionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _schoolSummaryServiceMock
            .Setup(s => s.GetSchoolSummaryReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetSchoolSummary(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<SchoolSummaryReport>(okResult.Value);
        Assert.Single(returnedReport.Departments);
        Assert.Equal("VME", returnedReport.Departments[0].Department);
    }

    [Fact]
    public async Task GetSchoolSummary_PassesAllFilters()
    {
        var report = CreateTestSchoolSummaryReport();
        _permissionServiceMock.Setup(s => s.HasPermissionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _schoolSummaryServiceMock
            .Setup(s => s.GetSchoolSummaryReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), 123, "1", "REG", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetSchoolSummary(202410, department: "VME", personId: 123, role: "1", title: "REG");

        Assert.IsType<OkObjectResult>(result.Result);
        _schoolSummaryServiceMock.Verify(
            s => s.GetSchoolSummaryReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), 123, "1", "REG", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSchoolSummary_ReturnsOk_WithEmptyReport()
    {
        var emptyReport = new SchoolSummaryReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            EffortTypes = [],
            Departments = [],
            GrandTotals = new SchoolSummaryTotalsRow
            {
                EffortTotals = new(),
                Averages = new()
            }
        };
        _permissionServiceMock.Setup(s => s.HasPermissionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _schoolSummaryServiceMock
            .Setup(s => s.GetSchoolSummaryReportAsync(202410, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var result = await _controller.GetSchoolSummary(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<SchoolSummaryReport>(okResult.Value);
        Assert.Empty(returnedReport.Departments);
    }

    #endregion

    #region Merit Detail Tests

    [Fact]
    public async Task GetMeritDetail_ReturnsBadRequest_WhenNoTermOrYear()
    {
        var result = await _controller.GetMeritDetail(0);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritDetail_ReturnsBadRequest_WhenAcademicYearNotConsecutive()
    {
        var result = await _controller.GetMeritDetail(academicYear: "2024-2099");
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("consecutive", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task GetMeritDetail_ReturnsBadRequest_WhenDepartmentTooLong()
    {
        var result = await _controller.GetMeritDetail(202410, department: "TOOLONG7");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritDetail_ReturnsBadRequest_WhenPersonIdIsZero()
    {
        var result = await _controller.GetMeritDetail(202410, personId: 0);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritDetail_ReturnsBadRequest_WhenPersonIdIsNegative()
    {
        var result = await _controller.GetMeritDetail(202410, personId: -5);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritDetail_ReturnsBadRequest_WhenRoleTooLong()
    {
        var result = await _controller.GetMeritDetail(202410, role: "IF");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritDetail_ReturnsOk_WhenPersonIdOmitted()
    {
        var report = CreateTestMeritDetailReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritReportServiceMock
            .Setup(s => s.GetMeritDetailReportAsync(202410, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritDetail(202410, personId: null);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritDetail_ReturnsOk_WithReport()
    {
        var report = CreateTestMeritDetailReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritReportServiceMock
            .Setup(s => s.GetMeritDetailReportAsync(202410, null, 100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritDetail(202410, personId: 100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritDetailReport>(okResult.Value);
        Assert.Equal(202410, returnedReport.TermCode);
        Assert.Single(returnedReport.Departments);
    }

    [Fact]
    public async Task GetMeritDetail_PassesAllFilters()
    {
        var report = CreateTestMeritDetailReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritReportServiceMock
            .Setup(s => s.GetMeritDetailReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), 123, "I", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritDetail(202410, department: "VME", personId: 123, role: "I");

        Assert.IsType<OkObjectResult>(result.Result);
        _meritReportServiceMock.Verify(
            s => s.GetMeritDetailReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), 123, "I", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMeritDetail_AcademicYear_ReturnsOk()
    {
        var report = CreateTestMeritDetailReport();
        report.AcademicYear = "2024-2025";
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritReportServiceMock
            .Setup(s => s.GetMeritDetailReportByYearAsync("2024-2025", null, 100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritDetail(academicYear: "2024-2025", personId: 100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritDetailReport>(okResult.Value);
        Assert.Equal("2024-2025", returnedReport.AcademicYear);
    }

    [Fact]
    public async Task GetMeritDetail_ViewDeptUser_NoDepts_ReturnsForbid()
    {
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var result = await _controller.GetMeritDetail(202410, personId: 100);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritDetail_ViewDeptUser_FiltersResponseDepartments()
    {
        var report = CreateTestMeritDetailReport();
        report.Departments.Add(new MeritDetailDepartmentGroup
        {
            Department = "PMI",
            Instructors = [],
            DepartmentTotals = new()
        });
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _meritReportServiceMock
            .Setup(s => s.GetMeritDetailReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), 100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritDetail(202410, personId: 100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritDetailReport>(okResult.Value);
        Assert.Single(returnedReport.Departments);
        Assert.DoesNotContain(returnedReport.Departments, d => d.Department == "PMI");
    }

    [Fact]
    public async Task GetMeritDetail_ReturnsOk_WithEmptyReport()
    {
        var emptyReport = new MeritDetailReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            EffortTypes = [],
            Departments = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritReportServiceMock
            .Setup(s => s.GetMeritDetailReportAsync(202410, null, 100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var result = await _controller.GetMeritDetail(202410, personId: 100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritDetailReport>(okResult.Value);
        Assert.Empty(returnedReport.Departments);
    }

    #endregion

    #region Merit Average Tests

    [Fact]
    public async Task GetMeritAverage_ReturnsBadRequest_WhenNoTermOrYear()
    {
        var result = await _controller.GetMeritAverage(0);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritAverage_ReturnsBadRequest_WhenAcademicYearNotConsecutive()
    {
        var result = await _controller.GetMeritAverage(academicYear: "2024-2099");
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("consecutive", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task GetMeritAverage_ReturnsBadRequest_WhenDepartmentTooLong()
    {
        var result = await _controller.GetMeritAverage(202410, department: "TOOLONG7");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritAverage_ReturnsBadRequest_WhenPersonIdIsZero()
    {
        var result = await _controller.GetMeritAverage(202410, personId: 0);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritAverage_ReturnsBadRequest_WhenPersonIdIsNegative()
    {
        var result = await _controller.GetMeritAverage(202410, personId: -5);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritAverage_ReturnsOk_WithReport()
    {
        var report = CreateTestMeritAverageReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritReportServiceMock
            .Setup(s => s.GetMeritAverageReportAsync(202410, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritAverage(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritAverageReport>(okResult.Value);
        Assert.Equal(202410, returnedReport.TermCode);
        Assert.Single(returnedReport.JobGroups);
    }

    [Fact]
    public async Task GetMeritAverage_PassesDepartmentAndPersonFilters()
    {
        var report = CreateTestMeritAverageReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritReportServiceMock
            .Setup(s => s.GetMeritAverageReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritAverage(202410, department: "VME", personId: 123);

        Assert.IsType<OkObjectResult>(result.Result);
        _meritReportServiceMock.Verify(
            s => s.GetMeritAverageReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), 123, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMeritAverage_AcademicYear_ReturnsOk()
    {
        var report = CreateTestMeritAverageReport();
        report.AcademicYear = "2024-2025";
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritReportServiceMock
            .Setup(s => s.GetMeritAverageReportByYearAsync("2024-2025", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritAverage(academicYear: "2024-2025");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritAverageReport>(okResult.Value);
        Assert.Equal("2024-2025", returnedReport.AcademicYear);
    }

    [Fact]
    public async Task GetMeritAverage_ViewDeptUser_NoDepts_ReturnsForbid()
    {
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var result = await _controller.GetMeritAverage(202410);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritAverage_ViewDeptUser_FiltersJobGroupDepartments()
    {
        // Arrange - report has VME and PMI departments within a job group
        var report = CreateTestMeritAverageReport();
        report.JobGroups[0].Departments.Add(new MeritAverageDepartmentGroup
        {
            Department = "PMI",
            FacultyCount = 1,
            FacultyWithCliCount = 0,
            Instructors = [],
            GroupTotals = new(),
            GroupAverages = new()
        });
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _meritReportServiceMock
            .Setup(s => s.GetMeritAverageReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritAverage(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritAverageReport>(okResult.Value);
        Assert.Single(returnedReport.JobGroups);
        Assert.Single(returnedReport.JobGroups[0].Departments);
        Assert.Equal("VME", returnedReport.JobGroups[0].Departments[0].Department);
    }

    [Fact]
    public async Task GetMeritAverage_ViewDeptUser_RemovesEmptyJobGroups()
    {
        // Arrange - report has two job groups; after filtering, one has no departments
        var report = CreateTestMeritAverageReport();
        report.JobGroups.Add(new MeritAverageJobGroup
        {
            JobGroupDescription = "Federation",
            Departments =
            [
                new MeritAverageDepartmentGroup
                {
                    Department = "PMI",
                    FacultyCount = 1,
                    FacultyWithCliCount = 0,
                    Instructors = [],
                    GroupTotals = new(),
                    GroupAverages = new()
                }
            ]
        });
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _meritReportServiceMock
            .Setup(s => s.GetMeritAverageReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritAverage(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritAverageReport>(okResult.Value);
        // "Federation" job group should be removed since PMI is unauthorized
        Assert.Single(returnedReport.JobGroups);
        Assert.Equal("Regular Rank", returnedReport.JobGroups[0].JobGroupDescription);
    }

    [Fact]
    public async Task GetMeritAverage_ReturnsOk_WithEmptyReport()
    {
        var emptyReport = new MeritAverageReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            EffortTypes = [],
            JobGroups = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritReportServiceMock
            .Setup(s => s.GetMeritAverageReportAsync(202410, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var result = await _controller.GetMeritAverage(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritAverageReport>(okResult.Value);
        Assert.Empty(returnedReport.JobGroups);
    }

    #endregion

    #region Zero Effort Tests

    [Fact]
    public async Task GetZeroEffort_ReturnsBadRequest_WhenNoTermOrYear()
    {
        var result = await _controller.GetZeroEffort(0, null);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetZeroEffort_ReturnsBadRequest_WhenTermCodeIsNegative()
    {
        var result = await _controller.GetZeroEffort(-1, null);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetZeroEffort_ReturnsBadRequest_WhenDepartmentTooLong()
    {
        var result = await _controller.GetZeroEffort(202410, department: "TOOLONG7");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetZeroEffort_ReturnsBadRequest_WhenAcademicYearFormatInvalid()
    {
        var result = await _controller.GetZeroEffort(0, academicYear: "2024");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetZeroEffort_ReturnsOk_WithReport()
    {
        var report = CreateTestZeroEffortReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _zeroEffortServiceMock
            .Setup(s => s.GetZeroEffortReportAsync(202410, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetZeroEffort(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ZeroEffortReport>(okResult.Value);
        Assert.Equal(202410, returnedReport.TermCode);
        Assert.Equal(2, returnedReport.Instructors.Count);
    }

    [Fact]
    public async Task GetZeroEffort_ReturnsOk_WithAcademicYear()
    {
        var report = CreateTestZeroEffortReport(termName: "2024-2025");
        report.AcademicYear = "2024-2025";
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _zeroEffortServiceMock
            .Setup(s => s.GetZeroEffortReportByYearAsync("2024-2025", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetZeroEffort(0, academicYear: "2024-2025");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ZeroEffortReport>(okResult.Value);
        Assert.Equal("2024-2025", returnedReport.AcademicYear);
        Assert.Equal(2, returnedReport.Instructors.Count);
    }

    [Fact]
    public async Task GetZeroEffort_PassesDepartmentFilter()
    {
        var report = CreateTestZeroEffortReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _zeroEffortServiceMock
            .Setup(s => s.GetZeroEffortReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetZeroEffort(202410, department: "VME");

        Assert.IsType<OkObjectResult>(result.Result);
        _zeroEffortServiceMock.Verify(
            s => s.GetZeroEffortReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetZeroEffort_ViewDeptUser_SingleDept_UsesThatDept()
    {
        var report = CreateTestZeroEffortReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _zeroEffortServiceMock
            .Setup(s => s.GetZeroEffortReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetZeroEffort(202410);

        Assert.IsType<OkObjectResult>(result.Result);
        _zeroEffortServiceMock.Verify(
            s => s.GetZeroEffortReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetZeroEffort_ViewDeptUser_NoDepts_ReturnsForbid()
    {
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var result = await _controller.GetZeroEffort(202410);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetZeroEffort_ViewDeptUser_FiltersInstructorsByDepartment()
    {
        var report = CreateTestZeroEffortReport(); // has VME and APC instructors
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _zeroEffortServiceMock
            .Setup(s => s.GetZeroEffortReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetZeroEffort(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ZeroEffortReport>(okResult.Value);
        Assert.Single(returnedReport.Instructors);
        Assert.Equal("VME", returnedReport.Instructors[0].Department);
    }

    [Fact]
    public async Task GetZeroEffort_ReturnsOk_WithEmptyReport()
    {
        var emptyReport = new ZeroEffortReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            Instructors = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _zeroEffortServiceMock
            .Setup(s => s.GetZeroEffortReportAsync(202410, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var result = await _controller.GetZeroEffort(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ZeroEffortReport>(okResult.Value);
        Assert.Empty(returnedReport.Instructors);
    }

    #endregion

    // ========================================================================
    // R3 Report Tests
    // ========================================================================

    #region Test Data Helpers (R3)

    private static MeritSummaryReport CreateTestMeritSummaryReport(int termCode = 202410, string termName = "Fall Quarter 2024")
    {
        return new MeritSummaryReport
        {
            TermCode = termCode,
            TermName = termName,
            EffortTypes = ["CLI", "LEC"],
            JobGroups =
            [
                new MeritSummaryJobGroup
                {
                    JobGroupDescription = "Regular Rank",
                    Departments =
                    [
                        new MeritSummaryDepartmentGroup
                        {
                            Department = "VME",
                            FacultyCount = 3,
                            FacultyWithCliCount = 2,
                            DepartmentTotals = new Dictionary<string, decimal> { ["CLI"] = 30.0m, ["LEC"] = 90.0m },
                            DepartmentAverages = new Dictionary<string, decimal> { ["CLI"] = 15.0m, ["LEC"] = 30.0m }
                        }
                    ]
                }
            ]
        };
    }

    private static ClinicalEffortReport CreateTestClinicalEffortReport(
        string academicYear = "2024-2025", int clinicalType = 1, string clinicalTypeName = "VMTH")
    {
        return new ClinicalEffortReport
        {
            TermName = academicYear,
            AcademicYear = academicYear,
            ClinicalType = clinicalType,
            ClinicalTypeName = clinicalTypeName,
            EffortTypes = ["CLI", "LEC"],
            JobGroups =
            [
                new ClinicalEffortJobGroup
                {
                    JobGroupDescription = "Regular Rank",
                    Instructors =
                    [
                        new ClinicalEffortInstructorRow
                        {
                            MothraId = "A12345678",
                            Instructor = "Smith, John",
                            Department = "VME",
                            ClinicalPercent = 50.0m,
                            EffortByType = new Dictionary<string, decimal> { ["CLI"] = 10.0m, ["LEC"] = 20.0m },
                            CliRatio = 0.2m
                        }
                    ]
                }
            ]
        };
    }

    private static ScheduledCliWeeksReport CreateTestScheduledCliWeeksReport(
        int termCode = 202410, string termName = "Fall Quarter 2024")
    {
        return new ScheduledCliWeeksReport
        {
            TermName = termName,
            TermNames = ["Fall Quarter 2024"],
            Services = ["SA", "LA"],
            Instructors =
            [
                new ScheduledCliWeeksInstructorRow
                {
                    MothraId = "A12345678",
                    Instructor = "Smith, John",
                    TotalWeeks = 8,
                    Terms =
                    [
                        new ScheduledCliWeeksTermRow
                        {
                            TermCode = termCode,
                            TermName = termName,
                            WeeksByService = new Dictionary<string, int> { ["SA"] = 5, ["LA"] = 3 },
                            TermTotal = 8
                        }
                    ]
                }
            ]
        };
    }

    #endregion

    #region Merit Summary Tests

    [Fact]
    public async Task GetMeritSummary_ReturnsBadRequest_WhenNoTermOrYear()
    {
        var result = await _controller.GetMeritSummary(0);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritSummary_ReturnsBadRequest_WhenAcademicYearBadFormat()
    {
        var result = await _controller.GetMeritSummary(academicYear: "2024");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritSummary_ReturnsBadRequest_WhenAcademicYearNotConsecutive()
    {
        var result = await _controller.GetMeritSummary(academicYear: "2024-2099");
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("consecutive", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task GetMeritSummary_ReturnsBadRequest_WhenDepartmentTooLong()
    {
        var result = await _controller.GetMeritSummary(202410, department: "TOOLONG7");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritSummary_ReturnsOk_WithReport()
    {
        var report = CreateTestMeritSummaryReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritSummaryServiceMock
            .Setup(s => s.GetMeritSummaryReportAsync(202410, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritSummary(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritSummaryReport>(okResult.Value);
        Assert.Equal(202410, returnedReport.TermCode);
        Assert.Single(returnedReport.JobGroups);
    }

    [Fact]
    public async Task GetMeritSummary_PassesDepartmentFilter()
    {
        var report = CreateTestMeritSummaryReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritSummaryServiceMock
            .Setup(s => s.GetMeritSummaryReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritSummary(202410, department: "VME");

        Assert.IsType<OkObjectResult>(result.Result);
        _meritSummaryServiceMock.Verify(
            s => s.GetMeritSummaryReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMeritSummary_AcademicYear_ReturnsOk()
    {
        var report = CreateTestMeritSummaryReport();
        report.AcademicYear = "2024-2025";
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritSummaryServiceMock
            .Setup(s => s.GetMeritSummaryReportByYearAsync("2024-2025", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritSummary(academicYear: "2024-2025");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritSummaryReport>(okResult.Value);
        Assert.Equal("2024-2025", returnedReport.AcademicYear);
    }

    [Fact]
    public async Task GetMeritSummary_ViewDeptUser_NoDepts_ReturnsForbid()
    {
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var result = await _controller.GetMeritSummary(202410);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritSummary_ViewDeptUser_FiltersJobGroupDepartments()
    {
        var report = CreateTestMeritSummaryReport();
        report.JobGroups[0].Departments.Add(new MeritSummaryDepartmentGroup
        {
            Department = "PMI",
            FacultyCount = 1,
            FacultyWithCliCount = 0,
            DepartmentTotals = new(),
            DepartmentAverages = new()
        });
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _meritSummaryServiceMock
            .Setup(s => s.GetMeritSummaryReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritSummary(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritSummaryReport>(okResult.Value);
        Assert.Single(returnedReport.JobGroups);
        Assert.Single(returnedReport.JobGroups[0].Departments);
        Assert.Equal("VME", returnedReport.JobGroups[0].Departments[0].Department);
    }

    [Fact]
    public async Task GetMeritSummary_ViewDeptUser_RemovesEmptyJobGroups()
    {
        var report = CreateTestMeritSummaryReport();
        report.JobGroups.Add(new MeritSummaryJobGroup
        {
            JobGroupDescription = "Federation",
            Departments =
            [
                new MeritSummaryDepartmentGroup
                {
                    Department = "PMI",
                    FacultyCount = 1,
                    FacultyWithCliCount = 0,
                    DepartmentTotals = new(),
                    DepartmentAverages = new()
                }
            ]
        });
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _meritSummaryServiceMock
            .Setup(s => s.GetMeritSummaryReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritSummary(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritSummaryReport>(okResult.Value);
        Assert.Single(returnedReport.JobGroups);
        Assert.Equal("Regular Rank", returnedReport.JobGroups[0].JobGroupDescription);
    }

    [Fact]
    public async Task GetMeritSummary_ReturnsOk_WithEmptyReport()
    {
        var emptyReport = new MeritSummaryReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            EffortTypes = [],
            JobGroups = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritSummaryServiceMock
            .Setup(s => s.GetMeritSummaryReportAsync(202410, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var result = await _controller.GetMeritSummary(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MeritSummaryReport>(okResult.Value);
        Assert.Empty(returnedReport.JobGroups);
    }

    #endregion

    #region Clinical Effort Tests

    [Fact]
    public async Task GetClinicalEffort_ReturnsBadRequest_WhenAcademicYearMissing()
    {
        var result = await _controller.GetClinicalEffort(academicYear: null, clinicalType: 1);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetClinicalEffort_ReturnsBadRequest_WhenAcademicYearBadFormat()
    {
        var result = await _controller.GetClinicalEffort(academicYear: "2024", clinicalType: 1);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetClinicalEffort_ReturnsBadRequest_WhenClinicalTypeInvalid()
    {
        var result = await _controller.GetClinicalEffort(academicYear: "2024-2025", clinicalType: 99);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("clinicalType", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task GetClinicalEffort_ReturnsOk_WithVmthReport()
    {
        var report = CreateTestClinicalEffortReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _clinicalEffortServiceMock
            .Setup(s => s.GetClinicalEffortReportAsync("2024-2025", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetClinicalEffort(academicYear: "2024-2025", clinicalType: 1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ClinicalEffortReport>(okResult.Value);
        Assert.Equal("2024-2025", returnedReport.AcademicYear);
        Assert.Equal(1, returnedReport.ClinicalType);
        Assert.Single(returnedReport.JobGroups);
    }

    [Fact]
    public async Task GetClinicalEffort_ReturnsOk_WithCahfsReport()
    {
        var report = CreateTestClinicalEffortReport(clinicalType: 25, clinicalTypeName: "CAHFS");
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _clinicalEffortServiceMock
            .Setup(s => s.GetClinicalEffortReportAsync("2024-2025", 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetClinicalEffort(academicYear: "2024-2025", clinicalType: 25);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ClinicalEffortReport>(okResult.Value);
        Assert.Equal(25, returnedReport.ClinicalType);
    }

    [Fact]
    public async Task GetClinicalEffort_ViewDeptUser_NoDepts_ReturnsForbid()
    {
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var result = await _controller.GetClinicalEffort(academicYear: "2024-2025", clinicalType: 1);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetClinicalEffort_ViewDeptUser_FiltersInstructorsByDepartment()
    {
        var report = CreateTestClinicalEffortReport();
        report.JobGroups[0].Instructors.Add(new ClinicalEffortInstructorRow
        {
            MothraId = "B98765432",
            Instructor = "Doe, Jane",
            Department = "PMI",
            ClinicalPercent = 40.0m,
            EffortByType = new Dictionary<string, decimal> { ["CLI"] = 8.0m },
            CliRatio = 0.2m
        });
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _clinicalEffortServiceMock
            .Setup(s => s.GetClinicalEffortReportAsync("2024-2025", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetClinicalEffort(academicYear: "2024-2025", clinicalType: 1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ClinicalEffortReport>(okResult.Value);
        Assert.Single(returnedReport.JobGroups);
        Assert.Single(returnedReport.JobGroups[0].Instructors);
        Assert.Equal("VME", returnedReport.JobGroups[0].Instructors[0].Department);
    }

    [Fact]
    public async Task GetClinicalEffort_ViewDeptUser_RemovesEmptyJobGroups()
    {
        var report = CreateTestClinicalEffortReport();
        report.JobGroups.Add(new ClinicalEffortJobGroup
        {
            JobGroupDescription = "Federation",
            Instructors =
            [
                new ClinicalEffortInstructorRow
                {
                    MothraId = "C11111111",
                    Instructor = "Wilson, Pat",
                    Department = "PMI",
                    ClinicalPercent = 30.0m,
                    EffortByType = new Dictionary<string, decimal> { ["CLI"] = 5.0m },
                    CliRatio = 0.17m
                }
            ]
        });
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _clinicalEffortServiceMock
            .Setup(s => s.GetClinicalEffortReportAsync("2024-2025", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetClinicalEffort(academicYear: "2024-2025", clinicalType: 1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ClinicalEffortReport>(okResult.Value);
        Assert.Single(returnedReport.JobGroups);
        Assert.Equal("Regular Rank", returnedReport.JobGroups[0].JobGroupDescription);
    }

    [Fact]
    public async Task GetClinicalEffort_ReturnsOk_WithEmptyReport()
    {
        var emptyReport = new ClinicalEffortReport
        {
            TermName = "2024-2025",
            AcademicYear = "2024-2025",
            ClinicalType = 1,
            ClinicalTypeName = "VMTH",
            EffortTypes = [],
            JobGroups = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _clinicalEffortServiceMock
            .Setup(s => s.GetClinicalEffortReportAsync("2024-2025", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var result = await _controller.GetClinicalEffort(academicYear: "2024-2025", clinicalType: 1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ClinicalEffortReport>(okResult.Value);
        Assert.Empty(returnedReport.JobGroups);
    }

    #endregion

    #region Scheduled CLI Weeks Tests

    [Fact]
    public async Task GetScheduledCliWeeks_ReturnsBadRequest_WhenNoTermOrYear()
    {
        var result = await _controller.GetScheduledCliWeeks(0);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetScheduledCliWeeks_ReturnsBadRequest_WhenAcademicYearBadFormat()
    {
        var result = await _controller.GetScheduledCliWeeks(academicYear: "2024");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetScheduledCliWeeks_ReturnsBadRequest_WhenAcademicYearNotConsecutive()
    {
        var result = await _controller.GetScheduledCliWeeks(academicYear: "2024-2099");
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("consecutive", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task GetScheduledCliWeeks_ReturnsForbid_WhenNotFullAccess()
    {
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _controller.GetScheduledCliWeeks(202410);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetScheduledCliWeeks_ReturnsOk_WithReport()
    {
        var report = CreateTestScheduledCliWeeksReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _clinicalScheduleServiceMock
            .Setup(s => s.GetScheduledCliWeeksReportAsync(202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetScheduledCliWeeks(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ScheduledCliWeeksReport>(okResult.Value);
        Assert.Single(returnedReport.Instructors);
        Assert.Equal(8, returnedReport.Instructors[0].TotalWeeks);
    }

    [Fact]
    public async Task GetScheduledCliWeeks_AcademicYear_ReturnsOk()
    {
        var report = CreateTestScheduledCliWeeksReport();
        report.AcademicYear = "2024-2025";
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _clinicalScheduleServiceMock
            .Setup(s => s.GetScheduledCliWeeksReportByYearAsync("2024-2025", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetScheduledCliWeeks(academicYear: "2024-2025");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ScheduledCliWeeksReport>(okResult.Value);
        Assert.Equal("2024-2025", returnedReport.AcademicYear);
    }

    [Fact]
    public async Task GetScheduledCliWeeks_ReturnsOk_WithEmptyReport()
    {
        var emptyReport = new ScheduledCliWeeksReport
        {
            TermName = "Fall Quarter 2024",
            TermNames = [],
            Services = [],
            Instructors = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _clinicalScheduleServiceMock
            .Setup(s => s.GetScheduledCliWeeksReportAsync(202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var result = await _controller.GetScheduledCliWeeks(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ScheduledCliWeeksReport>(okResult.Value);
        Assert.Empty(returnedReport.Instructors);
    }

    #endregion

    #region PDF Export Tests (R3)

    [Fact]
    public async Task ExportMeritSummaryPdf_ReturnsBadRequest_WhenNoTermOrYear()
    {
        var request = new ReportPdfRequest(TermCode: 0);
        var result = await _controller.ExportMeritSummaryPdf(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExportMeritSummaryPdf_ReturnsNoContent_WhenEmpty()
    {
        var emptyReport = new MeritSummaryReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            EffortTypes = [],
            JobGroups = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritSummaryServiceMock
            .Setup(s => s.GetMeritSummaryReportAsync(202410, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var request = new ReportPdfRequest(TermCode: 202410);
        var result = await _controller.ExportMeritSummaryPdf(request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ExportMeritSummaryPdf_ReturnsFile_WithData()
    {
        var report = CreateTestMeritSummaryReport();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritSummaryServiceMock
            .Setup(s => s.GetMeritSummaryReportAsync(202410, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        _meritSummaryServiceMock
            .Setup(s => s.GenerateReportPdfAsync(report))
            .ReturnsAsync(pdfBytes);

        var request = new ReportPdfRequest(TermCode: 202410);
        var result = await _controller.ExportMeritSummaryPdf(request);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal(pdfBytes, fileResult.FileContents);
    }

    [Fact]
    public async Task ExportClinicalEffortPdf_ReturnsBadRequest_WhenAcademicYearMissing()
    {
        var request = new ClinicalEffortPdfRequest(AcademicYear: null, ClinicalType: 1);
        var result = await _controller.ExportClinicalEffortPdf(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExportClinicalEffortPdf_ReturnsBadRequest_WhenClinicalTypeInvalid()
    {
        var request = new ClinicalEffortPdfRequest(AcademicYear: "2024-2025", ClinicalType: 99);
        var result = await _controller.ExportClinicalEffortPdf(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExportClinicalEffortPdf_ReturnsNoContent_WhenEmpty()
    {
        var emptyReport = new ClinicalEffortReport
        {
            TermName = "2024-2025",
            AcademicYear = "2024-2025",
            ClinicalType = 1,
            ClinicalTypeName = "VMTH",
            EffortTypes = [],
            JobGroups = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _clinicalEffortServiceMock
            .Setup(s => s.GetClinicalEffortReportAsync("2024-2025", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var request = new ClinicalEffortPdfRequest(AcademicYear: "2024-2025", ClinicalType: 1);
        var result = await _controller.ExportClinicalEffortPdf(request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ExportClinicalEffortPdf_ReturnsFile_WithData()
    {
        var report = CreateTestClinicalEffortReport();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _clinicalEffortServiceMock
            .Setup(s => s.GetClinicalEffortReportAsync("2024-2025", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        _clinicalEffortServiceMock
            .Setup(s => s.GenerateReportPdfAsync(report))
            .ReturnsAsync(pdfBytes);

        var request = new ClinicalEffortPdfRequest(AcademicYear: "2024-2025", ClinicalType: 1);
        var result = await _controller.ExportClinicalEffortPdf(request);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
    }

    [Fact]
    public async Task ExportScheduledCliWeeksPdf_ReturnsBadRequest_WhenNoTermOrYear()
    {
        var request = new ReportPdfRequest(TermCode: 0);
        var result = await _controller.ExportScheduledCliWeeksPdf(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExportScheduledCliWeeksPdf_ReturnsForbid_WhenNotFullAccess()
    {
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new ReportPdfRequest(TermCode: 202410);
        var result = await _controller.ExportScheduledCliWeeksPdf(request);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ExportScheduledCliWeeksPdf_ReturnsNoContent_WhenEmpty()
    {
        var emptyReport = new ScheduledCliWeeksReport
        {
            TermName = "Fall Quarter 2024",
            TermNames = [],
            Services = [],
            Instructors = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _clinicalScheduleServiceMock
            .Setup(s => s.GetScheduledCliWeeksReportAsync(202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var request = new ReportPdfRequest(TermCode: 202410);
        var result = await _controller.ExportScheduledCliWeeksPdf(request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ExportScheduledCliWeeksPdf_ReturnsFile_WithData()
    {
        var report = CreateTestScheduledCliWeeksReport();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _clinicalScheduleServiceMock
            .Setup(s => s.GetScheduledCliWeeksReportAsync(202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        _clinicalScheduleServiceMock
            .Setup(s => s.GenerateReportPdfAsync(report))
            .ReturnsAsync(pdfBytes);

        var request = new ReportPdfRequest(TermCode: 202410);
        var result = await _controller.ExportScheduledCliWeeksPdf(request);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
    }

    #endregion

    // ========================================================================
    // R4 Report Tests
    // ========================================================================

    #region Test Data Helpers (R4)

    private static EvalSummaryReport CreateTestEvalSummaryReport(int termCode = 202410, string termName = "Fall Quarter 2024")
    {
        return new EvalSummaryReport
        {
            TermCode = termCode,
            TermName = termName,
            Departments =
            [
                new EvalDepartmentGroup
                {
                    Department = "VME",
                    DepartmentAverage = 4.25m,
                    TotalResponses = 50,
                    Instructors =
                    [
                        new EvalInstructorSummary
                        {
                            MothraId = "A12345678",
                            Instructor = "Smith, John",
                            WeightedAverage = 4.25m,
                            TotalResponses = 50,
                            TotalEnrolled = 75
                        }
                    ]
                }
            ]
        };
    }

    private static EvalDetailReport CreateTestEvalDetailReport(int termCode = 202410, string termName = "Fall Quarter 2024")
    {
        return new EvalDetailReport
        {
            TermCode = termCode,
            TermName = termName,
            Departments =
            [
                new EvalDetailDepartmentGroup
                {
                    Department = "VME",
                    DepartmentAverage = 4.25m,
                    Instructors =
                    [
                        new EvalDetailInstructor
                        {
                            MothraId = "A12345678",
                            Instructor = "Smith, John",
                            InstructorAverage = 4.25m,
                            InstructorMedian = 5m,
                            Courses =
                            [
                                new EvalCourseDetail
                                {
                                    Course = "VME 400 (Small Animal Medicine)",
                                    Crn = "40076",
                                    TermCode = termCode,
                                    Role = "I",
                                    Average = 4.25m,
                                    Median = 5m,
                                    NumResponses = 50,
                                    NumEnrolled = 75
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    #endregion

    #region Eval Summary Tests

    [Fact]
    public async Task GetEvalSummary_ReturnsBadRequest_WhenNoTermOrYear()
    {
        var result = await _controller.GetEvalSummary(0);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetEvalSummary_ReturnsBadRequest_WhenAcademicYearBadFormat()
    {
        var result = await _controller.GetEvalSummary(academicYear: "2024");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetEvalSummary_ReturnsBadRequest_WhenAcademicYearNotConsecutive()
    {
        var result = await _controller.GetEvalSummary(academicYear: "2024-2099");
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("consecutive", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task GetEvalSummary_ReturnsBadRequest_WhenDepartmentTooLong()
    {
        var result = await _controller.GetEvalSummary(202410, department: "TOOLONG7");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetEvalSummary_ReturnsOk_WithReport()
    {
        var report = CreateTestEvalSummaryReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalSummaryReportAsync(202410, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetEvalSummary(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<EvalSummaryReport>(okResult.Value);
        Assert.Equal(202410, returnedReport.TermCode);
        Assert.Single(returnedReport.Departments);
    }

    [Fact]
    public async Task GetEvalSummary_PassesDepartmentFilter()
    {
        var report = CreateTestEvalSummaryReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalSummaryReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetEvalSummary(202410, department: "VME");

        Assert.IsType<OkObjectResult>(result.Result);
        _evaluationReportServiceMock.Verify(
            s => s.GetEvalSummaryReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEvalSummary_AcademicYear_ReturnsOk()
    {
        var report = CreateTestEvalSummaryReport();
        report.AcademicYear = "2024-2025";
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalSummaryReportByYearAsync("2024-2025", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetEvalSummary(academicYear: "2024-2025");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<EvalSummaryReport>(okResult.Value);
        Assert.Equal("2024-2025", returnedReport.AcademicYear);
    }

    [Fact]
    public async Task GetEvalSummary_ViewDeptUser_NoDepts_ReturnsForbid()
    {
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var result = await _controller.GetEvalSummary(202410);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetEvalSummary_ViewDeptUser_FiltersResponseDepartments()
    {
        var report = CreateTestEvalSummaryReport();
        report.Departments.Add(new EvalDepartmentGroup
        {
            Department = "PMI",
            DepartmentAverage = 3.8m,
            TotalResponses = 20,
            Instructors = []
        });
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalSummaryReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetEvalSummary(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<EvalSummaryReport>(okResult.Value);
        Assert.Single(returnedReport.Departments);
        Assert.Equal("VME", returnedReport.Departments[0].Department);
    }

    [Fact]
    public async Task GetEvalSummary_ReturnsOk_WithEmptyReport()
    {
        var emptyReport = new EvalSummaryReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            Departments = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalSummaryReportAsync(202410, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var result = await _controller.GetEvalSummary(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<EvalSummaryReport>(okResult.Value);
        Assert.Empty(returnedReport.Departments);
    }

    [Fact]
    public async Task GetEvalSummary_PassesPersonIdFilter()
    {
        var report = CreateTestEvalSummaryReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalSummaryReportAsync(202410, null, 42, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetEvalSummary(202410, personId: 42);

        Assert.IsType<OkObjectResult>(result.Result);
        _evaluationReportServiceMock.Verify(
            s => s.GetEvalSummaryReportAsync(202410, null, 42, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEvalSummary_PassesRoleFilter()
    {
        var report = CreateTestEvalSummaryReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalSummaryReportAsync(202410, null, null, "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetEvalSummary(202410, role: "1");

        Assert.IsType<OkObjectResult>(result.Result);
        _evaluationReportServiceMock.Verify(
            s => s.GetEvalSummaryReportAsync(202410, null, null, "1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Eval Detail Tests

    [Fact]
    public async Task GetEvalDetail_ReturnsBadRequest_WhenNoTermOrYear()
    {
        var result = await _controller.GetEvalDetail(0);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetEvalDetail_ReturnsBadRequest_WhenAcademicYearNotConsecutive()
    {
        var result = await _controller.GetEvalDetail(academicYear: "2024-2099");
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("consecutive", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task GetEvalDetail_ReturnsBadRequest_WhenDepartmentTooLong()
    {
        var result = await _controller.GetEvalDetail(202410, department: "TOOLONG7");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetEvalDetail_ReturnsOk_WithReport()
    {
        var report = CreateTestEvalDetailReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalDetailReportAsync(202410, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetEvalDetail(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<EvalDetailReport>(okResult.Value);
        Assert.Equal(202410, returnedReport.TermCode);
        Assert.Single(returnedReport.Departments);
    }

    [Fact]
    public async Task GetEvalDetail_PassesDepartmentFilter()
    {
        var report = CreateTestEvalDetailReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalDetailReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetEvalDetail(202410, department: "VME");

        Assert.IsType<OkObjectResult>(result.Result);
        _evaluationReportServiceMock.Verify(
            s => s.GetEvalDetailReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEvalDetail_AcademicYear_ReturnsOk()
    {
        var report = CreateTestEvalDetailReport();
        report.AcademicYear = "2024-2025";
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalDetailReportByYearAsync("2024-2025", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetEvalDetail(academicYear: "2024-2025");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<EvalDetailReport>(okResult.Value);
        Assert.Equal("2024-2025", returnedReport.AcademicYear);
    }

    [Fact]
    public async Task GetEvalDetail_ViewDeptUser_NoDepts_ReturnsForbid()
    {
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var result = await _controller.GetEvalDetail(202410);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetEvalDetail_ViewDeptUser_FiltersResponseDepartments()
    {
        var report = CreateTestEvalDetailReport();
        report.Departments.Add(new EvalDetailDepartmentGroup
        {
            Department = "PMI",
            DepartmentAverage = 3.8m,
            Instructors = []
        });
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalDetailReportAsync(202410, It.Is<IReadOnlyList<string>?>(d => IsDepts(d, "VME")), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetEvalDetail(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<EvalDetailReport>(okResult.Value);
        Assert.Single(returnedReport.Departments);
        Assert.DoesNotContain(returnedReport.Departments, d => d.Department == "PMI");
    }

    [Fact]
    public async Task GetEvalDetail_ReturnsOk_WithEmptyReport()
    {
        var emptyReport = new EvalDetailReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            Departments = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalDetailReportAsync(202410, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var result = await _controller.GetEvalDetail(202410);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<EvalDetailReport>(okResult.Value);
        Assert.Empty(returnedReport.Departments);
    }

    [Fact]
    public async Task GetEvalDetail_PassesPersonIdFilter()
    {
        var report = CreateTestEvalDetailReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalDetailReportAsync(202410, null, 42, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetEvalDetail(202410, personId: 42);

        Assert.IsType<OkObjectResult>(result.Result);
        _evaluationReportServiceMock.Verify(
            s => s.GetEvalDetailReportAsync(202410, null, 42, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEvalDetail_PassesRoleFilter()
    {
        var report = CreateTestEvalDetailReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalDetailReportAsync(202410, null, null, "2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetEvalDetail(202410, role: "2");

        Assert.IsType<OkObjectResult>(result.Result);
        _evaluationReportServiceMock.Verify(
            s => s.GetEvalDetailReportAsync(202410, null, null, "2", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Eval PDF Export Tests

    [Fact]
    public async Task ExportEvalSummaryPdf_ReturnsBadRequest_WhenNoTermOrYear()
    {
        var request = new ReportPdfRequest(TermCode: 0);
        var result = await _controller.ExportEvalSummaryPdf(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExportEvalSummaryPdf_ReturnsNoContent_WhenEmpty()
    {
        var emptyReport = new EvalSummaryReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            Departments = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalSummaryReportAsync(202410, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var request = new ReportPdfRequest(TermCode: 202410);
        var result = await _controller.ExportEvalSummaryPdf(request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ExportEvalSummaryPdf_ReturnsFile_WithData()
    {
        var report = CreateTestEvalSummaryReport();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalSummaryReportAsync(202410, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        _evaluationReportServiceMock
            .Setup(s => s.GenerateSummaryPdfAsync(report))
            .ReturnsAsync(pdfBytes);

        var request = new ReportPdfRequest(TermCode: 202410);
        var result = await _controller.ExportEvalSummaryPdf(request);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal(pdfBytes, fileResult.FileContents);
    }

    [Fact]
    public async Task ExportEvalDetailPdf_ReturnsBadRequest_WhenNoTermOrYear()
    {
        var request = new ReportPdfRequest(TermCode: 0);
        var result = await _controller.ExportEvalDetailPdf(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExportEvalDetailPdf_ReturnsNoContent_WhenEmpty()
    {
        var emptyReport = new EvalDetailReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            Departments = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalDetailReportAsync(202410, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var request = new ReportPdfRequest(TermCode: 202410);
        var result = await _controller.ExportEvalDetailPdf(request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ExportEvalDetailPdf_ReturnsFile_WithData()
    {
        var report = CreateTestEvalDetailReport();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _evaluationReportServiceMock
            .Setup(s => s.GetEvalDetailReportAsync(202410, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        _evaluationReportServiceMock
            .Setup(s => s.GenerateDetailPdfAsync(report))
            .ReturnsAsync(pdfBytes);

        var request = new ReportPdfRequest(TermCode: 202410);
        var result = await _controller.ExportEvalDetailPdf(request);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal(pdfBytes, fileResult.FileContents);
    }

    #endregion

    #region Multi-Year Merit + Evaluation Tests

    private static MultiYearReport CreateTestMultiYearReport()
    {
        return new MultiYearReport
        {
            MothraId = "A12345678",
            Instructor = "Smith, John",
            Department = "VME",
            StartYear = 2020,
            EndYear = 2024,
            UseAcademicYear = true,
            EffortTypes = ["CLI", "LEC"],
            MeritSection = new MultiYearMeritSection
            {
                Years =
                [
                    new MultiYearMeritYear
                    {
                        Year = 2020,
                        YearLabel = "2020-2021",
                        Courses =
                        [
                            new MultiYearCourseRow
                            {
                                Course = "VME 400-001",
                                TermCode = 202010,
                                Units = 4.0m,
                                Enrollment = 25,
                                Role = "I",
                                Efforts = new Dictionary<string, decimal> { ["LEC"] = 30.0m, ["CLI"] = 10.0m }
                            }
                        ],
                        YearTotals = new Dictionary<string, decimal> { ["LEC"] = 30.0m, ["CLI"] = 10.0m }
                    }
                ],
                GrandTotals = new Dictionary<string, decimal> { ["LEC"] = 30.0m, ["CLI"] = 10.0m },
                YearlyAverages = new Dictionary<string, decimal> { ["LEC"] = 30.0m, ["CLI"] = 10.0m }
            },
            EvalSection = new MultiYearEvalSection
            {
                Years =
                [
                    new MultiYearEvalYear
                    {
                        Year = 2020,
                        YearLabel = "2020-2021",
                        Courses =
                        [
                            new MultiYearEvalCourse
                            {
                                Course = "VME 400 (Intro)",
                                Crn = "40076",
                                TermCode = 202010,
                                Role = "I",
                                Average = 4.5m,
                                Median = 5.0m,
                                NumResponses = 20,
                                NumEnrolled = 25
                            }
                        ],
                        YearAverage = 4.5m,
                        YearMedian = 5.0m
                    }
                ],
                OverallAverage = 4.5m,
                OverallMedian = 5.0m
            }
        };
    }

    [Fact]
    public async Task GetMeritMultiYear_ReturnsBadRequest_WhenPersonIdIsZero()
    {
        var result = await _controller.GetMeritMultiYear(personId: 0, startYear: 2020, endYear: 2024);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritMultiYear_ReturnsBadRequest_WhenPersonIdIsNegative()
    {
        var result = await _controller.GetMeritMultiYear(personId: -1, startYear: 2020, endYear: 2024);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritMultiYear_ReturnsBadRequest_WhenYearsAreMissing()
    {
        var result = await _controller.GetMeritMultiYear(personId: 123, startYear: 0, endYear: 0);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritMultiYear_ReturnsBadRequest_WhenYearRangeExceeds10()
    {
        var result = await _controller.GetMeritMultiYear(personId: 123, startYear: 2010, endYear: 2025);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("10 years", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task GetMeritMultiYear_ReturnsBadRequest_WhenEndYearBeforeStartYear()
    {
        var result = await _controller.GetMeritMultiYear(personId: 123, startYear: 2024, endYear: 2020);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritMultiYear_ReturnsOk_WithReport()
    {
        var report = CreateTestMultiYearReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritMultiYearServiceMock
            .Setup(s => s.GetMultiYearReportAsync(123, 2020, 2024, null, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritMultiYear(personId: 123, startYear: 2020, endYear: 2024, useAcademicYear: true);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<MultiYearReport>(okResult.Value);
        Assert.Equal("Smith, John", returnedReport.Instructor);
        Assert.Equal("VME", returnedReport.Department);
        Assert.Single(returnedReport.MeritSection.Years);
        Assert.Single(returnedReport.EvalSection.Years);
    }

    [Fact]
    public async Task GetMeritMultiYear_ViewDeptUser_AuthorizedDept_AllowsAccess()
    {
        var report = CreateTestMultiYearReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _meritMultiYearServiceMock
            .Setup(s => s.GetMultiYearReportAsync(123, 2020, 2024, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritMultiYear(personId: 123, startYear: 2020, endYear: 2024);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMeritMultiYear_ViewDeptUser_UnauthorizedDept_ReturnsForbid()
    {
        var report = CreateTestMultiYearReport(); // Department = "VME"
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "APC" });
        _meritMultiYearServiceMock
            .Setup(s => s.GetMultiYearReportAsync(123, 2020, 2024, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetMeritMultiYear(personId: 123, startYear: 2020, endYear: 2024);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task ExportMeritMultiYearPdf_ReturnsBadRequest_WhenPersonIdIsZero()
    {
        var request = new MultiYearPdfRequest(PersonId: 0, StartYear: 2020, EndYear: 2024);
        var result = await _controller.ExportMeritMultiYearPdf(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExportMeritMultiYearPdf_ReturnsNoContent_WhenEmpty()
    {
        var emptyReport = new MultiYearReport
        {
            MothraId = "A12345678",
            Instructor = "Smith, John",
            Department = "VME",
            StartYear = 2020,
            EndYear = 2024,
            MeritSection = new MultiYearMeritSection(),
            EvalSection = new MultiYearEvalSection()
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritMultiYearServiceMock
            .Setup(s => s.GetMultiYearReportAsync(123, 2020, 2024, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var request = new MultiYearPdfRequest(PersonId: 123, StartYear: 2020, EndYear: 2024);
        var result = await _controller.ExportMeritMultiYearPdf(request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ExportMeritMultiYearPdf_ReturnsFile_WithData()
    {
        var report = CreateTestMultiYearReport();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _meritMultiYearServiceMock
            .Setup(s => s.GetMultiYearReportAsync(123, 2020, 2024, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        _meritMultiYearServiceMock
            .Setup(s => s.GenerateReportPdfAsync(report))
            .ReturnsAsync(pdfBytes);

        var request = new MultiYearPdfRequest(PersonId: 123, StartYear: 2020, EndYear: 2024);
        var result = await _controller.ExportMeritMultiYearPdf(request);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal(pdfBytes, fileResult.FileContents);
    }

    #endregion

    // ========================================================================
    // R5 Report Tests — Year Statistics
    // ========================================================================

    #region Test Data Helpers (R5 Year Statistics)

    private static YearStatisticsReport CreateTestYearStatisticsReport(string academicYear = "2024-2025")
    {
        return new YearStatisticsReport
        {
            AcademicYear = academicYear,
            EffortTypes = ["CLI", "LEC", "VAR"],
            Svm = new YearStatsSubReport
            {
                Label = "SVM - All Instructors",
                InstructorCount = 2,
                Instructors =
                [
                    new InstructorEffortDetail
                    {
                        MothraId = "A12345678",
                        Instructor = "Smith, John",
                        Department = "VME",
                        Discipline = "VME",
                        JobGroup = "PROFESSOR/IR",
                        Efforts = new Dictionary<string, decimal> { ["CLI"] = 10.0m, ["LEC"] = 30.0m },
                        TeachingHours = 30.0m
                    },
                    new InstructorEffortDetail
                    {
                        MothraId = "B98765432",
                        Instructor = "Doe, Jane",
                        Department = "APC",
                        Discipline = "APC",
                        JobGroup = "LECTURER",
                        Efforts = new Dictionary<string, decimal> { ["LEC"] = 20.0m, ["VAR"] = 5.0m },
                        TeachingHours = 20.0m
                    }
                ],
                Sums = new Dictionary<string, decimal> { ["CLI"] = 10.0m, ["LEC"] = 50.0m, ["VAR"] = 5.0m },
                Averages = new Dictionary<string, decimal> { ["CLI"] = 10.0m, ["LEC"] = 25.0m, ["VAR"] = 2.5m },
                Medians = new Dictionary<string, decimal> { ["CLI"] = 5.0m, ["LEC"] = 25.0m, ["VAR"] = 2.5m },
                TeachingHoursSum = 50.0m,
                TeachingHoursAverage = 25.0m,
                TeachingHoursMedian = 25.0m,
                ByDepartment = [],
                ByDiscipline = [],
                ByTitle = []
            },
            Dvm = new YearStatsSubReport { Label = "DVM/VET Programs" },
            Resident = new YearStatsSubReport { Label = "Resident Programs" },
            UndergradGrad = new YearStatsSubReport { Label = "Undergrad/Grad Programs" }
        };
    }

    #endregion

    #region Year Statistics Tests

    [Fact]
    public async Task GetYearStatistics_ReturnsBadRequest_WhenAcademicYearMissing()
    {
        var result = await _controller.GetYearStatistics(academicYear: null);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetYearStatistics_ReturnsBadRequest_WhenAcademicYearBadFormat()
    {
        var result = await _controller.GetYearStatistics(academicYear: "2024");
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetYearStatistics_ReturnsBadRequest_WhenAcademicYearNotConsecutive()
    {
        var result = await _controller.GetYearStatistics(academicYear: "2024-2099");
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("consecutive", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task GetYearStatistics_ReturnsForbid_WhenNotFullAccess()
    {
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _controller.GetYearStatistics(academicYear: "2024-2025");

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetYearStatistics_ReturnsOk_WithReport()
    {
        var report = CreateTestYearStatisticsReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _yearStatisticsServiceMock
            .Setup(s => s.GetYearStatisticsReportAsync("2024-2025", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetYearStatistics(academicYear: "2024-2025");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<YearStatisticsReport>(okResult.Value);
        Assert.Equal("2024-2025", returnedReport.AcademicYear);
        Assert.Equal(2, returnedReport.Svm.InstructorCount);
        Assert.Equal(3, returnedReport.EffortTypes.Count);
        _yearStatisticsServiceMock.Verify(
            s => s.GetYearStatisticsReportAsync("2024-2025", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetYearStatistics_ReturnsOk_WithEmptyReport()
    {
        var emptyReport = new YearStatisticsReport
        {
            AcademicYear = "2024-2025",
            EffortTypes = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _yearStatisticsServiceMock
            .Setup(s => s.GetYearStatisticsReportAsync("2024-2025", It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var result = await _controller.GetYearStatistics(academicYear: "2024-2025");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<YearStatisticsReport>(okResult.Value);
        Assert.Equal(0, returnedReport.Svm.InstructorCount);
    }

    [Fact]
    public async Task GetYearStatistics_VerifiesSubReportStructure()
    {
        var report = CreateTestYearStatisticsReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _yearStatisticsServiceMock
            .Setup(s => s.GetYearStatisticsReportAsync("2024-2025", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetYearStatistics(academicYear: "2024-2025");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<YearStatisticsReport>(okResult.Value);
        Assert.NotNull(returnedReport.Svm);
        Assert.NotNull(returnedReport.Dvm);
        Assert.NotNull(returnedReport.Resident);
        Assert.NotNull(returnedReport.UndergradGrad);
        Assert.Equal("SVM - All Instructors", returnedReport.Svm.Label);
        Assert.NotEmpty(returnedReport.Svm.Sums);
        Assert.NotEmpty(returnedReport.Svm.Averages);
        Assert.NotEmpty(returnedReport.Svm.Medians);
    }

    #endregion

    #region Year Statistics PDF Tests

    [Fact]
    public async Task ExportYearStatisticsPdf_ReturnsBadRequest_WhenAcademicYearMissing()
    {
        var request = new YearStatsPdfRequest(AcademicYear: null);
        var result = await _controller.ExportYearStatisticsPdf(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExportYearStatisticsPdf_ReturnsBadRequest_WhenAcademicYearBadFormat()
    {
        var request = new YearStatsPdfRequest(AcademicYear: "2024");
        var result = await _controller.ExportYearStatisticsPdf(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExportYearStatisticsPdf_ReturnsBadRequest_WhenAcademicYearNotConsecutive()
    {
        var request = new YearStatsPdfRequest(AcademicYear: "2024-2099");
        var result = await _controller.ExportYearStatisticsPdf(request);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("consecutive", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task ExportYearStatisticsPdf_ReturnsForbid_WhenNotFullAccess()
    {
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new YearStatsPdfRequest(AcademicYear: "2024-2025");
        var result = await _controller.ExportYearStatisticsPdf(request);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ExportYearStatisticsPdf_ReturnsNoContent_WhenEmpty()
    {
        var emptyReport = new YearStatisticsReport
        {
            AcademicYear = "2024-2025",
            EffortTypes = []
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _yearStatisticsServiceMock
            .Setup(s => s.GetYearStatisticsReportAsync("2024-2025", It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var request = new YearStatsPdfRequest(AcademicYear: "2024-2025");
        var result = await _controller.ExportYearStatisticsPdf(request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ExportYearStatisticsPdf_ReturnsFile_WithData()
    {
        var report = CreateTestYearStatisticsReport();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _yearStatisticsServiceMock
            .Setup(s => s.GetYearStatisticsReportAsync("2024-2025", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        _yearStatisticsServiceMock
            .Setup(s => s.GenerateReportPdfAsync(report))
            .ReturnsAsync(pdfBytes);

        var request = new YearStatsPdfRequest(AcademicYear: "2024-2025");
        var result = await _controller.ExportYearStatisticsPdf(request);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal(pdfBytes, fileResult.FileContents);
        Assert.Contains("YearStatistics_2024-2025", fileResult.FileDownloadName);
        _yearStatisticsServiceMock.Verify(
            s => s.GetYearStatisticsReportAsync("2024-2025", It.IsAny<CancellationToken>()),
            Times.Once);
        _yearStatisticsServiceMock.Verify(
            s => s.GenerateReportPdfAsync(report),
            Times.Once);
    }

    #endregion

    #region Sabbatical Endpoints

    [Fact]
    public async Task GetSabbatical_ValidPersonId_ReturnsSabbaticalData()
    {
        var dto = new SabbaticalDto
        {
            PersonId = 123,
            ExcludeClinicalTerms = "202401,202409",
            ExcludeDidacticTerms = "202401",
            ModifiedBy = "Test User"
        };
        _sabbaticalServiceMock
            .Setup(s => s.GetByPersonIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetSabbatical(123);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<SabbaticalDto>(okResult.Value);
        Assert.Equal(123, returnedDto.PersonId);
        Assert.Equal("202401,202409", returnedDto.ExcludeClinicalTerms);
    }

    [Fact]
    public async Task GetSabbatical_NoRecord_ReturnsEmptyDto()
    {
        _sabbaticalServiceMock
            .Setup(s => s.GetByPersonIdAsync(456, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SabbaticalDto?)null);

        var result = await _controller.GetSabbatical(456);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<SabbaticalDto>(okResult.Value);
        Assert.Equal(456, returnedDto.PersonId);
        Assert.Null(returnedDto.ExcludeClinicalTerms);
    }

    [Fact]
    public async Task GetSabbatical_InvalidPersonId_ReturnsBadRequest()
    {
        var result = await _controller.GetSabbatical(0);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task SaveSabbatical_ValidRequest_ReturnsUpdatedData()
    {
        var savedDto = new SabbaticalDto
        {
            PersonId = 123,
            ExcludeClinicalTerms = "202409",
            ExcludeDidacticTerms = null,
            ModifiedBy = "Admin User"
        };
        _permissionServiceMock.Setup(p => p.GetCurrentPersonId()).Returns(999);
        _sabbaticalServiceMock
            .Setup(s => s.SaveAsync(123, "202409", null, 999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedDto);

        var request = new SaveSabbaticalRequest("202409", null);
        var result = await _controller.SaveSabbatical(123, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<SabbaticalDto>(okResult.Value);
        Assert.Equal(123, returnedDto.PersonId);
        Assert.Equal("202409", returnedDto.ExcludeClinicalTerms);
    }

    [Fact]
    public async Task SaveSabbatical_InvalidPersonId_ReturnsBadRequest()
    {
        var request = new SaveSabbaticalRequest("202409", null);
        var result = await _controller.SaveSabbatical(-1, request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion
}
