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
/// Unit tests for InstructorsController API endpoints.
/// </summary>
public sealed class InstructorsControllerTests
{
    private readonly Mock<IInstructorService> _instructorServiceMock;
    private readonly Mock<IEffortPermissionService> _permissionServiceMock;
    private readonly Mock<ILogger<InstructorsController>> _loggerMock;
    private readonly InstructorsController _controller;

    public InstructorsControllerTests()
    {
        _instructorServiceMock = new Mock<IInstructorService>();
        _permissionServiceMock = new Mock<IEffortPermissionService>();
        _loggerMock = new Mock<ILogger<InstructorsController>>();

        _controller = new InstructorsController(
            _instructorServiceMock.Object,
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

    #region GetInstructors Tests

    [Fact]
    public async Task GetInstructors_ReturnsOk_WithInstructorList()
    {
        // Arrange
        var instructors = new List<PersonDto>
        {
            new PersonDto { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME" },
            new PersonDto { PersonId = 2, TermCode = 202410, FirstName = "Jane", LastName = "Smith", EffortDept = "VME" }
        };
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _instructorServiceMock.Setup(s => s.GetInstructorsAsync(202410, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instructors);

        // Act
        var result = await _controller.GetInstructors(202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstructors = Assert.IsAssignableFrom<IEnumerable<PersonDto>>(okResult.Value);
        Assert.Equal(2, returnedInstructors.Count());
    }

    [Fact]
    public async Task GetInstructors_FiltersByDepartment_WhenProvided()
    {
        // Arrange
        var instructors = new List<PersonDto>
        {
            new PersonDto { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME" }
        };
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _instructorServiceMock.Setup(s => s.GetInstructorsAsync(202410, "VME", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instructors);

        // Act
        var result = await _controller.GetInstructors(202410, "VME");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstructors = Assert.IsAssignableFrom<IEnumerable<PersonDto>>(okResult.Value);
        Assert.Single(returnedInstructors);
    }

    [Fact]
    public async Task GetInstructors_ReturnsEmptyList_WhenUnauthorizedForDepartment()
    {
        // Arrange
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("SECRET", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _controller.GetInstructors(202410, "SECRET");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstructors = Assert.IsAssignableFrom<IEnumerable<PersonDto>>(okResult.Value);
        Assert.Empty(returnedInstructors);
    }

    #endregion

    #region GetInstructor Tests

    [Fact]
    public async Task GetInstructor_ReturnsOk_WhenInstructorExists()
    {
        // Arrange
        var instructor = new PersonDto { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME" };
        _instructorServiceMock.Setup(s => s.GetInstructorAsync(1, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _controller.GetInstructor(1, 202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstructor = Assert.IsType<PersonDto>(okResult.Value);
        Assert.Equal(1, returnedInstructor.PersonId);
    }

    [Fact]
    public async Task GetInstructor_ReturnsNotFound_WhenInstructorDoesNotExist()
    {
        // Arrange
        _instructorServiceMock.Setup(s => s.GetInstructorAsync(999, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDto?)null);

        // Act
        var result = await _controller.GetInstructor(999, 202410);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetInstructor_ReturnsNotFound_WhenUnauthorizedForDepartment()
    {
        // Arrange
        var instructor = new PersonDto { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "SECRET" };
        _instructorServiceMock.Setup(s => s.GetInstructorAsync(1, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("SECRET", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _controller.GetInstructor(1, 202410);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region CreateInstructor Tests

    [Fact]
    public async Task CreateInstructor_ReturnsCreated_WithInstructorDto()
    {
        // Arrange
        var request = new CreateInstructorRequest
        {
            PersonId = 1,
            TermCode = 202410
        };
        var createdInstructor = new PersonDto { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME" };

        _instructorServiceMock.Setup(s => s.InstructorExistsAsync(1, 202410, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _instructorServiceMock.Setup(s => s.ResolveInstructorDepartmentAsync(1, 202410, It.IsAny<CancellationToken>())).ReturnsAsync("VME");
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _instructorServiceMock.Setup(s => s.CreateInstructorAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdInstructor);

        // Act
        var result = await _controller.CreateInstructor(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        var returnedInstructor = Assert.IsType<PersonDto>(createdResult.Value);
        Assert.Equal(1, returnedInstructor.PersonId);
    }

    [Fact]
    public async Task CreateInstructor_ReturnsConflict_WhenInstructorAlreadyExists()
    {
        // Arrange
        var request = new CreateInstructorRequest
        {
            PersonId = 1,
            TermCode = 202410
        };

        _instructorServiceMock.Setup(s => s.InstructorExistsAsync(1, 202410, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _controller.CreateInstructor(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, conflictResult.StatusCode);
        Assert.Contains("already exists", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateInstructor_ReturnsBadRequest_ForInvalidOperationException()
    {
        // Arrange
        var request = new CreateInstructorRequest
        {
            PersonId = 999,
            TermCode = 202410
        };

        _instructorServiceMock.Setup(s => s.InstructorExistsAsync(999, 202410, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _instructorServiceMock.Setup(s => s.ResolveInstructorDepartmentAsync(999, 202410, It.IsAny<CancellationToken>())).ReturnsAsync("VME");
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _instructorServiceMock.Setup(s => s.CreateInstructorAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Person not found in AAUD"));

        // Act
        var result = await _controller.CreateInstructor(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("not found in AAUD", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateInstructor_ReturnsBadRequest_ForDbUpdateException()
    {
        // Arrange
        var request = new CreateInstructorRequest
        {
            PersonId = 1,
            TermCode = 202410
        };

        _instructorServiceMock.Setup(s => s.InstructorExistsAsync(1, 202410, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _instructorServiceMock.Setup(s => s.ResolveInstructorDepartmentAsync(1, 202410, It.IsAny<CancellationToken>())).ReturnsAsync("VME");
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _instructorServiceMock.Setup(s => s.CreateInstructorAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Database constraint violation"));

        // Act
        var result = await _controller.CreateInstructor(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Failed to create instructor", badRequestResult.Value?.ToString());
    }

    #endregion

    #region UpdateInstructor Tests

    [Fact]
    public async Task UpdateInstructor_ReturnsOk_WithUpdatedInstructor()
    {
        // Arrange
        var existingInstructor = new PersonDto { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME" };
        var request = new UpdateInstructorRequest
        {
            EffortDept = "APC",
            EffortTitleCode = "5678",
            VolunteerWos = false
        };
        var updatedInstructor = new PersonDto { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "APC" };

        _instructorServiceMock.Setup(s => s.GetInstructorAsync(1, 202410, It.IsAny<CancellationToken>())).ReturnsAsync(existingInstructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("APC", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _instructorServiceMock.Setup(s => s.UpdateInstructorAsync(1, 202410, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedInstructor);

        // Act
        var result = await _controller.UpdateInstructor(1, 202410, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstructor = Assert.IsType<PersonDto>(okResult.Value);
        Assert.Equal("APC", returnedInstructor.EffortDept);
    }

    [Fact]
    public async Task UpdateInstructor_ReturnsNotFound_WhenChangingToUnauthorizedDepartment()
    {
        // Arrange
        var existingInstructor = new PersonDto { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME" };
        var request = new UpdateInstructorRequest
        {
            EffortDept = "SECRET",
            EffortTitleCode = "5678",
            VolunteerWos = false
        };

        _instructorServiceMock.Setup(s => s.GetInstructorAsync(1, 202410, It.IsAny<CancellationToken>())).ReturnsAsync(existingInstructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("SECRET", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateInstructor(1, 202410, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateInstructor_ReturnsNotFound_WhenInstructorDoesNotExist()
    {
        // Arrange
        var request = new UpdateInstructorRequest
        {
            EffortDept = "APC",
            EffortTitleCode = "5678",
            VolunteerWos = false
        };

        _instructorServiceMock.Setup(s => s.GetInstructorAsync(999, 202410, It.IsAny<CancellationToken>())).ReturnsAsync((PersonDto?)null);

        // Act
        var result = await _controller.UpdateInstructor(999, 202410, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateInstructor_ReturnsBadRequest_ForInvalidDepartment()
    {
        // Arrange
        var existingInstructor = new PersonDto { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME" };
        var request = new UpdateInstructorRequest
        {
            EffortDept = "INVALID",
            EffortTitleCode = "5678",
            VolunteerWos = false
        };

        _instructorServiceMock.Setup(s => s.GetInstructorAsync(1, 202410, It.IsAny<CancellationToken>())).ReturnsAsync(existingInstructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("INVALID", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _instructorServiceMock.Setup(s => s.UpdateInstructorAsync(1, 202410, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid department: INVALID"));

        // Act
        var result = await _controller.UpdateInstructor(1, 202410, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Invalid department", badRequestResult.Value?.ToString());
    }

    #endregion

    #region DeleteInstructor Tests

    [Fact]
    public async Task DeleteInstructor_ReturnsNoContent_OnSuccess()
    {
        // Arrange
        var existingInstructor = new PersonDto { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME" };

        _instructorServiceMock.Setup(s => s.GetInstructorAsync(1, 202410, It.IsAny<CancellationToken>())).ReturnsAsync(existingInstructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _instructorServiceMock.Setup(s => s.DeleteInstructorAsync(1, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteInstructor(1, 202410);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteInstructor_ReturnsNotFound_WhenInstructorDoesNotExist()
    {
        // Arrange
        _instructorServiceMock.Setup(s => s.GetInstructorAsync(999, 202410, It.IsAny<CancellationToken>())).ReturnsAsync((PersonDto?)null);

        // Act
        var result = await _controller.DeleteInstructor(999, 202410);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteInstructor_ReturnsNotFound_WhenUnauthorizedForDepartment()
    {
        // Arrange
        var existingInstructor = new PersonDto { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "SECRET" };

        _instructorServiceMock.Setup(s => s.GetInstructorAsync(1, 202410, It.IsAny<CancellationToken>())).ReturnsAsync(existingInstructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("SECRET", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteInstructor(1, 202410);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region CanDeleteInstructor Tests

    [Fact]
    public async Task CanDeleteInstructor_ReturnsOk_WithDeleteInfo()
    {
        // Arrange
        var existingInstructor = new PersonDto { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME" };

        _instructorServiceMock.Setup(s => s.GetInstructorAsync(1, 202410, It.IsAny<CancellationToken>())).ReturnsAsync(existingInstructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _instructorServiceMock.Setup(s => s.CanDeleteInstructorAsync(1, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 5));

        // Act
        var result = await _controller.CanDeleteInstructor(1, 202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetDepartments Tests

    [Fact]
    public async Task GetDepartments_ReturnsAllDepartments_ForFullAccessUser()
    {
        // Arrange
        var departments = new List<DepartmentDto>
        {
            new DepartmentDto { Code = "VME", Name = "Medicine & Epidemiology", Group = "Academic" },
            new DepartmentDto { Code = "APC", Name = "Anatomy, Physiology & Cell Biology", Group = "Academic" },
            new DepartmentDto { Code = "WHC", Name = "Wildlife Health Center", Group = "Centers" }
        };
        _instructorServiceMock.Setup(s => s.GetDepartments()).Returns(departments);
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _controller.GetDepartments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDepartments = Assert.IsAssignableFrom<IEnumerable<DepartmentDto>>(okResult.Value);
        Assert.Equal(3, returnedDepartments.Count());
    }

    [Fact]
    public async Task GetDepartments_ReturnsOnlyAuthorizedDepartments_ForNonAdminUser()
    {
        // Arrange
        var departments = new List<DepartmentDto>
        {
            new DepartmentDto { Code = "VME", Name = "Medicine & Epidemiology", Group = "Academic" },
            new DepartmentDto { Code = "APC", Name = "Anatomy, Physiology & Cell Biology", Group = "Academic" },
            new DepartmentDto { Code = "WHC", Name = "Wildlife Health Center", Group = "Centers" }
        };
        _instructorServiceMock.Setup(s => s.GetDepartments()).Returns(departments);
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "VME" });

        // Act
        var result = await _controller.GetDepartments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDepartments = Assert.IsAssignableFrom<IEnumerable<DepartmentDto>>(okResult.Value);
        Assert.Single(returnedDepartments);
        Assert.Equal("VME", returnedDepartments.First().Code);
    }

    #endregion

    #region GetReportUnits Tests

    [Fact]
    public async Task GetReportUnits_ReturnsOk_WithReportUnits()
    {
        // Arrange
        var reportUnits = new List<ReportUnitDto>
        {
            new ReportUnitDto { Abbrev = "SVM", Unit = "School of Veterinary Medicine" },
            new ReportUnitDto { Abbrev = "VMB", Unit = "Molecular Biosciences" }
        };
        _instructorServiceMock.Setup(s => s.GetReportUnitsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportUnits);

        // Act
        var result = await _controller.GetReportUnits();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUnits = Assert.IsAssignableFrom<IEnumerable<ReportUnitDto>>(okResult.Value);
        Assert.Equal(2, returnedUnits.Count());
    }

    #endregion

    #region GetInstructorEffortRecords Tests

    [Fact]
    public async Task GetInstructorEffortRecords_ReturnsOk_WithEffortRecords()
    {
        // Arrange
        var existingInstructor = new PersonDto { PersonId = 1, TermCode = 202410, FirstName = "John", LastName = "Doe", EffortDept = "VME" };
        var effortRecords = new List<InstructorEffortRecordDto>
        {
            new InstructorEffortRecordDto { CourseId = 100, Weeks = 2, Course = new CourseDto { Id = 100, Crn = "12345" } }
        };

        _instructorServiceMock.Setup(s => s.GetInstructorAsync(1, 202410, It.IsAny<CancellationToken>())).ReturnsAsync(existingInstructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _instructorServiceMock.Setup(s => s.GetInstructorEffortRecordsAsync(1, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(effortRecords);

        // Act
        var result = await _controller.GetInstructorEffortRecords(1, 202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRecords = Assert.IsAssignableFrom<IEnumerable<InstructorEffortRecordDto>>(okResult.Value);
        Assert.Single(returnedRecords);
    }

    [Fact]
    public async Task GetInstructorEffortRecords_ReturnsNotFound_WhenInstructorDoesNotExist()
    {
        // Arrange
        _instructorServiceMock.Setup(s => s.GetInstructorAsync(999, 202410, It.IsAny<CancellationToken>())).ReturnsAsync((PersonDto?)null);

        // Act
        var result = await _controller.GetInstructorEffortRecords(999, 202410);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region GetTitleCodes Tests

    [Fact]
    public async Task GetTitleCodes_ReturnsOk_WithTitleCodes()
    {
        // Arrange
        var titleCodes = new List<TitleCodeDto>
        {
            new TitleCodeDto { Code = "000353", Name = "VETERINARIAN" },
            new TitleCodeDto { Code = "000521", Name = "PROFESSOR" }
        };
        _instructorServiceMock.Setup(s => s.GetTitleCodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(titleCodes);

        // Act
        var result = await _controller.GetTitleCodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTitleCodes = Assert.IsAssignableFrom<IEnumerable<TitleCodeDto>>(okResult.Value);
        Assert.Equal(2, returnedTitleCodes.Count());
    }

    [Fact]
    public async Task GetTitleCodes_ReturnsOk_WithEmptyList_WhenNoTitleCodes()
    {
        // Arrange
        _instructorServiceMock.Setup(s => s.GetTitleCodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TitleCodeDto>());

        // Act
        var result = await _controller.GetTitleCodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTitleCodes = Assert.IsAssignableFrom<IEnumerable<TitleCodeDto>>(okResult.Value);
        Assert.Empty(returnedTitleCodes);
    }

    #endregion

    #region GetJobGroups Tests

    [Fact]
    public async Task GetJobGroups_ReturnsOk_WithJobGroups()
    {
        // Arrange
        var jobGroups = new List<JobGroupDto>
        {
            new JobGroupDto { Code = "I15", Name = "STAFF VET" },
            new JobGroupDto { Code = "B24", Name = "" } // NULL name in legacy data shows code only
        };
        _instructorServiceMock.Setup(s => s.GetJobGroupsAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobGroups);

        // Act
        var result = await _controller.GetJobGroups();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedJobGroups = Assert.IsAssignableFrom<IEnumerable<JobGroupDto>>(okResult.Value);
        Assert.Equal(2, returnedJobGroups.Count());
    }

    [Fact]
    public async Task GetJobGroups_ReturnsOk_WithEmptyList_WhenNoJobGroups()
    {
        // Arrange
        _instructorServiceMock.Setup(s => s.GetJobGroupsAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobGroupDto>());

        // Act
        var result = await _controller.GetJobGroups();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedJobGroups = Assert.IsAssignableFrom<IEnumerable<JobGroupDto>>(okResult.Value);
        Assert.Empty(returnedJobGroups);
    }

    #endregion
}
