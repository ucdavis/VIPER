using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Viper.Areas.Students.Constants;
using Viper.Areas.Students.Controllers;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.Students;

/// <summary>
/// Tests for CareerSelectionController: the authorization it applies itself (which students a
/// caller may see, and whose record they may open), and how service outcomes map to responses.
/// </summary>
public class CareerSelectionControllerTests
{
    private readonly ICareerSelectionService _service;
    private readonly ICareerSelectionOptionService _optionService;
    private readonly ICareerSelectionExportService _exportService;
    private readonly RAPSContext _rapsContext;
    private readonly IUserHelper _userHelper;
    private readonly CareerSelectionController _controller;

    public CareerSelectionControllerTests()
    {
        _service = Substitute.For<ICareerSelectionService>();
        _optionService = Substitute.For<ICareerSelectionOptionService>();
        _exportService = Substitute.For<ICareerSelectionExportService>();
        _rapsContext = Substitute.For<RAPSContext>();
        _userHelper = Substitute.For<IUserHelper>();
        _controller = new CareerSelectionController(
            _service, _optionService, _exportService, _rapsContext, _userHelper,
            Substitute.For<ILogger<CareerSelectionController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    #region Student list and report

    [Fact]
    public async Task GetStudentCareerList_AdminScope_RequestsEveryStudent()
    {
        var user = CreateUser(1, "admin", "ADMIN001");
        _userHelper.GetCurrentUser().Returns(user);
        _service.ResolveScope(user).Returns(CareerSelectionScope.All);
        _service.GetStudentCareerListAsync(StudentListAccess.AllStudents)
            .Returns([new StudentCareerListItemDto { PersonId = 5, FullName = "Student, Test" }]);

        var result = await _controller.GetStudentCareerList();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Single(Assert.IsType<List<StudentCareerListItemDto>>(okResult.Value));
    }

    [Fact]
    public async Task GetStudentCareerList_FacultyScope_NarrowsToTheirMentees()
    {
        var faculty = CreateUser(2, "faculty", "FAC001");
        _userHelper.GetCurrentUser().Returns(faculty);
        _service.ResolveScope(faculty).Returns(CareerSelectionScope.Mentored);
        _service.GetStudentCareerListAsync(StudentListAccess.MentoredBy("FAC001")).Returns([]);

        await _controller.GetStudentCareerList();

        await _service.Received(1).GetStudentCareerListAsync(StudentListAccess.MentoredBy("FAC001"));
        await _service.DidNotReceive().GetStudentCareerListAsync(StudentListAccess.AllStudents);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetStudentCareerList_FacultyWithNoMothraId_IsRefusedRatherThanShownEveryone(string? mothraId)
    {
        // A faculty member we cannot identify mentors nobody. The roster must fail closed, the
        // same way IsMentorOfAsync refuses a blank mentor on the single-record path.
        var faculty = CreateUser(2, "faculty", mothraId!);
        _userHelper.GetCurrentUser().Returns(faculty);
        _service.ResolveScope(faculty).Returns(CareerSelectionScope.Mentored);

        var result = await _controller.GetStudentCareerList();

        AssertForbidden(result.Result);
        await _service.DidNotReceive().GetStudentCareerListAsync(Arg.Any<StudentListAccess>());
    }

    [Fact]
    public async Task GetStudentCareerList_StudentScope_ReturnsForbid()
    {
        // A student has a record but no roster: their own page is reached directly.
        var student = CreateUser(3, "student", "STU001");
        _userHelper.GetCurrentUser().Returns(student);
        _service.ResolveScope(student).Returns(CareerSelectionScope.Own);

        var result = await _controller.GetStudentCareerList();

        AssertForbidden(result.Result);
        await _service.DidNotReceive().GetStudentCareerListAsync(Arg.Any<StudentListAccess>());
    }

    [Fact]
    public async Task GetStudentCareerList_NoUser_ReturnsForbid()
    {
        _userHelper.GetCurrentUser().ReturnsNull();

        var result = await _controller.GetStudentCareerList();

        AssertForbidden(result.Result);
    }

    [Fact]
    public async Task GetStudentCareerReport_FacultyScope_NarrowsToTheirMentees()
    {
        var faculty = CreateUser(2, "faculty", "FAC001");
        _userHelper.GetCurrentUser().Returns(faculty);
        _service.ResolveScope(faculty).Returns(CareerSelectionScope.Mentored);
        _service.GetStudentCareerReportAsync(StudentListAccess.MentoredBy("FAC001")).Returns([]);

        await _controller.GetStudentCareerReport();

        await _service.Received(1).GetStudentCareerReportAsync(StudentListAccess.MentoredBy("FAC001"));
    }

    [Fact]
    public async Task GetStudentCareerReport_NoScope_ReturnsForbid()
    {
        var user = CreateUser(4, "nobody", "NONE001");
        _userHelper.GetCurrentUser().Returns(user);
        _service.ResolveScope(user).Returns(CareerSelectionScope.None);

        var result = await _controller.GetStudentCareerReport();

        AssertForbidden(result.Result);
        await _service.DidNotReceive().GetStudentCareerReportAsync(Arg.Any<StudentListAccess>());
    }

    #endregion

    #region GetStudentCareerDetail

    [Fact]
    public async Task GetStudentCareerDetail_NoUser_ReturnsUnauthorized()
    {
        _userHelper.GetCurrentUser().ReturnsNull();

        var result = await _controller.GetStudentCareerDetail(5);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetStudentCareerDetail_NoCareerSelectionRole_ReturnsForbid()
    {
        var user = CreateUser(4, "nobody", "NONE001");
        _userHelper.GetCurrentUser().Returns(user);

        var result = await _controller.GetStudentCareerDetail(5);

        AssertForbidden(result.Result);
        await _service.DidNotReceive().GetStudentCareerDetailAsync(
            Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task GetStudentCareerDetail_AdminCanOpenAnyRecord()
    {
        var admin = GrantPermission(CreateUser(1, "admin", "ADMIN001"), CareerSelectionPermissions.Admin);
        _service.CanEdit(999, admin).Returns(true);
        _service.GetStudentCareerDetailAsync(999, true, true)
            .Returns(new StudentCareerDetailDto { PersonId = 999, FullName = "Student, Test" });

        var result = await _controller.GetStudentCareerDetail(999);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<StudentCareerDetailDto>(okResult.Value);
        Assert.Equal(999, dto.PersonId);
        // canEdit and canViewStudentList are the controller's decision, taken before the read.
        await _service.Received(1).GetStudentCareerDetailAsync(999, true, true);
    }

    [Fact]
    public async Task GetStudentCareerDetail_StudentOpeningOwnRecord_HasNoStudentList()
    {
        var student = GrantPermission(CreateUser(100, "student", "STU001"), CareerSelectionPermissions.Student);
        _service.CanEdit(100, student).Returns(true);
        _service.GetStudentCareerDetailAsync(100, true, false)
            .Returns(new StudentCareerDetailDto { PersonId = 100, FullName = "Self, Test" });

        var result = await _controller.GetStudentCareerDetail(100);

        Assert.IsType<OkObjectResult>(result.Result);
        // A student sees no roster, so the record is read without the student-list flag.
        await _service.Received(1).GetStudentCareerDetailAsync(100, true, false);
    }

    [Fact]
    public async Task GetStudentCareerDetail_StudentOpeningAnotherRecord_ReturnsForbid()
    {
        var student = GrantPermission(CreateUser(100, "student", "STU001"), CareerSelectionPermissions.Student);

        var result = await _controller.GetStudentCareerDetail(999);

        AssertForbidden(result.Result);
        await _service.DidNotReceive().GetStudentCareerDetailAsync(
            Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>());
        Assert.Equal(100, student.AaudUserId);
    }

    [Fact]
    public async Task GetStudentCareerDetail_FacultyOpeningTheirMentee_ReturnsOk()
    {
        var faculty = GrantPermission(CreateUser(2, "faculty", "FAC001"), CareerSelectionPermissions.Faculty);
        _service.IsMentorOfAsync("FAC001", 999).Returns(true);
        _service.CanEdit(999, faculty).Returns(false);
        _service.GetStudentCareerDetailAsync(999, false, true)
            .Returns(new StudentCareerDetailDto { PersonId = 999, FullName = "Mentee, Test" });

        var result = await _controller.GetStudentCareerDetail(999);

        Assert.IsType<OkObjectResult>(result.Result);
        // A mentor may open the record read-only, and does get the roster.
        await _service.Received(1).GetStudentCareerDetailAsync(999, false, true);
    }

    [Fact]
    public async Task GetStudentCareerDetail_FacultyOpeningSomeoneElsesMentee_ReturnsForbid()
    {
        GrantPermission(CreateUser(2, "faculty", "FAC001"), CareerSelectionPermissions.Faculty);
        _service.IsMentorOfAsync("FAC001", 999).Returns(false);

        var result = await _controller.GetStudentCareerDetail(999);

        AssertForbidden(result.Result);
        await _service.DidNotReceive().GetStudentCareerDetailAsync(
            Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task GetStudentCareerDetail_UnknownStudent_ReturnsNotFound()
    {
        var admin = GrantPermission(CreateUser(1, "admin", "ADMIN001"), CareerSelectionPermissions.Admin);
        _service.CanEdit(999, admin).Returns(true);
        _service.GetStudentCareerDetailAsync(999, true, true).ReturnsNull();

        var result = await _controller.GetStudentCareerDetail(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region UpdateStudentCareerSelection

    [Fact]
    public async Task UpdateStudentCareerSelection_NoUser_ReturnsUnauthorized()
    {
        _userHelper.GetCurrentUser().ReturnsNull();

        var result = await _controller.UpdateStudentCareerSelection(100, new StudentCareerInfoDto());

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStudentCareerSelection_WithoutEditRights_ReturnsForbid()
    {
        var student = CreateUser(100, "student", "STU001");
        _userHelper.GetCurrentUser().Returns(student);
        _service.CanEdit(100, student).Returns(false);

        var result = await _controller.UpdateStudentCareerSelection(100, new StudentCareerInfoDto());

        AssertForbidden(result.Result);
        await _service.DidNotReceive().UpdateStudentCareerSelectionAsync(
            Arg.Any<int>(), Arg.Any<StudentCareerInfoDto>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task UpdateStudentCareerSelection_ValidationErrors_ReturnsValidationProblem()
    {
        var admin = GrantPermission(CreateUser(1, "admin", "ADMIN001"), CareerSelectionPermissions.Admin);
        _service.CanEdit(100, admin).Returns(true);
        _service.UpdateStudentCareerSelectionAsync(100, Arg.Any<StudentCareerInfoDto>(), true)
            .Returns(["Select a career direction."]);

        var result = await _controller.UpdateStudentCareerSelection(100, new StudentCareerInfoDto());

        // ValidationProblem returns ObjectResult with a ValidationProblemDetails body; the status
        // code is filled in by the MVC pipeline, so assert the body shape rather than the code.
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.True(problem.Errors.TryGetValue("CareerSelection", out var errors));
        Assert.Contains("Select a career direction.", errors);
    }

    [Fact]
    public async Task UpdateStudentCareerSelection_StudentNoLongerCurrent_ReturnsNotFound()
    {
        var admin = GrantPermission(CreateUser(1, "admin", "ADMIN001"), CareerSelectionPermissions.Admin);
        _service.CanEdit(100, admin).Returns(true);
        _service.UpdateStudentCareerSelectionAsync(100, Arg.Any<StudentCareerInfoDto>(), true)
            .ThrowsAsync(new InvalidOperationException("PersonId 100 is not a current DVM student"));

        var result = await _controller.UpdateStudentCareerSelection(100, new StudentCareerInfoDto());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStudentCareerSelection_Saved_ReturnsRefreshedDetail()
    {
        var admin = GrantPermission(CreateUser(1, "admin", "ADMIN001"), CareerSelectionPermissions.Admin);
        _service.CanEdit(100, admin).Returns(true);
        _service.UpdateStudentCareerSelectionAsync(100, Arg.Any<StudentCareerInfoDto>(), true).Returns([]);
        _service.GetStudentCareerDetailAsync(100, true, true)
            .Returns(new StudentCareerDetailDto { PersonId = 100, FullName = "Student, Test" });

        var result = await _controller.UpdateStudentCareerSelection(100, new StudentCareerInfoDto());

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(100, Assert.IsType<StudentCareerDetailDto>(okResult.Value).PersonId);
    }

    #endregion

    #region Dropdown options

    [Fact]
    public async Task GetOptions_UnknownType_ReturnsNotFound()
    {
        _userHelper.GetCurrentUser().Returns(CreateUser(1, "admin", "ADMIN001"));

        var result = await _controller.GetOptions("colours", TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetOptions_AdminGetsUsageCounts()
    {
        GrantPermission(CreateUser(1, "admin", "ADMIN001"), CareerSelectionPermissions.Admin);
        _optionService.GetOptionsAsync(CareerOptionType.PostGrad, true, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _controller.GetOptions("post-grad", TestContext.Current.CancellationToken);

        Assert.IsType<OkObjectResult>(result.Result);
        await _optionService.Received(1)
            .GetOptionsAsync(CareerOptionType.PostGrad, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOptions_NonAdminGetsNoUsageCounts()
    {
        // Only admins manage the lists; nobody filling in the form needs the counts.
        _userHelper.GetCurrentUser().Returns(CreateUser(100, "student", "STU001"));
        _optionService.GetOptionsAsync(CareerOptionType.Career, false, Arg.Any<CancellationToken>()).Returns([]);

        await _controller.GetOptions("career", TestContext.Current.CancellationToken);

        await _optionService.Received(1)
            .GetOptionsAsync(CareerOptionType.Career, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOption_Success_ReturnsCreated()
    {
        _optionService.CreateOptionAsync(CareerOptionType.Career, "Mixed Animal", Arg.Any<CancellationToken>())
            .Returns(CareerSelectionOptionWriteResult.Success(
                new CareerSelectionOptionDto { Id = 7, Label = "Mixed Animal" }));

        var result = await _controller.CreateOption("career", new CareerSelectionOptionRequest { Label = "Mixed Animal" }, TestContext.Current.CancellationToken);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(7, Assert.IsType<CareerSelectionOptionDto>(created.Value).Id);
    }

    [Fact]
    public async Task CreateOption_DuplicateName_ReturnsConflictWithMessage()
    {
        _optionService.CreateOptionAsync(CareerOptionType.Career, "Equine", Arg.Any<CancellationToken>())
            .Returns(CareerSelectionOptionWriteResult.Conflict("That option already exists."));

        var result = await _controller.CreateOption("career", new CareerSelectionOptionRequest { Label = "Equine" }, TestContext.Current.CancellationToken);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("That option already exists.", conflict.Value);
    }

    [Fact]
    public async Task UpdateOption_CatchAll_ReturnsBadRequestWithMessage()
    {
        _optionService.UpdateOptionAsync(CareerOptionType.Species, 3, "Other things", Arg.Any<CancellationToken>())
            .Returns(CareerSelectionOptionWriteResult.Invalid("The Other option cannot be renamed."));

        var result = await _controller.UpdateOption("species", 3, new CareerSelectionOptionRequest { Label = "Other things" }, TestContext.Current.CancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("The Other option cannot be renamed.", badRequest.Value);
    }

    [Fact]
    public async Task DeleteOption_Success_ReturnsNoContent()
    {
        _optionService.DeleteOptionAsync(CareerOptionType.Species, 4, Arg.Any<CancellationToken>())
            .Returns(CareerSelectionOptionWriteResult.Success());

        var result = await _controller.DeleteOption("species", 4, TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteOption_AlreadyGone_ReturnsNotFound()
    {
        _optionService.DeleteOptionAsync(CareerOptionType.Species, 4, Arg.Any<CancellationToken>())
            .Returns(CareerSelectionOptionWriteResult.NotFound());

        var result = await _controller.DeleteOption("species", 4, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteOption_OptionInUse_ReturnsConflictWithMessage()
    {
        // A selection holds a foreign key to the option, so it cannot be deleted out from under it.
        _optionService.DeleteOptionAsync(CareerOptionType.Species, 4, Arg.Any<CancellationToken>())
            .Returns(CareerSelectionOptionWriteResult.Conflict("2 students have selected this option."));

        var result = await _controller.DeleteOption("species", 4, TestContext.Current.CancellationToken);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("2 students have selected this option.", conflict.Value);
    }

    [Fact]
    public async Task UpdateOption_Success_ReturnsTheRenamedOption()
    {
        _optionService.UpdateOptionAsync(CareerOptionType.Species, 3, "Equine and camelid", Arg.Any<CancellationToken>())
            .Returns(CareerSelectionOptionWriteResult.Success(
                new CareerSelectionOptionDto { Id = 3, Label = "Equine and camelid" }));

        var result = await _controller.UpdateOption("species", 3,
            new CareerSelectionOptionRequest { Label = "Equine and camelid" }, TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Equine and camelid", Assert.IsType<CareerSelectionOptionDto>(okResult.Value).Label);
    }

    [Theory]
    [InlineData("colours")]
    [InlineData("")]
    public async Task OptionWrites_UnknownType_ReturnNotFound(string type)
    {
        var request = new CareerSelectionOptionRequest { Label = "Exotics" };

        var created = await _controller.CreateOption(type, request, TestContext.Current.CancellationToken);
        var updated = await _controller.UpdateOption(type, 1, request, TestContext.Current.CancellationToken);
        var deleted = await _controller.DeleteOption(type, 1, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(created.Result);
        Assert.IsType<NotFoundResult>(updated.Result);
        Assert.IsType<NotFoundResult>(deleted);
        await _optionService.DidNotReceive().CreateOptionAsync(
            Arg.Any<CareerOptionType>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region App access

    [Fact]
    public async Task GetAccessStatus_ReturnsCurrentState()
    {
        _service.IsAppOpenAsync().Returns(true);

        var result = await _controller.GetAccessStatus();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True((bool)okResult.Value!);
    }

    [Fact]
    public async Task ToggleAppAccess_ReturnsNewState()
    {
        _service.ToggleAppAccessAsync().Returns(false);

        var result = await _controller.ToggleAppAccess();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.False((bool)okResult.Value!);
    }

    #endregion

    #region Exports

    [Fact]
    public async Task ExportOverviewExcel_WithoutListAccess_ReturnsForbid()
    {
        var student = CreateUser(100, "student", "STU001");
        _userHelper.GetCurrentUser().Returns(student);
        _service.ResolveScope(student).Returns(CareerSelectionScope.Own);

        var result = await _controller.ExportOverviewExcel();

        AssertForbidden(result);
        await _service.DidNotReceive().GetStudentCareerListAsync(Arg.Any<StudentListAccess>());
    }

    [Fact]
    public async Task ExportOverviewExcel_NoStudents_ReturnsNoContent()
    {
        var admin = CreateUser(1, "admin", "ADMIN001");
        _userHelper.GetCurrentUser().Returns(admin);
        _service.ResolveScope(admin).Returns(CareerSelectionScope.All);
        _service.GetStudentCareerListAsync(StudentListAccess.AllStudents).Returns([]);

        var result = await _controller.ExportOverviewExcel();

        Assert.IsType<NoContentResult>(result);
        _exportService.DidNotReceive().GenerateOverviewExcel(Arg.Any<List<StudentCareerListItemDto>>());
    }

    [Fact]
    public async Task ExportOverviewExcel_WithStudents_ReturnsWorkbook()
    {
        var admin = CreateUser(1, "admin", "ADMIN001");
        _userHelper.GetCurrentUser().Returns(admin);
        _service.ResolveScope(admin).Returns(CareerSelectionScope.All);
        _service.GetStudentCareerListAsync(StudentListAccess.AllStudents)
            .Returns([new StudentCareerListItemDto { PersonId = 5, FullName = "Student, Test" }]);
        using var stream = new MemoryStream([1, 2, 3]);
        _exportService.GenerateOverviewExcel(Arg.Any<List<StudentCareerListItemDto>>())
            .Returns(stream);

        var result = await _controller.ExportOverviewExcel();

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Contains("CareerSelectionOverview", file.FileDownloadName);
    }

    [Fact]
    public async Task ExportExcel_WithStudents_ReturnsTheReportWorkbook()
    {
        var admin = CreateUser(1, "admin", "ADMIN001");
        _userHelper.GetCurrentUser().Returns(admin);
        _service.ResolveScope(admin).Returns(CareerSelectionScope.All);
        _service.GetStudentCareerReportAsync(StudentListAccess.AllStudents)
            .Returns([new StudentCareerReportDto { PersonId = 5, FullName = "Student, Test" }]);
        using var stream = new MemoryStream([1, 2, 3]);
        _exportService.GenerateExcel(Arg.Any<List<StudentCareerReportDto>>()).Returns(stream);

        var result = await _controller.ExportExcel();

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Contains("CareerSelection", file.FileDownloadName);
    }

    [Fact]
    public async Task ExportOverviewPdf_WithStudents_ReturnsAnInlinePdf()
    {
        var admin = CreateUser(1, "admin", "ADMIN001");
        _userHelper.GetCurrentUser().Returns(admin);
        _service.ResolveScope(admin).Returns(CareerSelectionScope.All);
        _service.GetStudentCareerListAsync(StudentListAccess.AllStudents)
            .Returns([new StudentCareerListItemDto { PersonId = 5, FullName = "Student, Test" }]);
        _exportService.GenerateOverviewPdf(Arg.Any<List<StudentCareerListItemDto>>()).Returns([1, 2, 3]);

        var result = await _controller.ExportOverviewPdf();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        // Inline exports carry student data, so they must not be cached by browsers or proxies.
        Assert.Equal("private, no-store, max-age=0", _controller.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task ExportOverviewPdf_NoStudents_ReturnsNoContent()
    {
        var admin = CreateUser(1, "admin", "ADMIN001");
        _userHelper.GetCurrentUser().Returns(admin);
        _service.ResolveScope(admin).Returns(CareerSelectionScope.All);
        _service.GetStudentCareerListAsync(StudentListAccess.AllStudents).Returns([]);

        var result = await _controller.ExportOverviewPdf();

        Assert.IsType<NoContentResult>(result);
        _exportService.DidNotReceive().GenerateOverviewPdf(Arg.Any<List<StudentCareerListItemDto>>());
    }

    [Fact]
    public async Task ExportExcel_WithoutListAccess_ReturnsForbid()
    {
        var student = CreateUser(100, "student", "STU001");
        _userHelper.GetCurrentUser().Returns(student);
        _service.ResolveScope(student).Returns(CareerSelectionScope.Own);

        var result = await _controller.ExportExcel();

        AssertForbidden(result);
        await _service.DidNotReceive().GetStudentCareerReportAsync(Arg.Any<StudentListAccess>());
    }

    [Fact]
    public async Task ExportPdf_FacultyScope_ExportsOnlyTheirMentees()
    {
        var faculty = CreateUser(2, "faculty", "FAC001");
        _userHelper.GetCurrentUser().Returns(faculty);
        _service.ResolveScope(faculty).Returns(CareerSelectionScope.Mentored);
        _service.GetStudentCareerReportAsync(StudentListAccess.MentoredBy("FAC001"))
            .Returns([new StudentCareerReportDto { PersonId = 5, FullName = "Mentee, Test" }]);
        _exportService.GeneratePdf(Arg.Any<List<StudentCareerReportDto>>()).Returns([1, 2, 3]);

        var result = await _controller.ExportPdf();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        await _service.Received(1).GetStudentCareerReportAsync(StudentListAccess.MentoredBy("FAC001"));
    }

    [Fact]
    public async Task ExportOverviewCsv_WithStudents_ReturnsACsvDownload()
    {
        var admin = CreateUser(1, "admin", "ADMIN001");
        _userHelper.GetCurrentUser().Returns(admin);
        _service.ResolveScope(admin).Returns(CareerSelectionScope.All);
        _service.GetStudentCareerListAsync(StudentListAccess.AllStudents)
            .Returns([new StudentCareerListItemDto { PersonId = 5, FullName = "Student, Test" }]);
        _exportService.GenerateOverviewCsv(Arg.Any<List<StudentCareerListItemDto>>()).Returns([1, 2, 3]);

        var result = await _controller.ExportOverviewCsv();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        Assert.Equal("CareerSelectionOverview.csv", file.FileDownloadName);
    }

    [Fact]
    public async Task ExportCsv_WithStudents_ReturnsTheReportCsv()
    {
        var admin = CreateUser(1, "admin", "ADMIN001");
        _userHelper.GetCurrentUser().Returns(admin);
        _service.ResolveScope(admin).Returns(CareerSelectionScope.All);
        _service.GetStudentCareerReportAsync(StudentListAccess.AllStudents)
            .Returns([new StudentCareerReportDto { PersonId = 5, FullName = "Student, Test" }]);
        _exportService.GenerateCsv(Arg.Any<List<StudentCareerReportDto>>()).Returns([1, 2, 3]);

        var result = await _controller.ExportCsv();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("CareerSelection.csv", file.FileDownloadName);
    }

    [Fact]
    public async Task ExportCsv_WithoutListAccess_ReturnsForbid()
    {
        var student = CreateUser(100, "student", "STU001");
        _userHelper.GetCurrentUser().Returns(student);
        _service.ResolveScope(student).Returns(CareerSelectionScope.Own);

        var result = await _controller.ExportCsv();

        AssertForbidden(result);
        await _service.DidNotReceive().GetStudentCareerReportAsync(Arg.Any<StudentListAccess>());
    }

    [Fact]
    public async Task ExportOverviewCsv_NoStudents_ReturnsNoContent()
    {
        var admin = CreateUser(1, "admin", "ADMIN001");
        _userHelper.GetCurrentUser().Returns(admin);
        _service.ResolveScope(admin).Returns(CareerSelectionScope.All);
        _service.GetStudentCareerListAsync(StudentListAccess.AllStudents).Returns([]);

        var result = await _controller.ExportOverviewCsv();

        Assert.IsType<NoContentResult>(result);
        _exportService.DidNotReceive().GenerateOverviewCsv(Arg.Any<List<StudentCareerListItemDto>>());
    }

    #endregion

    #region Mentor search

    [Fact]
    public async Task SearchMentors_ReturnsMatches()
    {
        _service.SearchMentorsAsync("smi", Arg.Any<CancellationToken>())
            .Returns([new MentorOptionDto { PersonId = 8, FullName = "Smith, Ann", IamId = "IAM8" }]);

        var result = await _controller.SearchMentors("smi", TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Single(Assert.IsType<List<MentorOptionDto>>(okResult.Value));
    }

    #endregion

    #region Helpers

    private static void AssertForbidden(ActionResult? result)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    /// <summary>Signs the user in and grants them one career selection permission.</summary>
    private AaudUser GrantPermission(AaudUser user, string permission)
    {
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, permission).Returns(true);
        return user;
    }

    private static AaudUser CreateUser(int personId, string loginId, string mothraId)
    {
        return new AaudUser
        {
            AaudUserId = personId,
            ClientId = "UCD",
            MothraId = mothraId,
            LoginId = loginId,
            DisplayFullName = $"Test {loginId}",
            DisplayFirstName = "Test",
            DisplayLastName = loginId,
            LastName = loginId,
            FirstName = "Test"
        };
    }

    #endregion
}
