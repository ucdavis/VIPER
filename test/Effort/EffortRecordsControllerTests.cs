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
/// Unit tests for EffortRecordsController API endpoints.
/// </summary>
public sealed class EffortRecordsControllerTests
{
    private readonly Mock<IEffortRecordService> _recordServiceMock;
    private readonly Mock<IEffortPermissionService> _permissionServiceMock;
    private readonly Mock<ILogger<EffortRecordsController>> _loggerMock;
    private readonly EffortRecordsController _controller;

    private const int TestRecordId = 1;
    private const int TestPersonId = 100;
    private const int TestTermCode = 202410;

    public EffortRecordsControllerTests()
    {
        _recordServiceMock = new Mock<IEffortRecordService>();
        _permissionServiceMock = new Mock<IEffortPermissionService>();
        _loggerMock = new Mock<ILogger<EffortRecordsController>>();

        _controller = new EffortRecordsController(
            _recordServiceMock.Object,
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

    private static InstructorEffortRecordDto CreateTestRecord(int id = TestRecordId) => new()
    {
        Id = id,
        CourseId = 1,
        PersonId = TestPersonId,
        TermCode = TestTermCode,
        EffortType = "LEC",
        Role = 1,
        RoleDescription = "Instructor of Record",
        Hours = 40,
        Course = new CourseDto
        {
            Id = 1,
            Crn = "12345",
            TermCode = TestTermCode,
            SubjCode = "VET",
            CrseNumb = "410",
            SeqNumb = "01",
            Enrollment = 20,
            Units = 4,
            CustDept = "VME"
        }
    };

    #region GetRecord Tests

    [Fact]
    public async Task GetRecord_ReturnsOk_WhenRecordExistsAndAuthorized()
    {
        // Arrange
        var record = CreateTestRecord();
        _recordServiceMock.Setup(s => s.GetEffortRecordAsync(TestRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _permissionServiceMock.Setup(s => s.CanViewPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.GetRecord(TestRecordId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRecord = Assert.IsType<InstructorEffortRecordDto>(okResult.Value);
        Assert.Equal(TestRecordId, returnedRecord.Id);
    }

    [Fact]
    public async Task GetRecord_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        // Arrange
        _recordServiceMock.Setup(s => s.GetEffortRecordAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InstructorEffortRecordDto?)null);

        // Act
        var result = await _controller.GetRecord(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetRecord_ReturnsNotFound_WhenUserNotAuthorized()
    {
        // Arrange
        var record = CreateTestRecord();
        _recordServiceMock.Setup(s => s.GetEffortRecordAsync(TestRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _permissionServiceMock.Setup(s => s.CanViewPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetRecord(TestRecordId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region CreateRecord Tests

    [Fact]
    public async Task CreateRecord_ReturnsCreatedAtAction_WhenSuccessful()
    {
        // Arrange
        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = 1,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };
        var record = CreateTestRecord();

        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _recordServiceMock.Setup(s => s.CanEditTermAsync(TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _recordServiceMock.Setup(s => s.CreateEffortRecordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((record, (string?)null));

        // Act
        var result = await _controller.CreateRecord(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetRecord), createdResult.ActionName);
    }

    [Fact]
    public async Task CreateRecord_ReturnsNotFound_WhenUserNotAuthorizedForPerson()
    {
        // Arrange
        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = 1,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CreateRecord(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateRecord_ReturnsBadRequest_WhenTermNotEditable()
    {
        // Arrange
        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = 1,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _recordServiceMock.Setup(s => s.CanEditTermAsync(TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CreateRecord(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("not open for editing", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task CreateRecord_ReturnsBadRequest_WhenInvalidOperationException()
    {
        // Arrange
        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = 1,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _recordServiceMock.Setup(s => s.CanEditTermAsync(TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _recordServiceMock.Setup(s => s.CreateEffortRecordAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate record"));

        // Act
        var result = await _controller.CreateRecord(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Duplicate record", badRequest.Value);
    }

    [Fact]
    public async Task CreateRecord_ReturnsBadRequest_WhenDbUpdateException()
    {
        // Arrange
        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = 1,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _recordServiceMock.Setup(s => s.CanEditTermAsync(TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _recordServiceMock.Setup(s => s.CreateEffortRecordAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("DB error"));

        // Act
        var result = await _controller.CreateRecord(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Failed to create", badRequest.Value?.ToString());
    }

    #endregion

    #region UpdateRecord Tests

    [Fact]
    public async Task UpdateRecord_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var existingRecord = CreateTestRecord();
        var request = new UpdateEffortRecordRequest
        {
            EffortTypeId = "LAB",
            RoleId = 2,
            EffortValue = 30
        };

        _recordServiceMock.Setup(s => s.GetEffortRecordAsync(TestRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRecord);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _recordServiceMock.Setup(s => s.CanEditTermAsync(TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _recordServiceMock.Setup(s => s.UpdateEffortRecordAsync(TestRecordId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((existingRecord, (string?)null));

        // Act
        var result = await _controller.UpdateRecord(TestRecordId, request);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateRecord_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        // Arrange
        var request = new UpdateEffortRecordRequest
        {
            EffortTypeId = "LAB",
            RoleId = 2,
            EffortValue = 30
        };

        _recordServiceMock.Setup(s => s.GetEffortRecordAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InstructorEffortRecordDto?)null);

        // Act
        var result = await _controller.UpdateRecord(999, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateRecord_ReturnsNotFound_WhenUserNotAuthorized()
    {
        // Arrange
        var existingRecord = CreateTestRecord();
        var request = new UpdateEffortRecordRequest
        {
            EffortTypeId = "LAB",
            RoleId = 2,
            EffortValue = 30
        };

        _recordServiceMock.Setup(s => s.GetEffortRecordAsync(TestRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRecord);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateRecord(TestRecordId, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region DeleteRecord Tests

    [Fact]
    public async Task DeleteRecord_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var existingRecord = CreateTestRecord();

        _recordServiceMock.Setup(s => s.GetEffortRecordAsync(TestRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRecord);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _recordServiceMock.Setup(s => s.CanEditTermAsync(TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _recordServiceMock.Setup(s => s.DeleteEffortRecordAsync(TestRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteRecord(TestRecordId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteRecord_ReturnsNotFound_WhenRecordDoesNotExist()
    {
        // Arrange
        _recordServiceMock.Setup(s => s.GetEffortRecordAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InstructorEffortRecordDto?)null);

        // Act
        var result = await _controller.DeleteRecord(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteRecord_ReturnsNotFound_WhenUserNotAuthorized()
    {
        // Arrange
        var existingRecord = CreateTestRecord();

        _recordServiceMock.Setup(s => s.GetEffortRecordAsync(TestRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRecord);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteRecord(TestRecordId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteRecord_ReturnsBadRequest_WhenTermNotEditable()
    {
        // Arrange
        var existingRecord = CreateTestRecord();

        _recordServiceMock.Setup(s => s.GetEffortRecordAsync(TestRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRecord);
        _permissionServiceMock.Setup(s => s.CanEditPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _recordServiceMock.Setup(s => s.CanEditTermAsync(TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteRecord(TestRecordId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetAvailableCourses Tests

    [Fact]
    public async Task GetAvailableCourses_ReturnsOk_WhenAuthorized()
    {
        // Arrange
        var courses = new AvailableCoursesDto
        {
            ExistingCourses = new List<CourseOptionDto>(),
            AllCourses = new List<CourseOptionDto>
            {
                new() { Id = 1, SubjCode = "VET", CrseNumb = "410", SeqNumb = "01", Label = "VET 410-01 (4 units)" }
            }
        };

        _permissionServiceMock.Setup(s => s.CanViewPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _recordServiceMock.Setup(s => s.GetAvailableCoursesAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courses);

        // Act
        var result = await _controller.GetAvailableCourses(TestPersonId, TestTermCode);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourses = Assert.IsType<AvailableCoursesDto>(okResult.Value);
        Assert.Single(returnedCourses.AllCourses);
    }

    [Fact]
    public async Task GetAvailableCourses_ReturnsNotFound_WhenUserNotAuthorized()
    {
        // Arrange
        _permissionServiceMock.Setup(s => s.CanViewPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetAvailableCourses(TestPersonId, TestTermCode);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region GetEffortTypes Tests

    [Fact]
    public async Task GetEffortTypes_ReturnsOk_WithEffortTypeList()
    {
        // Arrange
        var effortTypes = new List<EffortTypeOptionDto>
        {
            new() { Id = "LEC", Description = "Lecture" },
            new() { Id = "LAB", Description = "Laboratory" }
        };

        _recordServiceMock.Setup(s => s.GetEffortTypeOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(effortTypes);

        // Act
        var result = await _controller.GetEffortTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTypes = Assert.IsAssignableFrom<List<EffortTypeOptionDto>>(okResult.Value);
        Assert.Equal(2, returnedTypes.Count);
    }

    #endregion

    #region GetRoles Tests

    [Fact]
    public async Task GetRoles_ReturnsOk_WithRoleList()
    {
        // Arrange
        var roles = new List<RoleOptionDto>
        {
            new() { Id = 1, Description = "Instructor of Record" },
            new() { Id = 2, Description = "Co-Instructor" }
        };

        _recordServiceMock.Setup(s => s.GetRoleOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        // Act
        var result = await _controller.GetRoles();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRoles = Assert.IsAssignableFrom<List<RoleOptionDto>>(okResult.Value);
        Assert.Equal(2, returnedRoles.Count);
    }

    #endregion

    #region CanEditTerm Tests

    [Fact]
    public async Task CanEditTerm_ReturnsTrue_WhenTermEditable()
    {
        // Arrange
        _recordServiceMock.Setup(s => s.CanEditTermAsync(TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CanEditTerm(TestTermCode);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True((bool)okResult.Value!);
    }

    [Fact]
    public async Task CanEditTerm_ReturnsFalse_WhenTermNotEditable()
    {
        // Arrange
        _recordServiceMock.Setup(s => s.CanEditTermAsync(TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CanEditTerm(TestTermCode);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.False((bool)okResult.Value!);
    }

    #endregion

    #region UsesWeeks Tests

    [Fact]
    public void UsesWeeks_ReturnsCorrectValue()
    {
        // Arrange
        _recordServiceMock.Setup(s => s.UsesWeeks("CLI", TestTermCode))
            .Returns(true);

        // Act
        var result = _controller.UsesWeeks("CLI", TestTermCode);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True((bool)okResult.Value!);
    }

    #endregion
}
