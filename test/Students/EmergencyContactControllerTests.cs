using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Viper.Areas.Students.Constants;
using Viper.Areas.Students.Controllers;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.Students;

/// <summary>
/// Tests for EmergencyContactController: permission enforcement, response codes,
/// and correct delegation to the service layer.
/// </summary>
public class EmergencyContactControllerTests
{
    private readonly IEmergencyContactService _service;
    private readonly IEmergencyContactExportService _exportService;
    private readonly RAPSContext _rapsContext;
    private readonly IUserHelper _userHelper;
    private readonly ILogger<EmergencyContactController> _logger;
    private readonly EmergencyContactController _controller;

    public EmergencyContactControllerTests()
    {
        _service = Substitute.For<IEmergencyContactService>();
        _exportService = Substitute.For<IEmergencyContactExportService>();
        _rapsContext = Substitute.For<RAPSContext>();
        _userHelper = Substitute.For<IUserHelper>();
        _logger = Substitute.For<ILogger<EmergencyContactController>>();
        _controller = new EmergencyContactController(_service, _exportService, _rapsContext, _userHelper, _logger);
    }

    #region GetStudentContactList Tests

    [Fact]
    public async Task GetStudentContactList_ReturnsOk()
    {
        _service.GetStudentContactListAsync()
            .Returns(new List<StudentContactListItemDto>
            {
                new() { PersonId = 1, FullName = "Test Student" }
            });

        var result = await _controller.GetStudentContactList();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<StudentContactListItemDto>>(okResult.Value);
        Assert.Single(list);
    }

    #endregion

    #region GetStudentContactDetail Tests

    [Fact]
    public async Task GetStudentContactDetail_NoUser_ReturnsUnauthorized()
    {
        _userHelper.GetCurrentUser().Returns((AaudUser?)null);

        var result = await _controller.GetStudentContactDetail(1);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetStudentContactDetail_AdminCanViewAnyStudent()
    {
        var adminUser = CreateAdminUser();
        _userHelper.GetCurrentUser().Returns(adminUser);
        _userHelper.HasPermission(_rapsContext, adminUser, EmergencyContactPermissions.Admin).Returns(true);
        _userHelper.HasPermission(_rapsContext, adminUser, EmergencyContactPermissions.SISAllStudents).Returns(false);

        _service.CanEditAsync(999, adminUser.LoginId).Returns(true);
        _service.GetStudentContactDetailAsync(999, true)
            .Returns(new StudentContactDetailDto { PersonId = 999, FullName = "Student" });

        var result = await _controller.GetStudentContactDetail(999);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<StudentContactDetailDto>(okResult.Value);
        Assert.Equal(999, dto.PersonId);
    }

    [Fact]
    public async Task GetStudentContactDetail_StudentCanViewOwnRecord()
    {
        var studentUser = CreateStudentUser(100);
        _userHelper.GetCurrentUser().Returns(studentUser);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.Admin).Returns(false);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.SISAllStudents).Returns(false);

        _service.CanEditAsync(100, studentUser.LoginId).Returns(true);
        _service.GetStudentContactDetailAsync(100, true)
            .Returns(new StudentContactDetailDto { PersonId = 100, FullName = "Self" });

        var result = await _controller.GetStudentContactDetail(100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetStudentContactDetail_StudentCannotViewOtherRecord_ReturnsForbid()
    {
        var studentUser = CreateStudentUser(100);
        _userHelper.GetCurrentUser().Returns(studentUser);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.Admin).Returns(false);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.SISAllStudents).Returns(false);

        var result = await _controller.GetStudentContactDetail(999);

