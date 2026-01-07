using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort.Controllers;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for CoursesController API endpoints.
/// </summary>
public sealed class CoursesControllerTests
{
    private readonly Mock<ICourseService> _courseServiceMock;
    private readonly Mock<IEffortPermissionService> _permissionServiceMock;
    private readonly Mock<ILogger<CoursesController>> _loggerMock;
    private readonly CoursesController _controller;

    public CoursesControllerTests()
    {
        _courseServiceMock = new Mock<ICourseService>();
        _permissionServiceMock = new Mock<IEffortPermissionService>();
        _loggerMock = new Mock<ILogger<CoursesController>>();

        _controller = new CoursesController(
            _courseServiceMock.Object,
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

    #region GetCourses Tests

    [Fact]
    public async Task GetCourses_ReturnsOk_WithCourseList()
    {
        // Arrange
        var courses = new List<CourseDto>
        {
            new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new CourseDto { Id = 2, TermCode = 202410, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 15, Units = 3, CustDept = "VME" }
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.GetCoursesAsync(202410, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courses);

        // Act
        var result = await _controller.GetCourses(202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourses = Assert.IsAssignableFrom<IEnumerable<CourseDto>>(okResult.Value);
        Assert.Equal(2, returnedCourses.Count());
    }

    [Fact]
    public async Task GetCourses_FiltersByDepartment_WhenProvided()
    {
        // Arrange
        var courses = new List<CourseDto>
        {
            new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" }
        };
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.GetCoursesAsync(202410, "DVM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(courses);

        // Act
        var result = await _controller.GetCourses(202410, "DVM");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourses = Assert.IsAssignableFrom<IEnumerable<CourseDto>>(okResult.Value);
        Assert.Single(returnedCourses);
    }

    #endregion

    #region GetCourse Tests

    [Fact]
    public async Task GetCourse_ReturnsOk_WhenCourseExists()
    {
        // Arrange
        var course = new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" };
        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _controller.GetCourse(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourse = Assert.IsType<CourseDto>(okResult.Value);
        Assert.Equal(1, returnedCourse.Id);
    }

    [Fact]
    public async Task GetCourse_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        // Arrange
        _courseServiceMock.Setup(s => s.GetCourseAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseDto?)null);

        // Act
        var result = await _controller.GetCourse(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("999", notFoundResult.Value?.ToString());
    }

    #endregion

    #region CreateCourse Tests

    [Fact]
    public async Task CreateCourse_ReturnsCreated_WithCourseDto()
    {
        // Arrange
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
        var createdCourse = new CourseDto { Id = 10, TermCode = 202410, Crn = "99999", SubjCode = "TST", CrseNumb = "101", SeqNumb = "001", Enrollment = 25, Units = 4, CustDept = "DVM" };

        _courseServiceMock.Setup(s => s.IsValidCustodialDepartment("DVM")).Returns(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.CourseExistsAsync(202410, "99999", 4, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _courseServiceMock.Setup(s => s.CreateCourseAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCourse);

        // Act
        var result = await _controller.CreateCourse(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        var returnedCourse = Assert.IsType<CourseDto>(createdResult.Value);
        Assert.Equal(10, returnedCourse.Id);
    }

    [Fact]
    public async Task CreateCourse_ReturnsBadRequest_WhenUserNotAuthorizedForDepartment()
    {
        // Arrange
        var request = new CreateCourseRequest
        {
            TermCode = 202410,
            Crn = "99999",
            SubjCode = "TST",
            CrseNumb = "101",
            SeqNumb = "001",
            Enrollment = 25,
            Units = 4,
            CustDept = "VME"
        };

        _courseServiceMock.Setup(s => s.IsValidCustodialDepartment("VME")).Returns(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _controller.CreateCourse(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
        Assert.Contains("Invalid custodial department", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateCourse_ReturnsConflict_ForDuplicateCourse()
    {
        // Arrange
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

        _courseServiceMock.Setup(s => s.IsValidCustodialDepartment("DVM")).Returns(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.CourseExistsAsync(202410, "99999", 4, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _controller.CreateCourse(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, conflictResult.StatusCode);
        Assert.Contains("already exists", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateCourse_ReturnsBadRequest_ForInvalidCustodialDepartment()
    {
        // Arrange
        var request = new CreateCourseRequest
        {
            TermCode = 202410,
            Crn = "99999",
            SubjCode = "TST",
            CrseNumb = "101",
            SeqNumb = "001",
            Enrollment = 25,
            Units = 4,
            CustDept = "INVALID"
        };

        _courseServiceMock.Setup(s => s.IsValidCustodialDepartment("INVALID")).Returns(false);

        // Act
        var result = await _controller.CreateCourse(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
        Assert.Contains("Invalid custodial department", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateCourse_ReturnsBadRequest_ForDbUpdateException()
    {
        // Arrange
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

        _courseServiceMock.Setup(s => s.IsValidCustodialDepartment("DVM")).Returns(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.CourseExistsAsync(202410, "99999", 4, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _courseServiceMock.Setup(s => s.CreateCourseAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Database constraint violation"));

        // Act
        var result = await _controller.CreateCourse(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
        Assert.Contains("Failed to save course", badRequestResult.Value?.ToString());
    }

    #endregion

    #region UpdateCourse Tests

    [Fact]
    public async Task UpdateCourse_ReturnsOk_WithUpdatedCourse()
    {
        // Arrange
        var existingCourse = new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" };
        var request = new UpdateCourseRequest
        {
            Enrollment = 30,
            Units = 5,
            CustDept = "VME"
        };
        var updatedCourse = new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 30, Units = 5, CustDept = "VME" };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.UpdateCourseAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCourse);

        // Act
        var result = await _controller.UpdateCourse(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourse = Assert.IsType<CourseDto>(okResult.Value);
        Assert.Equal(30, returnedCourse.Enrollment);
        Assert.Equal(5, returnedCourse.Units);
    }

    [Fact]
    public async Task UpdateCourse_ReturnsNotFound_WhenUserNotAuthorizedForNewDepartment()
    {
        // Arrange
        var existingCourse = new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" };
        var request = new UpdateCourseRequest
        {
            Enrollment = 30,
            Units = 5,
            CustDept = "VME"
        };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateCourse(1, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("1", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateCourse_AllowsSameDepartment_WithoutNewDepartmentAuth()
    {
        // Arrange - User can update course within their authorized department without needing extra check
        var existingCourse = new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" };
        var request = new UpdateCourseRequest
        {
            Enrollment = 30,
            Units = 5,
            CustDept = "DVM"
        };
        var updatedCourse = new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 30, Units = 5, CustDept = "DVM" };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.UpdateCourseAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCourse);

        // Act
        var result = await _controller.UpdateCourse(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourse = Assert.IsType<CourseDto>(okResult.Value);
        Assert.Equal(30, returnedCourse.Enrollment);
    }

    [Fact]
    public async Task UpdateCourse_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        // Arrange
        var request = new UpdateCourseRequest
        {
            Enrollment = 30,
            Units = 5,
            CustDept = "VME"
        };

        _courseServiceMock.Setup(s => s.GetCourseAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((CourseDto?)null);

        // Act
        var result = await _controller.UpdateCourse(999, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("999", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateCourse_ReturnsBadRequest_ForInvalidCustodialDepartment()
    {
        // Arrange
        var existingCourse = new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" };
        var request = new UpdateCourseRequest
        {
            Enrollment = 30,
            Units = 5,
            CustDept = "INVALID"
        };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("INVALID", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.UpdateCourseAsync(1, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid custodial department: INVALID"));

        // Act
        var result = await _controller.UpdateCourse(1, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Invalid custodial department", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateCourse_ReturnsBadRequest_ForDbUpdateException()
    {
        // Arrange
        var existingCourse = new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" };
        var request = new UpdateCourseRequest
        {
            Enrollment = 30,
            Units = 5,
            CustDept = "VME"
        };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.UpdateCourseAsync(1, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Database constraint violation"));

        // Act
        var result = await _controller.UpdateCourse(1, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Failed to update course", badRequestResult.Value?.ToString());
    }

    #endregion

    #region UpdateCourseEnrollment Tests

    [Fact]
    public async Task UpdateCourseEnrollment_ReturnsOk_ForRCourse()
    {
        // Arrange
        var existingCourse = new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443R", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" };
        var request = new UpdateEnrollmentRequest { Enrollment = 50 };
        var updatedCourse = new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443R", SeqNumb = "001", Enrollment = 50, Units = 4, CustDept = "DVM" };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.UpdateCourseEnrollmentAsync(1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCourse);

        // Act
        var result = await _controller.UpdateCourseEnrollment(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourse = Assert.IsType<CourseDto>(okResult.Value);
        Assert.Equal(50, returnedCourse.Enrollment);
    }

    [Fact]
    public async Task UpdateCourseEnrollment_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        // Arrange
        var request = new UpdateEnrollmentRequest { Enrollment = 50 };

        _courseServiceMock.Setup(s => s.GetCourseAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((CourseDto?)null);

        // Act
        var result = await _controller.UpdateCourseEnrollment(999, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateCourseEnrollment_ReturnsBadRequest_ForNonRCourse()
    {
        // Arrange
        var existingCourse = new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" };
        var request = new UpdateEnrollmentRequest { Enrollment = 50 };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.UpdateCourseEnrollmentAsync(1, 50, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Course is not an R-course"));

        // Act
        var result = await _controller.UpdateCourseEnrollment(1, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("R-course", badRequestResult.Value?.ToString());
    }

    #endregion

    #region DeleteCourse Tests

    [Fact]
    public async Task DeleteCourse_ReturnsNoContent_OnSuccess()
    {
        // Arrange
        var existingCourse = new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.DeleteCourseAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteCourse(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteCourse_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        // Arrange
        _courseServiceMock.Setup(s => s.GetCourseAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((CourseDto?)null);

        // Act
        var result = await _controller.DeleteCourse(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("999", notFoundResult.Value?.ToString());
    }

    #endregion

    #region CanDeleteCourse Tests

    [Fact]
    public async Task CanDeleteCourse_ReturnsOk_WithDeleteInfo()
    {
        // Arrange
        var existingCourse = new CourseDto { Id = 1, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.CanDeleteCourseAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 5));

        // Act
        var result = await _controller.CanDeleteCourse(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetDepartments Tests

    [Fact]
    public async Task GetDepartments_ReturnsOk_WithDepartmentList()
    {
        // Arrange
        var departments = new List<string> { "APC", "VMB", "VME", "VSR", "PMI", "PHR", "UNK", "DVM", "VET" };
        _courseServiceMock.Setup(s => s.GetValidCustodialDepartments())
            .Returns(departments);
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _controller.GetDepartments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDepartments = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
        Assert.Equal(9, returnedDepartments.Count());
    }

    #endregion

    #region ImportCourse Tests

    [Fact]
    public async Task ImportCourse_ReturnsCreated_OnSuccess()
    {
        // Arrange
        var request = new ImportCourseRequest
        {
            TermCode = 202410,
            Crn = "12345"
        };
        var bannerCourse = new BannerCourseDto { Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, UnitType = "F", UnitLow = 4, UnitHigh = 4, DeptCode = "72030" };
        var importedCourse = new CourseDto { Id = 10, TermCode = 202410, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "VME" };

        _courseServiceMock.Setup(s => s.GetBannerCourseAsync(202410, "12345", It.IsAny<CancellationToken>())).ReturnsAsync(bannerCourse);
        _courseServiceMock.Setup(s => s.CourseExistsAsync(202410, "12345", 4, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _courseServiceMock.Setup(s => s.GetCustodialDepartmentForBannerCode("72030")).Returns("VME");
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _courseServiceMock.Setup(s => s.ImportCourseFromBannerAsync(request, bannerCourse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(importedCourse);

        // Act
        var result = await _controller.ImportCourse(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task ImportCourse_ReturnsNotFound_WhenUserNotAuthorizedForTargetDepartment()
    {
        // Arrange
        var request = new ImportCourseRequest
        {
            TermCode = 202410,
            Crn = "12345"
        };
        var bannerCourse = new BannerCourseDto { Crn = "12345", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 20, UnitType = "F", UnitLow = 4, UnitHigh = 4, DeptCode = "72030" };

        _courseServiceMock.Setup(s => s.GetBannerCourseAsync(202410, "12345", It.IsAny<CancellationToken>())).ReturnsAsync(bannerCourse);
        _courseServiceMock.Setup(s => s.CourseExistsAsync(202410, "12345", 4, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _courseServiceMock.Setup(s => s.GetCustodialDepartmentForBannerCode("72030")).Returns("VME");
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _controller.ImportCourse(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("not found in Banner", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task ImportCourse_ReturnsConflict_WhenCourseAlreadyExists()
    {
        // Arrange
        var request = new ImportCourseRequest
        {
            TermCode = 202410,
            Crn = "12345"
        };
        var bannerCourse = new BannerCourseDto { Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, UnitType = "F", UnitLow = 4, UnitHigh = 4, DeptCode = "72030" };

        _courseServiceMock.Setup(s => s.GetBannerCourseAsync(202410, "12345", It.IsAny<CancellationToken>())).ReturnsAsync(bannerCourse);
        _courseServiceMock.Setup(s => s.CourseExistsAsync(202410, "12345", 4, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _controller.ImportCourse(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("already exists", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task ImportCourse_ReturnsNotFound_WhenBannerCourseNotFound()
    {
        // Arrange
        var request = new ImportCourseRequest
        {
            TermCode = 202410,
            Crn = "99999"
        };

        _courseServiceMock.Setup(s => s.GetBannerCourseAsync(202410, "99999", It.IsAny<CancellationToken>())).ReturnsAsync((BannerCourseDto?)null);

        // Act
        var result = await _controller.ImportCourse(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("not found in Banner", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task ImportCourse_ReturnsBadRequest_WhenUnitsOutOfRange()
    {
        // Arrange
        var request = new ImportCourseRequest
        {
            TermCode = 202410,
            Crn = "12345",
            Units = 10 // Out of range
        };
        var bannerCourse = new BannerCourseDto { Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, UnitType = "V", UnitLow = 1, UnitHigh = 4, DeptCode = "72030" };

        _courseServiceMock.Setup(s => s.GetBannerCourseAsync(202410, "12345", It.IsAny<CancellationToken>())).ReturnsAsync(bannerCourse);

        // Act
        var result = await _controller.ImportCourse(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("must be between", badRequestResult.Value?.ToString());
    }

    #endregion

    #region SearchBannerCourses Tests

    [Fact]
    public async Task SearchBannerCourses_ReturnsOk_WithResults()
    {
        // Arrange
        var bannerCourses = new List<BannerCourseDto>
        {
            new BannerCourseDto { Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20 }
        };
        _courseServiceMock.Setup(s => s.SearchBannerCoursesAsync(202410, "DVM", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bannerCourses);

        // Act
        var result = await _controller.SearchBannerCourses(202410, "DVM");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourses = Assert.IsAssignableFrom<IEnumerable<BannerCourseDto>>(okResult.Value);
        Assert.Single(returnedCourses);
    }

    [Fact]
    public async Task SearchBannerCourses_ReturnsBadRequest_WhenNoSearchParametersProvided()
    {
        // Act
        var result = await _controller.SearchBannerCourses(202410);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("At least one search parameter", badRequestResult.Value?.ToString());
    }

    #endregion
}
