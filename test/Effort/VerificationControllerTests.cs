using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort.Controllers;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for VerificationController API endpoints.
/// </summary>
public sealed class VerificationControllerTests
{
    private readonly Mock<IVerificationService> _verificationServiceMock;
    private readonly Mock<IEffortPermissionService> _permissionServiceMock;
    private readonly Mock<IInstructorService> _instructorServiceMock;
    private readonly Mock<ILogger<VerificationController>> _loggerMock;
    private readonly VerificationController _controller;

    public VerificationControllerTests()
    {
        _verificationServiceMock = new Mock<IVerificationService>();
        _permissionServiceMock = new Mock<IEffortPermissionService>();
        _instructorServiceMock = new Mock<IInstructorService>();
        _loggerMock = new Mock<ILogger<VerificationController>>();

        _controller = new VerificationController(
            _verificationServiceMock.Object,
            _permissionServiceMock.Object,
            _instructorServiceMock.Object,
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

    #region GetMyEffort Tests

    [Fact]
    public async Task GetMyEffort_ReturnsOk_WithEffortData()
    {
        // Arrange
        var myEffort = new MyEffortDto
        {
            Instructor = new PersonDto { PersonId = 123, FirstName = "John", LastName = "Doe" },
            EffortRecords = [new InstructorEffortRecordDto { Id = 1, Hours = 10 }],
            CrossListedCourses = [],
            HasZeroEffort = false,
            ZeroEffortRecordIds = [],
            CanVerify = true,
            CanEdit = true,
            HasVerifyPermission = true,
            TermName = "Fall 2024",
            LastModifiedDate = DateTime.UtcNow
        };
        _verificationServiceMock.Setup(s => s.GetMyEffortAsync(202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(myEffort);

        // Act
        var result = await _controller.GetMyEffort(202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEffort = Assert.IsType<MyEffortDto>(okResult.Value);
        Assert.Equal(123, returnedEffort.Instructor.PersonId);
        Assert.True(returnedEffort.CanVerify);
    }

    [Fact]
    public async Task GetMyEffort_ReturnsEmptyDto_WhenNoInstructorRecord()
    {
        // Arrange
        _verificationServiceMock.Setup(s => s.GetMyEffortAsync(202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MyEffortDto?)null);

        // Act
        var result = await _controller.GetMyEffort(202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEffort = Assert.IsType<MyEffortDto>(okResult.Value);
        Assert.Empty(returnedEffort.EffortRecords);
        Assert.False(returnedEffort.CanVerify);
        Assert.Equal("", returnedEffort.TermName);
    }

    #endregion

    #region VerifyEffort Tests

    [Fact]
    public async Task VerifyEffort_ReturnsOk_WithSuccessResult()
    {
        // Arrange
        var result = new VerificationResult { Success = true };
        _verificationServiceMock.Setup(s => s.VerifyEffortAsync(202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.VerifyEffort(202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedResult = Assert.IsType<VerificationResult>(okResult.Value);
        Assert.True(returnedResult.Success);
    }

    [Fact]
    public async Task VerifyEffort_ReturnsOk_WithErrorDetails_WhenVerificationFails()
    {
        // Arrange
        var result = new VerificationResult { Success = false, ErrorMessage = "Zero effort records exist" };
        _verificationServiceMock.Setup(s => s.VerifyEffortAsync(202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.VerifyEffort(202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedResult = Assert.IsType<VerificationResult>(okResult.Value);
        Assert.False(returnedResult.Success);
        Assert.Equal("Zero effort records exist", returnedResult.ErrorMessage);
    }

    #endregion

    #region SendVerificationEmail Tests

    [Fact]
    public async Task SendVerificationEmail_ReturnsOk_WhenEmailSent()
    {
        // Arrange
        var request = new SendVerificationEmailRequest { PersonId = 123, TermCode = 202410 };
        var instructor = new PersonDto { PersonId = 123, EffortDept = "DVM" };
        var emailResult = new EmailSendResult { Success = true };

        _instructorServiceMock.Setup(s => s.GetInstructorAsync(123, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _verificationServiceMock.Setup(s => s.SendVerificationEmailAsync(123, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailResult);

        // Act
        var result = await _controller.SendVerificationEmail(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<EmailSendResult>(okResult.Value);
        Assert.True(returnedResult.Success);
    }

    [Fact]
    public async Task SendVerificationEmail_ReturnsNotFound_WhenInstructorNotFound()
    {
        // Arrange
        var request = new SendVerificationEmailRequest { PersonId = 999, TermCode = 202410 };

        _instructorServiceMock.Setup(s => s.GetInstructorAsync(999, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDto?)null);

        // Act
        var result = await _controller.SendVerificationEmail(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task SendVerificationEmail_ReturnsForbid_WhenNoPermission()
    {
        // Arrange
        var request = new SendVerificationEmailRequest { PersonId = 123, TermCode = 202410 };
        var instructor = new PersonDto { PersonId = 123, EffortDept = "VME" };

        _instructorServiceMock.Setup(s => s.GetInstructorAsync(123, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.SendVerificationEmail(request);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    #endregion

    #region SendBulkVerificationEmails Tests

    [Fact]
    public async Task SendBulkVerificationEmails_ReturnsOk_WhenAuthorized()
    {
        // Arrange
        var request = new SendBulkEmailRequest { DepartmentCode = "DVM", TermCode = 202410 };
        var bulkResult = new BulkEmailResult { TotalInstructors = 5, EmailsSent = 5, EmailsFailed = 0 };

        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _verificationServiceMock.Setup(s => s.SendBulkVerificationEmailsAsync("DVM", 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.SendBulkVerificationEmails(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<BulkEmailResult>(okResult.Value);
        Assert.Equal(5, returnedResult.EmailsSent);
        Assert.Equal(0, returnedResult.EmailsFailed);
    }

    [Fact]
    public async Task SendBulkVerificationEmails_ReturnsForbid_WhenNoPermission()
    {
        // Arrange
        var request = new SendBulkEmailRequest { DepartmentCode = "VME", TermCode = 202410 };

        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.SendBulkVerificationEmails(request);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    #endregion

    #region GetEmailHistory Tests

    [Fact]
    public async Task GetEmailHistory_ReturnsOk_WithHistory()
    {
        // Arrange
        var instructor = new PersonDto { PersonId = 123, EffortDept = "DVM" };
        var history = new List<EmailHistoryDto>
        {
            new() { SentDate = DateTime.UtcNow.AddDays(-1), SentBy = "Admin" },
            new() { SentDate = DateTime.UtcNow.AddDays(-7), SentBy = "Admin" }
        };

        _instructorServiceMock.Setup(s => s.GetInstructorAsync(123, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _verificationServiceMock.Setup(s => s.GetEmailHistoryAsync(123, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.GetEmailHistory(123, 202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedHistory = Assert.IsType<List<EmailHistoryDto>>(okResult.Value);
        Assert.Equal(2, returnedHistory.Count);
    }

    [Fact]
    public async Task GetEmailHistory_ReturnsNotFound_WhenInstructorNotFound()
    {
        // Arrange
        _instructorServiceMock.Setup(s => s.GetInstructorAsync(999, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDto?)null);

        // Act
        var result = await _controller.GetEmailHistory(999, 202410);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetEmailHistory_ReturnsForbid_WhenNoPermission()
    {
        // Arrange
        var instructor = new PersonDto { PersonId = 123, EffortDept = "VME" };

        _instructorServiceMock.Setup(s => s.GetInstructorAsync(123, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetEmailHistory(123, 202410);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    #endregion

    #region CanVerify Tests

    [Fact]
    public async Task CanVerify_ReturnsOk_ForSelfCheck()
    {
        // Arrange
        var canVerifyResult = new CanVerifyResult { CanVerify = true, ZeroEffortCount = 0 };

        _permissionServiceMock.Setup(s => s.GetCurrentPersonId()).Returns(123);
        _verificationServiceMock.Setup(s => s.CanVerifyAsync(123, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(canVerifyResult);

        // Act
        var result = await _controller.CanVerify(123, 202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<CanVerifyResult>(okResult.Value);
        Assert.True(returnedResult.CanVerify);
    }

    [Fact]
    public async Task CanVerify_ReturnsOk_ForAdminCheck_WhenAuthorized()
    {
        // Arrange
        var instructor = new PersonDto { PersonId = 456, EffortDept = "DVM" };
        var canVerifyResult = new CanVerifyResult { CanVerify = false, ZeroEffortCount = 2, ZeroEffortCourses = ["DVM 443-001", "VME 200-001"] };

        _permissionServiceMock.Setup(s => s.GetCurrentPersonId()).Returns(123); // Different person
        _instructorServiceMock.Setup(s => s.GetInstructorAsync(456, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("DVM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _verificationServiceMock.Setup(s => s.CanVerifyAsync(456, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(canVerifyResult);

        // Act
        var result = await _controller.CanVerify(456, 202410);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<CanVerifyResult>(okResult.Value);
        Assert.False(returnedResult.CanVerify);
        Assert.Equal(2, returnedResult.ZeroEffortCount);
    }

    [Fact]
    public async Task CanVerify_ReturnsNotFound_WhenInstructorNotFound()
    {
        // Arrange
        _permissionServiceMock.Setup(s => s.GetCurrentPersonId()).Returns(123); // Different person
        _instructorServiceMock.Setup(s => s.GetInstructorAsync(456, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDto?)null);

        // Act
        var result = await _controller.CanVerify(456, 202410);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CanVerify_ReturnsForbid_WhenNoPermission()
    {
        // Arrange
        var instructor = new PersonDto { PersonId = 456, EffortDept = "VME" };

        _permissionServiceMock.Setup(s => s.GetCurrentPersonId()).Returns(123); // Different person
        _instructorServiceMock.Setup(s => s.GetInstructorAsync(456, 202410, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instructor);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CanVerify(456, 202410);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    #endregion
}
