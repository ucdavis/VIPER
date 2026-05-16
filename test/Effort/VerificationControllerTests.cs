using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Viper.Areas.Effort;
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
    private readonly IVerificationService _verificationServiceMock;
    private readonly IEffortPermissionService _permissionServiceMock;
    private readonly IInstructorService _instructorServiceMock;
    private readonly IOptions<EffortSettings> _settingsMock;
    private readonly ILogger<VerificationController> _loggerMock;
    private readonly VerificationController _controller;

    public VerificationControllerTests()
    {
        _verificationServiceMock = Substitute.For<IVerificationService>();
        _permissionServiceMock = Substitute.For<IEffortPermissionService>();
        _instructorServiceMock = Substitute.For<IInstructorService>();
        _settingsMock = Substitute.For<IOptions<EffortSettings>>();
        _settingsMock.Value.Returns(new EffortSettings());
        _loggerMock = Substitute.For<ILogger<VerificationController>>();

        _controller = new VerificationController(
            _verificationServiceMock,
            _permissionServiceMock,
            _instructorServiceMock,
            _settingsMock,
            _loggerMock);

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
            LastModifiedDate = DateTime.Now
        };
        _verificationServiceMock.GetMyEffortAsync(202410, Arg.Any<CancellationToken>()).Returns(myEffort);

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
        _verificationServiceMock.GetMyEffortAsync(202410, Arg.Any<CancellationToken>()).ReturnsNull();

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
        _verificationServiceMock.VerifyEffortAsync(202410, Arg.Any<CancellationToken>()).Returns(result);

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
        _verificationServiceMock.VerifyEffortAsync(202410, Arg.Any<CancellationToken>()).Returns(result);

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

        _instructorServiceMock.GetInstructorAsync(123, 202410, Arg.Any<CancellationToken>()).Returns(instructor);
        _permissionServiceMock.CanViewDepartmentAsync("DVM", Arg.Any<CancellationToken>()).Returns(true);
        _verificationServiceMock.SendVerificationEmailAsync(123, 202410, Arg.Any<CancellationToken>()).Returns(emailResult);

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

        _instructorServiceMock.GetInstructorAsync(999, 202410, Arg.Any<CancellationToken>()).ReturnsNull();

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

        _instructorServiceMock.GetInstructorAsync(123, 202410, Arg.Any<CancellationToken>()).Returns(instructor);
        _permissionServiceMock.CanViewDepartmentAsync("VME", Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _controller.SendVerificationEmail(request);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task SendVerificationEmail_ReturnsConflict_WhenInstructorAlreadyVerified()
    {
        // Arrange
        var request = new SendVerificationEmailRequest { PersonId = 123, TermCode = 202410 };
        var instructor = new PersonDto { PersonId = 123, EffortDept = "DVM", EffortVerified = DateTime.Now };

        _instructorServiceMock.GetInstructorAsync(123, 202410, Arg.Any<CancellationToken>()).Returns(instructor);
        _permissionServiceMock.CanViewDepartmentAsync("DVM", Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _controller.SendVerificationEmail(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("Instructor has already verified their effort.", conflictResult.Value);
    }

    #endregion

    #region SendBulkVerificationEmails Tests

    [Fact]
    public async Task SendBulkVerificationEmails_ReturnsOk_WhenAuthorized()
    {
        // Arrange
        var request = new SendBulkEmailRequest { DepartmentCode = "DVM", TermCode = 202410 };
        var bulkResult = new BulkEmailResult { TotalInstructors = 5, EmailsSent = 5, EmailsFailed = 0 };

        _permissionServiceMock.CanViewDepartmentAsync("DVM", Arg.Any<CancellationToken>()).Returns(true);
        _verificationServiceMock.SendBulkVerificationEmailsAsync("DVM", 202410, false, Arg.Any<CancellationToken>()).Returns(bulkResult);

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

        _permissionServiceMock.CanViewDepartmentAsync("VME", Arg.Any<CancellationToken>()).Returns(false);

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
            new() { SentDate = DateTime.Now.AddDays(-1), SentBy = "Admin" },
            new() { SentDate = DateTime.Now.AddDays(-7), SentBy = "Admin" }
        };

        _instructorServiceMock.GetInstructorAsync(123, 202410, Arg.Any<CancellationToken>()).Returns(instructor);
        _permissionServiceMock.CanViewDepartmentAsync("DVM", Arg.Any<CancellationToken>()).Returns(true);
        _verificationServiceMock.GetEmailHistoryAsync(123, 202410, Arg.Any<CancellationToken>()).Returns(history);

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
        _instructorServiceMock.GetInstructorAsync(999, 202410, Arg.Any<CancellationToken>()).ReturnsNull();

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

        _instructorServiceMock.GetInstructorAsync(123, 202410, Arg.Any<CancellationToken>()).Returns(instructor);
        _permissionServiceMock.CanViewDepartmentAsync("VME", Arg.Any<CancellationToken>()).Returns(false);

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

        _permissionServiceMock.GetCurrentPersonId().Returns(123);
        _verificationServiceMock.CanVerifyAsync(123, 202410, Arg.Any<CancellationToken>()).Returns(canVerifyResult);

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

        _permissionServiceMock.GetCurrentPersonId().Returns(123); // Different person
        _instructorServiceMock.GetInstructorAsync(456, 202410, Arg.Any<CancellationToken>()).Returns(instructor);
        _permissionServiceMock.CanViewDepartmentAsync("DVM", Arg.Any<CancellationToken>()).Returns(true);
        _verificationServiceMock.CanVerifyAsync(456, 202410, Arg.Any<CancellationToken>()).Returns(canVerifyResult);

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
        _permissionServiceMock.GetCurrentPersonId().Returns(123); // Different person
        _instructorServiceMock.GetInstructorAsync(456, 202410, Arg.Any<CancellationToken>()).ReturnsNull();

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

        _permissionServiceMock.GetCurrentPersonId().Returns(123); // Different person
        _instructorServiceMock.GetInstructorAsync(456, 202410, Arg.Any<CancellationToken>()).Returns(instructor);
        _permissionServiceMock.CanViewDepartmentAsync("VME", Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _controller.CanVerify(456, 202410);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    #endregion
}
