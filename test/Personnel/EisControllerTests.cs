using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Viper.Areas.Personnel.Controllers;
using Viper.Areas.Personnel.Models.Eis;
using Viper.Areas.Personnel.Services;
using Viper.Areas.Students.Services;
using static Viper.test.Personnel.EisTestData;

namespace Viper.test.Personnel;

/// <summary>
/// EisController checks access to the employee before every person request, so a department
/// user can't reach someone outside their units by changing the URL.
/// </summary>
public sealed class EisControllerTests
{
    private static readonly byte[] PhotoBytes = [1, 2, 3];

    private readonly IEisService _eis = Substitute.For<IEisService>();
    private readonly IPhotoService _photos = Substitute.For<IPhotoService>();
    private readonly EisController _controller;

    public EisControllerTests()
    {
        var userHelper = Substitute.For<IUserHelper>();
        userHelper.GetCurrentUser().Returns(User());
        _controller = new EisController(_eis, _photos, userHelper, NullLogger<EisController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private void Allow(bool canView = true)
    {
        _eis.CanViewAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(canView);
    }

    [Fact]
    public async Task GetPeople_ReturnsTheServiceList()
    {
        _eis.GetPeopleAsync(Arg.Any<CancellationToken>()).Returns([new EisPersonOption(EmployeeId, "Lovelace, Ada")]);

        ActionResult<IReadOnlyList<EisPersonOption>> result = await _controller.GetPeople(Ct);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<EisPersonOption>>(ok.Value));
    }

    [Fact]
    public async Task GetHeader_ReturnsTheHeaderForAViewableEmployee()
    {
        Allow();
        var header = new EisPersonHeader { EmployeeId = EmployeeId, Name = "Lovelace, Ada" };
        _eis.GetHeaderAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(header);

        ActionResult<EisPersonHeader> result = await _controller.GetHeader(EmployeeId, Ct);

        Assert.Same(header, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task GetHeader_UnknownEmployee_IsNotFound()
    {
        Allow();
        _eis.GetHeaderAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns((EisPersonHeader?)null);

        ActionResult<EisPersonHeader> result = await _controller.GetHeader(EmployeeId, Ct);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task PersonRequests_WithAMalformedId_AreNotFoundWithoutAnAccessCheck()
    {
        Assert.IsType<NotFoundObjectResult>((await _controller.GetHeader("../1", Ct)).Result);
        Assert.IsType<NotFoundResult>(await _controller.GetPhoto("abc", Ct));

        await _eis.DidNotReceive().CanViewAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PersonRequests_ForAnEmployeeOutsideTheUsersUnits_AreForbidden()
    {
        Allow(canView: false);

        Assert.Equal(StatusCodes.Status403Forbidden, StatusOf((await _controller.GetHeader(EmployeeId, Ct)).Result));
        Assert.Equal(StatusCodes.Status403Forbidden, StatusOf((await _controller.GetAppointments(EmployeeId, Ct)).Result));
        Assert.Equal(StatusCodes.Status403Forbidden, StatusOf((await _controller.GetHistory(EmployeeId, Ct)).Result));
        Assert.Equal(StatusCodes.Status403Forbidden, StatusOf((await _controller.GetAddress(EmployeeId, Ct)).Result));
        await _eis.DidNotReceive().GetAppointmentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PageRequests_ReturnTheServiceData()
    {
        Allow();
        var appointments = new EisAppointments([], [], 0m);
        var history = new EisHistory([], [], [], []);
        var address = new EisAddress(null, null, [], CampusDirectoryAvailable: true);
        _eis.GetAppointmentsAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(appointments);
        _eis.GetHistoryAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(history);
        _eis.GetAddressAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(address);

        Assert.Same(appointments, ValueOf((await _controller.GetAppointments(EmployeeId, Ct)).Result));
        Assert.Same(history, ValueOf((await _controller.GetHistory(EmployeeId, Ct)).Result));
        Assert.Same(address, ValueOf((await _controller.GetAddress(EmployeeId, Ct)).Result));
    }

    [Fact]
    public async Task GetPhoto_ReturnsTheIdCardPhoto()
    {
        Allow();
        _eis.GetMailIdAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns("aalovelace");
        _photos.GetStudentPhotoAsync("aalovelace").Returns(PhotoBytes);

        ActionResult result = await _controller.GetPhoto(EmployeeId, Ct);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/jpeg", file.ContentType);
        Assert.Equal(PhotoBytes, file.FileContents);
    }

    [Fact]
    public async Task GetPhoto_WithoutAMailId_AsksForTheDefaultPhoto()
    {
        Allow();
        _eis.GetMailIdAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns((string?)null);
        _photos.GetStudentPhotoAsync(string.Empty).Returns(PhotoBytes);

        Assert.IsType<FileContentResult>(await _controller.GetPhoto(EmployeeId, Ct));
    }

    [Fact]
    public async Task GetPhoto_ForAnEmployeeTheUserCannotView_IsNotFound()
    {
        Allow(canView: false);

        Assert.IsType<NotFoundResult>(await _controller.GetPhoto(EmployeeId, Ct));
        await _photos.DidNotReceive().GetStudentPhotoAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task MyInfoVaultAndCategoryPages_ReturnTheServiceData()
    {
        Allow();
        var academics = new EisAcademics(false, [], [], [], [], [], []);
        var categories = new EisAppointmentCategories([], "2026-2027", CanEditFlags: false);
        _eis.GetAcademicsAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(academics);
        _eis.GetAppointmentCategoriesAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(categories);

        Assert.Same(academics, ValueOf((await _controller.GetAcademics(EmployeeId, Ct)).Result));
        Assert.Same(categories, ValueOf((await _controller.GetCategories(EmployeeId, Ct)).Result));
    }

    [Fact]
    public async Task MyInfoVaultAndCategoryPages_ForAnEmployeeTheUserCannotView_AreForbidden()
    {
        Allow(canView: false);

        Assert.Equal(StatusCodes.Status403Forbidden, StatusOf((await _controller.GetAcademics(EmployeeId, Ct)).Result));
        Assert.Equal(StatusCodes.Status403Forbidden, StatusOf((await _controller.GetCategories(EmployeeId, Ct)).Result));
        Assert.Equal(StatusCodes.Status403Forbidden, StatusOf((await _controller.SetFlag(EmployeeId, 1, Ct)).Result));
        await _eis.DidNotReceive().SetFlagAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAndClearFlag_ReturnTheUpdatedCategories()
    {
        Allow();
        var categories = new EisAppointmentCategories([], "2026-2027", CanEditFlags: true);
        _eis.SetFlagAsync(EmployeeId, 4, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(categories);

        Assert.Same(categories, ValueOf((await _controller.SetFlag(EmployeeId, 4, Ct)).Result));
        Assert.Same(categories, ValueOf((await _controller.ClearFlag(EmployeeId, 4, Ct)).Result));
        await _eis.Received(1).SetFlagAsync(EmployeeId, 4, true, Arg.Any<CancellationToken>());
        await _eis.Received(1).SetFlagAsync(EmployeeId, 4, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetFlag_UnknownCategory_IsNotFound()
    {
        Allow();

        Assert.IsType<NotFoundObjectResult>((await _controller.SetFlag(EmployeeId, 10, Ct)).Result);
        await _eis.DidNotReceive().SetFlagAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetFlag_WhenTheServiceRefuses_IsForbidden()
    {
        Allow();
        _eis.SetFlagAsync(EmployeeId, 4, true, Arg.Any<CancellationToken>()).Returns((EisAppointmentCategories?)null);

        Assert.Equal(StatusCodes.Status403Forbidden, StatusOf((await _controller.SetFlag(EmployeeId, 4, Ct)).Result));
    }

    private static int? StatusOf(IActionResult? result)
    {
        return Assert.IsAssignableFrom<ObjectResult>(result).StatusCode;
    }

    private static object? ValueOf(IActionResult? result)
    {
        return Assert.IsType<OkObjectResult>(result).Value;
    }
}
