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
    private readonly Mock<IEffortPermissionService> _permissionServiceMock;
    private readonly Mock<ILogger<ReportsController>> _loggerMock;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _teachingActivityServiceMock = new Mock<ITeachingActivityService>();
        _permissionServiceMock = new Mock<IEffortPermissionService>();
        _loggerMock = new Mock<ILogger<ReportsController>>();

        _controller = new ReportsController(
            _teachingActivityServiceMock.Object,
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
            .Setup(s => s.GetTeachingActivityReportAsync(202410, "VME", null, null, null, It.IsAny<CancellationToken>()))
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
            .Setup(s => s.GetTeachingActivityReportAsync(202410, "VME", 123, "I", "REG", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, department: "VME", personId: 123, role: "I", title: "REG");

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
        _teachingActivityServiceMock.Verify(
            s => s.GetTeachingActivityReportAsync(202410, "VME", 123, "I", "REG", It.IsAny<CancellationToken>()),
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
            .Setup(s => s.GetTeachingActivityReportAsync(202410, "APC", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, department: "APC");

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
        _teachingActivityServiceMock.Verify(
            s => s.GetTeachingActivityReportAsync(202410, "APC", null, null, null, It.IsAny<CancellationToken>()),
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
            .Setup(s => s.GetTeachingActivityReportAsync(202410, "VME", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act - no department filter requested
        var result = await _controller.GetTeachingActivityGrouped(202410);

        // Assert - should auto-select the single authorized dept
        Assert.IsType<OkObjectResult>(result.Result);
        _teachingActivityServiceMock.Verify(
            s => s.GetTeachingActivityReportAsync(202410, "VME", null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ViewDeptUser_MultipleDepts_PassesNull()
    {
        // Arrange - ViewDept user with multiple authorized departments
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME", "APC" });
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act - no department filter requested
        var result = await _controller.GetTeachingActivityGrouped(202410);

        // Assert - should pass null (let SP return all, service filters)
        Assert.IsType<OkObjectResult>(result.Result);
        _teachingActivityServiceMock.Verify(
            s => s.GetTeachingActivityReportAsync(202410, null, null, null, null, It.IsAny<CancellationToken>()),
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
            .Setup(s => s.GetTeachingActivityReportAsync(202410, "VME", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTeachingActivityGrouped(202410, department: "VME");

        // Assert - authorized dept is allowed through
        Assert.IsType<OkObjectResult>(result.Result);
        _teachingActivityServiceMock.Verify(
            s => s.GetTeachingActivityReportAsync(202410, "VME", null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTeachingActivityGrouped_ViewDeptUser_RequestsUnauthorizedDept_FallsBackToFirstAuthorized()
    {
        // Arrange - ViewDept user requests a department they don't have access to
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, "VME", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act - requests "PHR" but only authorized for "VME"
        var result = await _controller.GetTeachingActivityGrouped(202410, department: "PHR");

        // Assert - falls back to first authorized dept
        Assert.IsType<OkObjectResult>(result.Result);
        _teachingActivityServiceMock.Verify(
            s => s.GetTeachingActivityReportAsync(202410, "VME", null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
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
            .Setup(s => s.GetTeachingActivityReportAsync(202410, null, null, null, null, It.IsAny<CancellationToken>()))
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

    #region Boundary Value Tests

    [Fact]
    public async Task GetTeachingActivityGrouped_AcceptsDepartmentAt6Chars()
    {
        // Arrange
        var report = CreateTestReport();
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teachingActivityServiceMock
            .Setup(s => s.GetTeachingActivityReportAsync(202410, "ABCDEF", null, null, null, It.IsAny<CancellationToken>()))
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
}
