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
    private readonly Mock<IZeroEffortService> _zeroEffortServiceMock;
    private readonly Mock<IEffortPermissionService> _permissionServiceMock;
    private readonly Mock<ILogger<ReportsController>> _loggerMock;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _teachingActivityServiceMock = new Mock<ITeachingActivityService>();
        _deptSummaryServiceMock = new Mock<IDeptSummaryService>();
        _schoolSummaryServiceMock = new Mock<ISchoolSummaryService>();
        _meritReportServiceMock = new Mock<IMeritReportService>();
        _zeroEffortServiceMock = new Mock<IZeroEffortService>();
        _permissionServiceMock = new Mock<IEffortPermissionService>();
        _loggerMock = new Mock<ILogger<ReportsController>>();

        _controller = new ReportsController(
            _teachingActivityServiceMock.Object,
            _deptSummaryServiceMock.Object,
            _schoolSummaryServiceMock.Object,
            _meritReportServiceMock.Object,
            _zeroEffortServiceMock.Object,
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
}