        Assert.IsType<ObjectResult>(result.Result);
        var objectResult = (ObjectResult)result.Result!;
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetStudentContactDetail_NotFound_Returns404()
    {
        var adminUser = CreateAdminUser();
        _userHelper.GetCurrentUser().Returns(adminUser);
        _userHelper.HasPermission(_rapsContext, adminUser, EmergencyContactPermissions.Admin).Returns(true);
        _userHelper.HasPermission(_rapsContext, adminUser, EmergencyContactPermissions.SISAllStudents).Returns(false);

        _service.CanEditAsync(999, adminUser.LoginId).Returns(true);
        _service.GetStudentContactDetailAsync(999, true).Returns((StudentContactDetailDto?)null);

        var result = await _controller.GetStudentContactDetail(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region UpdateStudentContact Tests

    [Fact]
    public async Task UpdateStudentContact_NoUser_ReturnsUnauthorized()
    {
        _userHelper.GetCurrentUser().Returns((AaudUser?)null);

        var result = await _controller.UpdateStudentContact(1, new UpdateStudentContactRequest { ContactPermanent = false });

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStudentContact_NotPermitted_Returns403()
    {
        var studentUser = CreateStudentUser(100);
        _userHelper.GetCurrentUser().Returns(studentUser);
        _service.CanEditAsync(999, studentUser.LoginId).Returns(false);

        var result = await _controller.UpdateStudentContact(999, new UpdateStudentContactRequest { ContactPermanent = false });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public async Task UpdateStudentContact_Permitted_ReturnsUpdatedDetail()
    {
        var adminUser = CreateAdminUser();
        _userHelper.GetCurrentUser().Returns(adminUser);
        _service.CanEditAsync(100, adminUser.LoginId).Returns(true);
        _service.GetStudentContactDetailAsync(100, true)
            .Returns(new StudentContactDetailDto
            {
                PersonId = 100,
                FullName = "Test Student",
                CanEdit = true,
                LastUpdated = DateTime.Now
            });

        var request = new UpdateStudentContactRequest { ContactPermanent = false };
        var result = await _controller.UpdateStudentContact(100, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<StudentContactDetailDto>(okResult.Value);
        Assert.Equal(100, dto.PersonId);
        Assert.True(dto.CanEdit);
        await _service.Received(1).UpdateStudentContactAsync(100, request, adminUser.LoginId!);
    }

    [Fact]
    public async Task UpdateStudentContact_NoPidm_Returns404()
    {
        var adminUser = CreateAdminUser();
        _userHelper.GetCurrentUser().Returns(adminUser);
        _service.CanEditAsync(100, adminUser.LoginId).Returns(true);
        _service.UpdateStudentContactAsync(100, Arg.Any<UpdateStudentContactRequest>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("No PIDM found"));

        var request = new UpdateStudentContactRequest { ContactPermanent = false };
        var result = await _controller.UpdateStudentContact(100, request);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetStudentContactReport Tests

    [Fact]
    public async Task GetStudentContactReport_ReturnsOk()
    {
        _service.GetStudentContactReportAsync()
            .Returns(new List<StudentContactReportDto>());

        var result = await _controller.GetStudentContactReport();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    #endregion

    #region GetAccessStatus Tests

    [Fact]
    public async Task GetAccessStatus_ReturnsOk()
    {
        _service.GetAccessStatusAsync()
            .Returns(new AppAccessStatusDto { AppOpen = true });

        var result = await _controller.GetAccessStatus();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AppAccessStatusDto>(okResult.Value);
        Assert.True(dto.AppOpen);
    }

    #endregion

    #region ToggleAppAccess Tests

    [Fact]
    public async Task ToggleAppAccess_ReturnsNewState()
    {
        _service.ToggleAppAccessAsync().Returns(true);

        var result = await _controller.ToggleAppAccess();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True((bool)okResult.Value!);
    }

    #endregion

    #region ToggleIndividualAccess Tests

    [Fact]
    public async Task ToggleIndividualAccess_ReturnsNewState()
    {
        _service.ToggleIndividualAccessAsync(100).Returns(true);

        var result = await _controller.ToggleIndividualAccess(100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True((bool)okResult.Value!);
    }

    [Fact]
    public async Task ToggleIndividualAccess_UserNotFound_Returns404()
    {
        _service.ToggleIndividualAccessAsync(99999)
            .ThrowsAsync(new InvalidOperationException("No user found"));

        var result = await _controller.ToggleIndividualAccess(99999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region CanEdit Tests

    [Fact]
    public async Task CanEdit_NoUser_ReturnsUnauthorized()
    {
        _userHelper.GetCurrentUser().Returns((AaudUser?)null);

        var result = await _controller.CanEdit(1);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task CanEdit_ReturnsCanEditValue()
    {
        var user = CreateAdminUser();
        _userHelper.GetCurrentUser().Returns(user);
        _service.CanEditAsync(100, user.LoginId).Returns(true);

        var result = await _controller.CanEdit(100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True((bool)okResult.Value!);
    }

    #endregion

    #region Route Attribute Tests

    [Fact]
    public void Controller_HasCorrectRouteAttribute()
    {
        var controllerType = typeof(EmergencyContactController);
        var routeAttributes = controllerType.GetCustomAttributes(typeof(RouteAttribute), false);

        Assert.NotEmpty(routeAttributes);
        var routeAttr = (RouteAttribute)routeAttributes[0];
        Assert.Equal("/api/students/emergency-contacts", routeAttr.Template);
    }

    [Fact]
    public void Controller_DoesNotHaveAreaAttribute()
    {
        // Per CLAUDE.md: Never use [Area] on API controllers
        var controllerType = typeof(EmergencyContactController);
        var areaAttributes = controllerType.GetCustomAttributes(typeof(AreaAttribute), false);
        Assert.Empty(areaAttributes);
    }

    #endregion

    #region Helper Methods

    private static AaudUser CreateAdminUser()
    {
        return new AaudUser
        {
            AaudUserId = 1,
            ClientId = "UCD",
            MothraId = "ADMIN001",
            LoginId = "admin",
            DisplayFullName = "Admin User",
            DisplayFirstName = "Admin",
            DisplayLastName = "User",
            LastName = "User",
            FirstName = "Admin"
        };
    }

    private static AaudUser CreateStudentUser(int personId)
    {
        return new AaudUser
        {
            AaudUserId = personId,
            ClientId = "UCD",
            MothraId = $"STU{personId}",
            LoginId = $"student{personId}",
            DisplayFullName = $"Student {personId}",
            DisplayFirstName = "Student",
            DisplayLastName = $"{personId}",
            LastName = $"{personId}",
            FirstName = "Student"
        };
    }

    #endregion
}
