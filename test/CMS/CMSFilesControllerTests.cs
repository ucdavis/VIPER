using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NSubstitute.ExceptionExtensions;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Controllers;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Areas.CMS.Services;
using Viper.Classes.SQLContext;
using Viper.Models;
using Viper.Models.AAUD;

namespace Viper.test.CMS;

/// <summary>
/// Controller wiring tests for CMSFilesController: list/paging passthrough, get/upload/update
/// delete/restore status mapping (404 on null, 400 on bad input/conflict), import + preview +
/// bulk-encrypt empty-input guards, and admin-only permanent delete. The service behavior is
/// covered separately in CmsFileServiceTests / CmsFileImportServiceTests.
/// </summary>
public sealed class CMSFilesControllerTests : IDisposable
{
    private readonly ICmsFileService _fileService;
    private readonly ICmsFileStorageService _storage;
    private readonly ICmsFileAuditService _auditService;
    private readonly ICmsFileImportService _importService;
    private readonly RAPSContext _rapsContext;
    private readonly IUserHelper _userHelper;
    private readonly CMSFilesController _controller;

    public CMSFilesControllerTests()
    {
        _fileService = Substitute.For<ICmsFileService>();
        _storage = Substitute.For<ICmsFileStorageService>();
        _auditService = Substitute.For<ICmsFileAuditService>();
        _importService = Substitute.For<ICmsFileImportService>();
        _rapsContext = new RAPSContext(new DbContextOptionsBuilder<RAPSContext>()
            .UseInMemoryDatabase("RAPS_" + Guid.NewGuid()).Options);
        _userHelper = Substitute.For<IUserHelper>();

        _controller = new CMSFilesController(_fileService, _storage, _auditService, _importService, _rapsContext,
            Substitute.For<ILogger<CMSFilesController>>(), _userHelper);
        SetupControllerContext();
    }

    public void Dispose() => _rapsContext.Dispose();

