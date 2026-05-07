using Microsoft.AspNetCore.Http;
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
using Web.Authorization;

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
        _controller = new EmergencyContactController(_service, _exportService, _rapsContext, _userHelper, _logger)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
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
        _userHelper.IsInRole(_rapsContext, studentUser, EmergencyContactPermissions.StudentRoleName).Returns(true);

        _service.CanEditAsync(100, studentUser.LoginId).Returns(true);
        _service.GetStudentContactDetailAsync(100, true)
            .Returns(new StudentContactDetailDto { PersonId = 100, FullName = "Self" });

        var result = await _controller.GetStudentContactDetail(100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetStudentContactDetail_StudentCaller_StripsUpdatedBy()
    {
        var studentUser = CreateStudentUser(100);
        _userHelper.GetCurrentUser().Returns(studentUser);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.Admin).Returns(false);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.SISAllStudents).Returns(false);
        _userHelper.IsInRole(_rapsContext, studentUser, EmergencyContactPermissions.StudentRoleName).Returns(true);

        _service.CanEditAsync(100, studentUser.LoginId).Returns(true);
        _service.GetStudentContactDetailAsync(100, true)
            .Returns(new StudentContactDetailDto
            {
                PersonId = 100,
                FullName = "Self",
                UpdatedBy = "admin-who-edited"
            });

        var result = await _controller.GetStudentContactDetail(100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<StudentContactDetailDto>(okResult.Value);
        Assert.False(dto.IsAdmin);
        Assert.Null(dto.UpdatedBy);
    }

    [Fact]
    public async Task GetStudentContactDetail_StudentWithoutEmergencyContactStudentPermission_CanStillViewOwnRecord()
    {
        // Simulates app-closed state: student retains STUDENTS_DVM role membership
        // but no longer has the EmergencyContactStudent permission. Read access
        // must still work so the read-only view page can render.
        var studentUser = CreateStudentUser(100);
        _userHelper.GetCurrentUser().Returns(studentUser);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.Admin).Returns(false);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.SISAllStudents).Returns(false);
        _userHelper.IsInRole(_rapsContext, studentUser, EmergencyContactPermissions.StudentRoleName).Returns(true);

        _service.CanEditAsync(100, studentUser.LoginId).Returns(false);
        _service.GetStudentContactDetailAsync(100, false)
            .Returns(new StudentContactDetailDto { PersonId = 100, FullName = "Self", CanEdit = false });

        var result = await _controller.GetStudentContactDetail(100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<StudentContactDetailDto>(okResult.Value);
        Assert.Equal(100, dto.PersonId);
        Assert.False(dto.CanEdit);
    }

    [Fact]
    public async Task GetStudentContactDetail_UserWithoutAnyRole_ReturnsForbid()
    {
        // Non-admin, non-SIS, non-DVM-student user attempting access is rejected at the role check
        var user = CreateAdminUser();
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, EmergencyContactPermissions.Admin).Returns(false);
        _userHelper.HasPermission(_rapsContext, user, EmergencyContactPermissions.SISAllStudents).Returns(false);
        _userHelper.IsInRole(_rapsContext, user, EmergencyContactPermissions.StudentRoleName).Returns(false);

        var result = await _controller.GetStudentContactDetail(100);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetStudentContactDetail_SisCaller_PreservesUpdatedBy()
    {
        var sisUser = CreateAdminUser();
        _userHelper.GetCurrentUser().Returns(sisUser);
        _userHelper.HasPermission(_rapsContext, sisUser, EmergencyContactPermissions.Admin).Returns(false);
        _userHelper.HasPermission(_rapsContext, sisUser, EmergencyContactPermissions.SISAllStudents).Returns(true);

        _service.CanEditAsync(100, sisUser.LoginId).Returns(false);
        _service.GetStudentContactDetailAsync(100, false)
            .Returns(new StudentContactDetailDto
            {
                PersonId = 100,
                FullName = "Student",
                UpdatedBy = "prior-admin"
            });

        var result = await _controller.GetStudentContactDetail(100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<StudentContactDetailDto>(okResult.Value);
        Assert.True(dto.IsAdmin);
        Assert.Equal("prior-admin", dto.UpdatedBy);
    }

    [Fact]
    public async Task GetStudentContactDetail_StudentCannotViewOtherRecord_ReturnsForbid()
    {
        var studentUser = CreateStudentUser(100);
        _userHelper.GetCurrentUser().Returns(studentUser);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.Admin).Returns(false);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.SISAllStudents).Returns(false);
        _userHelper.IsInRole(_rapsContext, studentUser, EmergencyContactPermissions.StudentRoleName).Returns(true);

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

    [Fact]
    public async Task UpdateStudentContact_ArgumentException_ReturnsValidationProblem()
    {
        var adminUser = CreateAdminUser();
        _userHelper.GetCurrentUser().Returns(adminUser);
        _service.CanEditAsync(100, adminUser.LoginId).Returns(true);
        _service.UpdateStudentContactAsync(100, Arg.Any<UpdateStudentContactRequest>(), Arg.Any<string>())
            .ThrowsAsync(new ArgumentException("Invalid phone number: 12345"));

        var request = new UpdateStudentContactRequest { ContactPermanent = false };
        var result = await _controller.UpdateStudentContact(100, request);

        // ValidationProblem returns ObjectResult with a ValidationProblemDetails body.
        // Status code is set by the MVC pipeline's ProblemDetailsFactory (400 in prod),
        // but in unit tests without an HttpContext it stays null — assert the body shape.
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.True(problem.Errors.ContainsKey("PhoneValidation"));
        Assert.Contains("Invalid phone number: 12345", problem.Errors["PhoneValidation"]);
    }

    [Fact]
    public async Task UpdateStudentContact_StudentCaller_StripsUpdatedBy()
    {
        var studentUser = CreateStudentUser(100);
        _userHelper.GetCurrentUser().Returns(studentUser);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.Admin).Returns(false);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.SISAllStudents).Returns(false);

        _service.CanEditAsync(100, studentUser.LoginId).Returns(true);
        _service.GetStudentContactDetailAsync(100, true)
            .Returns(new StudentContactDetailDto
            {
                PersonId = 100,
                FullName = "Self",
                UpdatedBy = "admin-who-edited"
            });

        var result = await _controller.UpdateStudentContact(100, new UpdateStudentContactRequest { ContactPermanent = false });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<StudentContactDetailDto>(okResult.Value);
        Assert.False(dto.IsAdmin);
        Assert.Null(dto.UpdatedBy);
    }

    [Fact]
    public async Task UpdateStudentContact_AdminCaller_PreservesUpdatedBy()
    {
        var adminUser = CreateAdminUser();
        _userHelper.GetCurrentUser().Returns(adminUser);
        _userHelper.HasPermission(_rapsContext, adminUser, EmergencyContactPermissions.Admin).Returns(true);
        _userHelper.HasPermission(_rapsContext, adminUser, EmergencyContactPermissions.SISAllStudents).Returns(false);

        _service.CanEditAsync(100, adminUser.LoginId).Returns(true);
        _service.GetStudentContactDetailAsync(100, true)
            .Returns(new StudentContactDetailDto
            {
                PersonId = 100,
                FullName = "Student",
                UpdatedBy = "prior-admin"
            });

        var result = await _controller.UpdateStudentContact(100, new UpdateStudentContactRequest { ContactPermanent = false });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<StudentContactDetailDto>(okResult.Value);
        Assert.True(dto.IsAdmin);
        Assert.Equal("prior-admin", dto.UpdatedBy);
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
        _userHelper.HasPermission(_rapsContext, user, EmergencyContactPermissions.Admin).Returns(true);
        _userHelper.HasPermission(_rapsContext, user, EmergencyContactPermissions.SISAllStudents).Returns(false);
        _userHelper.IsInRole(_rapsContext, user, EmergencyContactPermissions.StudentRoleName).Returns(false);
        _service.CanEditAsync(100, user.LoginId).Returns(true);

        var result = await _controller.CanEdit(100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True((bool)okResult.Value!);
    }

    [Fact]
    public async Task CanEdit_UserWithoutAnyRole_ReturnsForbid()
    {
        // Mirrors GetStudentContactDetail: caller with no admin/SIS/DVM-student gate is rejected
        // before the service is consulted. Guards against regressing the in-method authorization path.
        var user = CreateAdminUser();
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, EmergencyContactPermissions.Admin).Returns(false);
        _userHelper.HasPermission(_rapsContext, user, EmergencyContactPermissions.SISAllStudents).Returns(false);
        _userHelper.IsInRole(_rapsContext, user, EmergencyContactPermissions.StudentRoleName).Returns(false);

        var result = await _controller.CanEdit(100);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        await _service.DidNotReceive().CanEditAsync(Arg.Any<int>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task CanEdit_DvmStudentQueryingOtherStudent_ReturnsForbid()
    {
        // BOLA guard: a DVM student must not be able to probe editability for another student's PersonId.
        var studentUser = CreateStudentUser(100);
        _userHelper.GetCurrentUser().Returns(studentUser);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.Admin).Returns(false);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.SISAllStudents).Returns(false);
        _userHelper.IsInRole(_rapsContext, studentUser, EmergencyContactPermissions.StudentRoleName).Returns(true);

        var result = await _controller.CanEdit(999);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        await _service.DidNotReceive().CanEditAsync(Arg.Any<int>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task CanEdit_DvmStudentQueryingOwnRecord_ReturnsCanEditValue()
    {
        var studentUser = CreateStudentUser(100);
        _userHelper.GetCurrentUser().Returns(studentUser);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.Admin).Returns(false);
        _userHelper.HasPermission(_rapsContext, studentUser, EmergencyContactPermissions.SISAllStudents).Returns(false);
        _userHelper.IsInRole(_rapsContext, studentUser, EmergencyContactPermissions.StudentRoleName).Returns(true);
        _service.CanEditAsync(100, studentUser.LoginId).Returns(true);

        var result = await _controller.CanEdit(100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True((bool)okResult.Value!);
    }

    #endregion

    #region Export Tests

    [Fact]
    public async Task ExportOverviewExcel_WithData_ReturnsFile()
    {
        var data = new List<StudentContactListItemDto> { new() { PersonId = 1 } };
        _service.GetStudentContactListAsync().Returns(data);
        using var stream = new MemoryStream(new byte[] { 1 });
        _exportService.GenerateOverviewExcel(data).Returns(stream);

        var result = await _controller.ExportOverviewExcel();

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
    }

    [Fact]
    public async Task ExportOverviewExcel_Empty_ReturnsNoContent()
    {
        _service.GetStudentContactListAsync().Returns(new List<StudentContactListItemDto>());

        var result = await _controller.ExportOverviewExcel();

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ExportOverviewPdf_WithData_ReturnsFile()
    {
        var data = new List<StudentContactListItemDto> { new() { PersonId = 1 } };
        _service.GetStudentContactListAsync().Returns(data);
        _exportService.GenerateOverviewPdf(data).Returns(new byte[] { 1 });

        var result = await _controller.ExportOverviewPdf();

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
    }

    [Fact]
    public async Task ExportOverviewPdf_Empty_ReturnsNoContent()
    {
        _service.GetStudentContactListAsync().Returns(new List<StudentContactListItemDto>());

        var result = await _controller.ExportOverviewPdf();

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ExportExcel_WithData_ReturnsFile()
    {
        var data = new List<StudentContactReportDto> { new() { PersonId = 1 } };
        _service.GetStudentContactReportAsync().Returns(data);
        using var stream = new MemoryStream(new byte[] { 1 });
        _exportService.GenerateExcel(data).Returns(stream);

        var result = await _controller.ExportExcel();

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
    }

    [Fact]
    public async Task ExportExcel_Empty_ReturnsNoContent()
    {
        _service.GetStudentContactReportAsync().Returns(new List<StudentContactReportDto>());

        var result = await _controller.ExportExcel();

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ExportPdf_WithData_ReturnsInlineFile()
    {
        var data = new List<StudentContactReportDto> { new() { PersonId = 1 } };
        _service.GetStudentContactReportAsync().Returns(data);
        _exportService.GeneratePdf(data).Returns(new byte[] { 1 });

        var result = await _controller.ExportPdf();

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        // InlineFile must set Content-Disposition: inline with a filename so the browser
        // renders the PDF in its built-in viewer instead of forcing a download.
        var disposition = _controller.Response.Headers.ContentDisposition.ToString();
        Assert.StartsWith("inline", disposition);
        Assert.Contains("EmergencyContacts_", disposition);
    }

    [Fact]
    public async Task ExportPdf_Empty_ReturnsNoContent()
    {
        _service.GetStudentContactReportAsync().Returns(new List<StudentContactReportDto>());

        var result = await _controller.ExportPdf();

        Assert.IsType<NoContentResult>(result);
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

    [Fact]
    public void GetStudentContactDetail_HasAuthorizeAttributeButNoPermissionGate()
    {
        // Gate is role-based (STUDENTS_DVM), enforced inside the method — students
        // don't have any SVMSecure.Students.* permission when the app is closed,
        // so the [Permission] attribute can't filter them and [Authorize] is used instead.
        var method = typeof(EmergencyContactController).GetMethod(nameof(EmergencyContactController.GetStudentContactDetail));
        Assert.NotNull(method);

        var permissionAttrs = method!.GetCustomAttributes(typeof(PermissionAttribute), false);
        Assert.Empty(permissionAttrs);

        var authorizeAttrs = method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
        Assert.NotEmpty(authorizeAttrs);
    }

    [Fact]
    public void CanEdit_HasAuthorizeAttributeButNoPermissionGate()
    {
        // Mirrors GetStudentContactDetail — role-based gate enforced inside the method.
        var method = typeof(EmergencyContactController).GetMethod(nameof(EmergencyContactController.CanEdit));
        Assert.NotNull(method);

        var permissionAttrs = method!.GetCustomAttributes(typeof(PermissionAttribute), false);
        Assert.Empty(permissionAttrs);

        var authorizeAttrs = method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
        Assert.NotEmpty(authorizeAttrs);
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