    private void SetupControllerContext()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
        };
    }

    private static CmsFileDto File(Guid? guid = null) => new()
    {
        FileGuid = guid ?? Guid.NewGuid(),
        FileName = "report.pdf",
        FriendlyName = "cats-report.pdf",
        Folder = "cats"
    };

    private static IFormFile MakeFormFile(string fileName = "report.pdf", long length = 3)
    {
        var stream = new MemoryStream(new byte[length]);
        return new FormFile(stream, 0, length, "file", fileName);
    }

    #region List

    [Fact]
    public async Task GetFiles_PassesFiltersAndPaginationThrough_AndSetsTotal()
    {
        var files = new List<CmsFileDto> { File() };
        _fileService.GetFilesAsync("cats", "deleted", "budget", true, false, 2, 25, "modifiedOn", true,
            Arg.Any<CancellationToken>()).Returns((files, 42));
        var pagination = new ApiPagination { Page = 2, PerPage = 25 };

        var result = await _controller.GetFiles("cats", "budget", true, false, pagination, "deleted", "modifiedOn", true, TestContext.Current.CancellationToken);

        Assert.Same(files, result.Value);
        Assert.Equal(42, pagination.TotalRecords);
        await _fileService.Received(1).GetFilesAsync("cats", "deleted", "budget", true, false, 2, 25, "modifiedOn", true,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFiles_DefaultsPageAndPerPage_WhenNoPagination()
    {
        _fileService.GetFilesAsync(null, "active", null, null, null, 1, 50, "friendlyName", false,
            Arg.Any<CancellationToken>()).Returns((new List<CmsFileDto>(), 0));

        await _controller.GetFiles(null, null, null, null, null, ct: TestContext.Current.CancellationToken);

        await _fileService.Received(1).GetFilesAsync(null, "active", null, null, null, 1, 50, "friendlyName", false,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFolders_DiskListByDefault_FilterListWhenIncludeData()
    {
        _storage.GetTopLevelFolders().Returns(new List<string> { "cats" });
        _storage.GetFilterFoldersAsync(Arg.Any<CancellationToken>()).Returns(new List<string> { "cats", "legacy" });

        var disk = await _controller.GetFolders(includeData: false, TestContext.Current.CancellationToken);
        var filter = await _controller.GetFolders(includeData: true, TestContext.Current.CancellationToken);

        Assert.Equal(new List<string> { "cats" }, disk.Value);
        Assert.Equal(new List<string> { "cats", "legacy" }, filter.Value);
    }

    [Fact]
    public async Task GetAudit_PassesFilterAndPaginationThrough_AndSetsTotal()
    {
        _auditService.GetAuditEntriesAsync(Arg.Any<CmsFileAuditFilter>(), 3, 10, Arg.Any<CancellationToken>())
            .Returns(new List<Viper.Models.VIPER.FileAudit>());
        _auditService.GetAuditEntryCountAsync(Arg.Any<CmsFileAuditFilter>(), Arg.Any<CancellationToken>()).Returns(5);
        var fileGuid = Guid.NewGuid();
        var pagination = new ApiPagination { Page = 3, PerPage = 10 };

        await _controller.GetAudit(fileGuid, "AccessFile", "loginX", null, null, "needle", pagination, TestContext.Current.CancellationToken);

        Assert.Equal(5, pagination.TotalRecords);
        await _auditService.Received(1).GetAuditEntriesAsync(
            Arg.Is<CmsFileAuditFilter>(f =>
                f.FileGuid == fileGuid && f.Action == "AccessFile" && f.LoginId == "loginX" && f.Search == "needle"),
            3, 10, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Get / CheckName

    [Fact]
    public async Task GetFile_ReturnsFile_WhenFound()
    {
        var guid = Guid.NewGuid();
        _fileService.GetFileAsync(guid, Arg.Any<CancellationToken>()).Returns(File(guid));

        var result = await _controller.GetFile(guid, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Value);
        Assert.Equal(guid, result.Value!.FileGuid);
    }

    [Fact]
    public async Task GetFile_ReturnsNotFound_WhenMissing()
    {
        _fileService.GetFileAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _controller.GetFile(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CheckName_ReturnsBadRequest_OnArgumentException()
    {
        _fileService.CheckNameAsync("nope", "report.pdf", Arg.Any<CancellationToken>())
            .Throws(new ArgumentException("Invalid folder."));

        var result = await _controller.CheckName("nope", "report.pdf", TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region Upload / Update

    [Fact]
    public async Task UploadFile_ReturnsBadRequest_WhenNoFile()
    {
        var result = await _controller.UploadFile(new CmsFileCreateRequest { Folder = "cats" }, null, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await _fileService.DidNotReceive().CreateFileAsync(Arg.Any<CmsFileCreateRequest>(), Arg.Any<IFormFile>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadFile_ReturnsFile_OnSuccess()
    {
        var request = new CmsFileCreateRequest { Folder = "cats" };
        var dto = File();
        _fileService.CreateFileAsync(request, Arg.Any<IFormFile>(), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _controller.UploadFile(request, MakeFormFile(), TestContext.Current.CancellationToken);

        Assert.Same(dto, result.Value);
    }

    [Fact]
    public async Task UploadFile_ReturnsBadRequest_OnConflict()
    {
        var request = new CmsFileCreateRequest { Folder = "cats" };
        _fileService.CreateFileAsync(request, Arg.Any<IFormFile>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("A file with that name already exists."));

        var result = await _controller.UploadFile(request, MakeFormFile(), TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateFile_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var guid = Guid.NewGuid();
        var request = new CmsFileUpdateRequest { Description = "x" };
        _fileService.UpdateFileAsync(guid, request, Arg.Any<IFormFile?>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _controller.UpdateFile(guid, request, null, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateFile_ReturnsBadRequest_OnArgumentException()
    {
        var guid = Guid.NewGuid();
        var request = new CmsFileUpdateRequest { Description = "x" };
        _fileService.UpdateFileAsync(guid, request, Arg.Any<IFormFile?>(), Arg.Any<CancellationToken>())
            .Throws(new ArgumentException("Replacement must keep the same extension."));

        var result = await _controller.UpdateFile(guid, request, MakeFormFile("x.png"), TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region Delete / Restore

    [Fact]
    public async Task DeleteFile_SoftDelete_ReturnsNoContent()
    {
        var guid = Guid.NewGuid();
        _fileService.SoftDeleteFileAsync(guid, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _controller.DeleteFile(guid, permanent: false, TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteFile_SoftDelete_ReturnsNotFound_WhenMissing()
    {
        _fileService.SoftDeleteFileAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _controller.DeleteFile(Guid.NewGuid(), permanent: false, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteFile_PermanentWithoutAdmin_ReturnsForbidden()
    {
        var user = new AaudUser { AaudUserId = 1, LoginId = "user" };
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.Admin).Returns(false);

        var result = await _controller.DeleteFile(Guid.NewGuid(), permanent: true, TestContext.Current.CancellationToken);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        await _fileService.DidNotReceive().PermanentlyDeleteFileAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteFile_PermanentAsAdmin_ReturnsNoContent()
    {
        var guid = Guid.NewGuid();
        var user = new AaudUser { AaudUserId = 1, LoginId = "admin" };
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.Admin).Returns(true);
        _fileService.PermanentlyDeleteFileAsync(guid, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _controller.DeleteFile(guid, permanent: true, TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
        await _fileService.Received(1).PermanentlyDeleteFileAsync(guid, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreFile_ReturnsNoContent_WhenRestored()
    {
        var guid = Guid.NewGuid();
        _fileService.RestoreFileAsync(guid, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _controller.RestoreFile(guid, TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region Import / Preview / Bulk-encrypt

    [Fact]
    public async Task ImportFiles_ReturnsBadRequest_WhenNoPaths()
    {
        var result = await _controller.ImportFiles(new CmsFileImportRequest { Folder = "cats" }, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await _importService.DidNotReceive().ImportFilesAsync(Arg.Any<CmsFileImportRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportFiles_PassesRequestThrough_OnSuccess()
    {
        var request = new CmsFileImportRequest { Folder = "cats", FilePaths = new List<string> { "legacy/a.pdf" } };
        var results = new List<CmsFileImportResult> { new() { FilePath = "legacy/a.pdf", Success = true } };
        _importService.ImportFilesAsync(request, Arg.Any<CancellationToken>()).Returns(results);

        var result = await _controller.ImportFiles(request, TestContext.Current.CancellationToken);

        Assert.Same(results, result.Value);
    }

    [Fact]
    public async Task ImportFiles_ReturnsBadRequest_OnInvalidOperation()
    {
        var request = new CmsFileImportRequest { Folder = "cats", FilePaths = new List<string> { "legacy/a.pdf" } };
        _importService.ImportFilesAsync(request, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Legacy webroot not configured."));

        var result = await _controller.ImportFiles(request, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task PreviewImport_ReturnsBadRequest_WhenNoPaths()
    {
        var result = await _controller.PreviewImport(new CmsFileImportRequest { Folder = "cats" }, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task PreviewImport_PassesRequestThrough()
    {
        var request = new CmsFileImportRequest { Folder = "cats", FilePaths = new List<string> { "legacy/a.pdf" } };
        var preview = new List<CmsFileImportPreviewResult> { new() { FilePath = "legacy/a.pdf", CanImport = true } };
        _importService.PreviewImportAsync(request, Arg.Any<CancellationToken>()).Returns(preview);

        var result = await _controller.PreviewImport(request, TestContext.Current.CancellationToken);

        Assert.Same(preview, result.Value);
    }

    [Fact]
    public async Task BulkEncrypt_ReturnsBadRequest_WhenNoGuids()
    {
        var result = await _controller.BulkEncrypt(new List<Guid>(), TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await _importService.DidNotReceive().BulkEncryptAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkEncrypt_PassesGuidsThrough()
    {
        var guids = new List<Guid> { Guid.NewGuid() };
        var results = new List<CmsBulkEncryptResult> { new() { FileGuid = guids[0], Success = true } };
        _importService.BulkEncryptAsync(guids, Arg.Any<CancellationToken>()).Returns(results);

        var result = await _controller.BulkEncrypt(guids, TestContext.Current.CancellationToken);

        Assert.Same(results, result.Value);
    }

    #endregion
}
