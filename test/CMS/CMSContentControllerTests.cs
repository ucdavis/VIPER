using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NSubstitute.ExceptionExtensions;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Controllers;
using Viper.Areas.CMS.Models;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Areas.CMS.Services;
using Viper.Classes.SQLContext;
using Viper.Models;
using Viper.Models.AAUD;
using Viper.Services;

namespace Viper.test.CMS;

/// <summary>
/// Controller wiring tests for CMSContentController: the action layer delegates to
/// ICmsContentBlockService and maps results to status codes (404 on null, 409 on
/// concurrency, 400 on validation). The new history-list and version-diff endpoints
/// are covered for passthrough and 404-on-null only; the query/diff logic itself lives
/// in CmsContentBlockServiceTests. Permanent delete is admin-gated.
/// </summary>
public sealed class CMSContentControllerTests : IDisposable
{
    private readonly ICmsContentBlockService _blockService;
    private readonly ICmsFileService _fileService;
    private readonly ICmsFileStorageService _storage;
    private readonly IUserHelper _userHelper;
    private readonly VIPERContext _context;
    private readonly RAPSContext _rapsContext;
    private readonly CMSContentController _controller;

    public CMSContentControllerTests()
    {
        _blockService = Substitute.For<ICmsContentBlockService>();
        _fileService = Substitute.For<ICmsFileService>();
        _storage = Substitute.For<ICmsFileStorageService>();
        _userHelper = Substitute.For<IUserHelper>();
        var sanitizer = Substitute.For<IHtmlSanitizerService>();
        var logger = Substitute.For<ILogger<CMSContentController>>();
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _rapsContext = new RAPSContext(new DbContextOptionsBuilder<RAPSContext>()
            .UseInMemoryDatabase("RAPS_" + Guid.NewGuid()).Options);

        // The content/history/PATCH endpoints now gate on CanEditAsync; default it open so the
        // existing passthrough tests exercise the mapping. Tests that assert the 403 path override
        // it for their block id.
        _blockService.CanEditAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        _controller = new CMSContentController(_context, _rapsContext, sanitizer, _blockService, _fileService,
            _storage, _userHelper, logger);
        SetupControllerContext();
    }

    public void Dispose()
    {
        _context.Dispose();
        _rapsContext.Dispose();
    }

    private void SetupControllerContext()
    {
        // ValidationProblem(string) resolves ProblemDetailsFactory from RequestServices, so a
        // stub is registered; otherwise the call throws before producing the ObjectResult.
        var services = new ServiceCollection();
        services.AddSingleton<ProblemDetailsFactory, StubProblemDetailsFactory>();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() }
        };
    }

    private sealed class StubProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null,
            string? title = null, string? type = null, string? detail = null, string? instance = null)
            => new() { Status = statusCode, Title = title, Detail = detail };

        public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext,
            ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null,
            string? type = null, string? detail = null, string? instance = null)
            => new(modelStateDictionary) { Status = statusCode, Title = title, Detail = detail };
    }

    private static ContentBlockDto Block(int id = 1) => new()
    {
        ContentBlockId = id,
        Title = "Block",
        System = "Viper",
        Content = "<p>x</p>"
    };

    private static CMSBlockAddEdit Request(int id = 1) => new()
    {
        ContentBlockId = id,
        Title = "Block",
        System = "Viper",
        Content = "<p>x</p>",
        AllowPublicAccess = false,
        LastModifiedOn = DateTime.Now
    };

    #region List / Get

    [Fact]
    public async Task GetContentBlocks_PassesFiltersThrough()
    {
        var blocks = new List<ContentBlockDto> { Block() };
        _blockService.GetContentBlocksAsync("deleted", "Viper", "cats", "search", true, 1, 50, "title", false,
            Arg.Any<CancellationToken>()).Returns((blocks, 1));

        var result = await _controller.GetContentBlocks(null, "deleted", "Viper", "cats", "search", true,
            ct: TestContext.Current.CancellationToken);

        Assert.Same(blocks, result.Value);
        await _blockService.Received(1).GetContentBlocksAsync("deleted", "Viper", "cats", "search", true, 1, 50,
            "title", false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetFolders_ReturnsTopLevelUploadFolders()
    {
        var folders = new List<string> { "accreditation", "students" };
        _storage.GetTopLevelFolders().Returns(folders);

        var result = _controller.GetFolders();

        Assert.Same(folders, result.Value);
        _storage.Received(1).GetTopLevelFolders();
    }

    [Fact]
    public async Task GetContentBlock_ReturnsBlock_WhenFound()
    {
        _blockService.GetContentBlockAsync(5, Arg.Any<CancellationToken>()).Returns(Block(5));

        var result = await _controller.GetContentBlock(5, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Value);
        Assert.Equal(5, result.Value!.ContentBlockId);
    }

    [Fact]
    public async Task GetContentBlock_ReturnsNotFound_WhenBlockDoesNotExist()
    {
        // A genuinely missing block: not editable AND no section-path row, so 404 (not 403), and the
        // full block is never loaded on this path.
        _blockService.CanEditAsync(999, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(999, Arg.Any<CancellationToken>()).Returns((false, (string?)null));

        var result = await _controller.GetContentBlock(999, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
        await _blockService.DidNotReceive().GetContentBlockAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetContentBlock_ReturnsNotFound_WhenBlockNullAfterAuthPasses()
    {
        // Defensive race guard: CanEditAsync passed but the block is gone by the time we load it,
        // so 404 rather than a null 200.
        _blockService.GetContentBlockAsync(5, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _controller.GetContentBlock(5, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void GetContentBlockByFn_ReturnsNotFound_WhenMissing()
    {
        var result = _controller.GetContentBlockByFn("does-not-exist");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetContentBlockByFn_ProjectsToDto_AndDoesNotLeakEntityInternals()
    {
        // Regression: this endpoint is anonymous. It must project to a DTO and never serialize the
        // raw ContentBlock graph, which would leak each attached file's AES Key and server FilePath,
        // the full content history, and the block's permission rows.
        var file = new Models.VIPER.File
        {
            FileGuid = Guid.NewGuid(),
            FriendlyName = "attached-doc",
            FilePath = @"S:\Files\secret\attached-doc.pdf",
            Key = "SUPER_SECRET_AES_KEY",
            Encrypted = true,
            AllowPublicAccess = true,
            Description = "internal notes",
            ModifiedBy = "author"
        };
        var block = new Models.VIPER.ContentBlock
        {
            Content = "<p>body</p>",
            Title = "Public Block",
            System = "Viper",
            FriendlyName = "public-fn",
            AllowPublicAccess = true,
            ModifiedOn = DateTime.Now,
            ModifiedBy = "author",
            ContentBlockToFiles = { new Models.VIPER.ContentBlockToFile { FileGuid = file.FileGuid, File = file } },
            ContentHistories = { new Models.VIPER.ContentHistory { ContentBlockContent = "<p>OLD SECRET VERSION</p>", ModifiedOn = DateTime.Now.AddDays(-1), ModifiedBy = "author" } }
        };
        _context.Files.Add(file);
        _context.ContentBlocks.Add(block);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = _controller.GetContentBlockByFn("public-fn");

        Assert.NotNull(result.Value);
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value,
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

        // Consumer contract (VueApp ContentBlock type) preserved.
        Assert.Contains("\"contentBlockId\"", json);
        Assert.Contains("\"content\"", json);
        Assert.Contains("\"title\"", json);
        // No entity internals or secrets.
        Assert.DoesNotContain("\"key\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("filePath", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contentHistories", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SUPER_SECRET_AES_KEY", json);
        Assert.DoesNotContain(@"S:\Files", json);
        // No management metadata: the anonymous endpoint must not disclose editor login ids,
        // permission names, or placement fields (PublicContentBlockDto, not ContentBlockDto).
        Assert.DoesNotContain("modifiedBy", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("permissions", json, StringComparison.OrdinalIgnoreCase);
        // "permissions" DoesNotContain already covers "editPermissions", but assert it explicitly:
        // the public DTO must never disclose a block's delegated-editor permission list.
        Assert.DoesNotContain("editPermissions", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"system\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("viperSectionPath", json, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Create

    [Fact]
    public async Task CreateContentBlock_ReturnsBadRequest_WhenTitleAndSystemMissing()
    {
        var request = new CMSBlockAddEdit { ContentBlockId = 0, AllowPublicAccess = false, Content = "x", Title = "", System = "" };

        var result = await _controller.CreateContentBlock(request, TestContext.Current.CancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Title is required", badRequest.Value?.ToString());
        Assert.Contains("System is required", badRequest.Value?.ToString());
        await _blockService.DidNotReceive().CreateContentBlockAsync(Arg.Any<CMSBlockAddEdit>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateContentBlock_ReturnsBlock_OnSuccess()
    {
        var request = Request(0);
        _blockService.CreateContentBlockAsync(request, Arg.Any<CancellationToken>()).Returns(Block(7));

        var result = await _controller.CreateContentBlock(request, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Value);
        Assert.Equal(7, result.Value!.ContentBlockId);
    }

    [Fact]
    public async Task CreateContentBlock_ReturnsValidationProblem_OnArgumentException()
    {
        var request = Request(0);
        _blockService.CreateContentBlockAsync(request, Arg.Any<CancellationToken>())
            .Throws(new ArgumentException("Friendly name already in use"));

        var result = await _controller.CreateContentBlock(request, TestContext.Current.CancellationToken);

        Assert.IsType<ObjectResult>(result.Result);
        var problem = (ObjectResult)result.Result!;
        Assert.IsType<ValidationProblemDetails>(problem.Value);
    }

    #endregion

    #region Update

    [Fact]
    public async Task UpdateContentBlock_ReturnsBadRequest_WhenRouteIdMismatch()
    {
        var request = Request(2);

        var result = await _controller.UpdateContentBlock(1, request, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task UpdateContentBlock_ReturnsBlock_OnSuccess()
    {
        var request = Request(3);
        _blockService.UpdateContentBlockAsync(3, request, Arg.Any<CancellationToken>()).Returns(Block(3));

        var result = await _controller.UpdateContentBlock(3, request, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value!.ContentBlockId);
    }

    [Fact]
    public async Task UpdateContentBlock_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var request = Request(3);
        _blockService.UpdateContentBlockAsync(3, request, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _controller.UpdateContentBlock(3, request, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateContentBlock_ReturnsConflict_OnConcurrencyException()
    {
        var request = Request(3);
        _blockService.UpdateContentBlockAsync(3, request, Arg.Any<CancellationToken>())
            .Throws(new CmsConcurrencyException("stale"));

        var result = await _controller.UpdateContentBlock(3, request, TestContext.Current.CancellationToken);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateContentOnly_ReturnsBlock_OnSuccess()
    {
        var fileGuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var update = new ContentOnlyUpdate { Content = "<p>quick</p>", LastModifiedOn = DateTime.Now, FileGuids = fileGuids };
        // Match the exact GUID collection so the test fails if the controller drops or replaces it.
        _blockService.UpdateContentOnlyAsync(4, update.Content, update.LastModifiedOn,
            Arg.Is<List<Guid>?>(g => g != null && g.SequenceEqual(fileGuids)), Arg.Any<CancellationToken>()).Returns(Block(4));

        var result = await _controller.UpdateContentOnly(4, update, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Value);
        await _blockService.Received(1).UpdateContentOnlyAsync(4, update.Content, update.LastModifiedOn,
            Arg.Is<List<Guid>?>(g => g != null && g.SequenceEqual(fileGuids)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateContentOnly_ReturnsConflict_OnConcurrencyException()
    {
        var update = new ContentOnlyUpdate { Content = "<p>quick</p>", LastModifiedOn = DateTime.Now };
        _blockService.UpdateContentOnlyAsync(4, update.Content, update.LastModifiedOn, Arg.Any<List<Guid>?>(),
            Arg.Any<CancellationToken>()).Throws(new CmsConcurrencyException("stale"));

        var result = await _controller.UpdateContentOnly(4, update, TestContext.Current.CancellationToken);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    #endregion

    #region History list + version + diff

    [Fact]
    public async Task GetHistory_PassesBlockIdThrough()
    {
        var history = new List<ContentHistoryListItemDto> { new() { ContentHistoryId = 1 } };
        _blockService.GetHistoryAsync(5, Arg.Any<CancellationToken>()).Returns(history);

        var result = await _controller.GetHistory(5, TestContext.Current.CancellationToken);

        Assert.Same(history, result.Value);
    }

    [Fact]
    public async Task GetHistoryVersion_ReturnsNotFound_WhenMissing()
    {
        _blockService.GetHistoryVersionAsync(5, 12, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _controller.GetHistoryVersion(5, 12, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetHistoryEntries_PassesFilterAndPaginationThrough_AndSetsTotal()
    {
        var entries = new List<ContentHistoryAuditDto> { new() { ContentHistoryId = 1 } };
        _blockService.GetHistoryEntriesAsync(Arg.Any<CmsContentHistoryFilter>(), 2, 25, Arg.Any<CancellationToken>())
            .Returns(entries);
        _blockService.GetHistoryEntryCountAsync(Arg.Any<CmsContentHistoryFilter>(), Arg.Any<CancellationToken>())
            .Returns(99);
        var pagination = new ApiPagination { Page = 2, PerPage = 25 };
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var to = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Local);

        var result = await _controller.GetHistoryEntries(7, "editorX", from, to, "needle", pagination, TestContext.Current.CancellationToken);

        Assert.Same(entries, result.Value);
        Assert.Equal(99, pagination.TotalRecords);
        await _blockService.Received(1).GetHistoryEntriesAsync(
            Arg.Is<CmsContentHistoryFilter>(f =>
                f.ContentBlockId == 7 && f.ModifiedBy == "editorX" && f.From == from && f.To == to && f.Search == "needle"),
            2, 25, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistoryEntries_DefaultsPageAndPerPage_AndSkipsCount_WhenNoPagination()
    {
        _blockService.GetHistoryEntriesAsync(Arg.Any<CmsContentHistoryFilter>(), 1, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ContentHistoryAuditDto>());

        await _controller.GetHistoryEntries(null, null, null, null, null, null, TestContext.Current.CancellationToken);

        await _blockService.Received(1).GetHistoryEntriesAsync(Arg.Any<CmsContentHistoryFilter>(), 1, 50,
            Arg.Any<CancellationToken>());
        await _blockService.DidNotReceive().GetHistoryEntryCountAsync(Arg.Any<CmsContentHistoryFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistoryVersionDiff_PassesThrough_AndReturnsDto()
    {
        var diff = new ContentHistoryDiffDto { HasComparison = true, Content = "<ins>x</ins>" };
        _blockService.GetHistoryVersionDiffAsync(5, 12, Arg.Any<CancellationToken>()).Returns(diff);

        var result = await _controller.GetHistoryVersionDiff(5, 12, TestContext.Current.CancellationToken);

        Assert.Same(diff, result.Value);
        await _blockService.Received(1).GetHistoryVersionDiffAsync(5, 12, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistoryVersionDiff_ReturnsNotFound_WhenNull()
    {
        _blockService.GetHistoryVersionDiffAsync(5, 12, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _controller.GetHistoryVersionDiff(5, 12, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DiffAgainstHistoryVersion_PassesPostedContentThrough_AndReturnsDto()
    {
        var diff = new ContentHistoryDiffDto { HasComparison = true, Content = "<ins>draft</ins>" };
        _blockService.DiffContentAgainstHistoryAsync(5, 12, "<p>draft</p>", Arg.Any<CancellationToken>()).Returns(diff);
        var request = new DiffAgainstHistoryRequest { Content = "<p>draft</p>" };

        var result = await _controller.DiffAgainstHistoryVersion(5, 12, request, TestContext.Current.CancellationToken);

        Assert.Same(diff, result.Value);
        await _blockService.Received(1).DiffContentAgainstHistoryAsync(5, 12, "<p>draft</p>",
            Arg.Any<CancellationToken>());
    }

    // Denial coverage for the history endpoints: CanEditAsync defaults to true in setup, so without
    // these a removed gate would go unnoticed. Each asserts 403 (block exists) / 404 (block missing)
    // and that the underlying history service method is never reached.
    [Fact]
    public async Task GetHistory_Returns403_WhenExistsButCannotEdit()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((true, (string?)null));

        var result = await _controller.GetHistory(5, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result.Result).StatusCode);
        await _blockService.DidNotReceive().GetHistoryAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistory_Returns404_WhenBlockMissing()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((false, (string?)null));

        var result = await _controller.GetHistory(5, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
        await _blockService.DidNotReceive().GetHistoryAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistoryVersion_Returns403_WhenExistsButCannotEdit()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((true, (string?)null));

        var result = await _controller.GetHistoryVersion(5, 12, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result.Result).StatusCode);
        await _blockService.DidNotReceive().GetHistoryVersionAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistoryVersion_Returns404_WhenBlockMissing()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((false, (string?)null));

        var result = await _controller.GetHistoryVersion(5, 12, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
        await _blockService.DidNotReceive().GetHistoryVersionAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistoryVersionDiff_Returns403_WhenExistsButCannotEdit()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((true, (string?)null));

        var result = await _controller.GetHistoryVersionDiff(5, 12, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result.Result).StatusCode);
        await _blockService.DidNotReceive().GetHistoryVersionDiffAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistoryVersionDiff_Returns404_WhenBlockMissing()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((false, (string?)null));

        var result = await _controller.GetHistoryVersionDiff(5, 12, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
        await _blockService.DidNotReceive().GetHistoryVersionDiffAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DiffAgainstHistoryVersion_Returns403_WhenExistsButCannotEdit()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((true, (string?)null));
        var request = new DiffAgainstHistoryRequest { Content = "<p>draft</p>" };

        var result = await _controller.DiffAgainstHistoryVersion(5, 12, request, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result.Result).StatusCode);
        await _blockService.DidNotReceive().DiffContentAgainstHistoryAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DiffAgainstHistoryVersion_Returns404_WhenBlockMissing()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((false, (string?)null));
        var request = new DiffAgainstHistoryRequest { Content = "<p>draft</p>" };

        var result = await _controller.DiffAgainstHistoryVersion(5, 12, request, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DiffAgainstHistoryVersion_ReturnsNotFound_WhenNull()
    {
        _blockService.DiffContentAgainstHistoryAsync(5, 12, Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _controller.DiffAgainstHistoryVersion(5, 12, new DiffAgainstHistoryRequest { Content = "x" }, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region Restore / Delete

    [Fact]
    public async Task RestoreContentBlock_ReturnsNoContent_WhenRestored()
    {
        _blockService.RestoreAsync(5, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _controller.RestoreContentBlock(5, TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RestoreContentBlock_ReturnsNotFound_WhenMissing()
    {
        _blockService.RestoreAsync(5, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _controller.RestoreContentBlock(5, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteContentBlock_SoftDelete_ReturnsNoContent()
    {
        _blockService.SoftDeleteAsync(5, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _controller.DeleteContentBlock(5, permanent: false, TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
        await _blockService.DidNotReceive().PermanentlyDeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteContentBlock_PermanentWithoutAdmin_ReturnsForbidden()
    {
        var user = new AaudUser { AaudUserId = 1, LoginId = "user" };
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.Admin).Returns(false);

        var result = await _controller.DeleteContentBlock(5, permanent: true, TestContext.Current.CancellationToken);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        await _blockService.DidNotReceive().PermanentlyDeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteContentBlock_PermanentAsAdmin_ReturnsNoContent()
    {
        var user = new AaudUser { AaudUserId = 1, LoginId = "admin" };
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.Admin).Returns(true);
        _blockService.PermanentlyDeleteAsync(5, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _controller.DeleteContentBlock(5, permanent: true, TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
        await _blockService.Received(1).PermanentlyDeleteAsync(5, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Delegated editing gates + content-scoped file ops

    private static IFormFile FakeFormFile(string name, byte[]? bytes = null)
    {
        bytes ??= new byte[] { 1, 2, 3 };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name);
    }

    [Fact]
    public async Task GetContentBlock_Returns403_WhenExistsButCannotEdit()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        // Block exists (section-path row present) but the user may not edit it -> 403, not 404.
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((true, (string?)null));

        var result = await _controller.GetContentBlock(5, TestContext.Current.CancellationToken);

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
        await _blockService.DidNotReceive().GetContentBlockAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateContentOnly_Returns403_WhenCannotEdit()
    {
        _blockService.CanEditAsync(4, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(4, Arg.Any<CancellationToken>()).Returns((true, (string?)null));
        var update = new ContentOnlyUpdate { Content = "<p>x</p>", LastModifiedOn = DateTime.Now };

        var result = await _controller.UpdateContentOnly(4, update, TestContext.Current.CancellationToken);

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
        await _blockService.DidNotReceive().UpdateContentOnlyAsync(Arg.Any<int>(), Arg.Any<string>(),
            Arg.Any<DateTime?>(), Arg.Any<List<Guid>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateContentOnly_Returns404_WhenBlockMissing()
    {
        _blockService.CanEditAsync(4, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(4, Arg.Any<CancellationToken>()).Returns((false, (string?)null));
        var update = new ContentOnlyUpdate { Content = "<p>x</p>", LastModifiedOn = DateTime.Now };

        var result = await _controller.UpdateContentOnly(4, update, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetEditableBlocks_PassesThrough()
    {
        var blocks = new List<ContentBlockDto> { Block() };
        _blockService.GetEditableBlocksAsync(Arg.Any<CancellationToken>()).Returns(blocks);

        var result = await _controller.GetEditableBlocks(TestContext.Current.CancellationToken);

        Assert.Same(blocks, result.Value);
    }

    [Fact]
    public async Task SearchAttachableFiles_EditMode_Returns403_WhenCannotEdit()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((true, (string?)null));

        var result = await _controller.SearchAttachableFiles("report", 5, TestContext.Current.CancellationToken);

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
        await _blockService.DidNotReceive().SearchAttachableFilesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAttachableFiles_EditMode_Returns404_WhenBlockMissing()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((false, (string?)null));

        var result = await _controller.SearchAttachableFiles("report", 5, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
        await _blockService.DidNotReceive().SearchAttachableFilesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAttachableFiles_CreateMode_Returns403_WithoutCreateOrManage()
    {
        var user = new AaudUser { AaudUserId = 1, LoginId = "u" };
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.ManageContentBlocks).Returns(false);
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.CreateContentBlock).Returns(false);

        var result = await _controller.SearchAttachableFiles("report", null, TestContext.Current.CancellationToken);

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
    }

    [Fact]
    public async Task SearchAttachableFiles_CreateMode_AllowsCreator()
    {
        var user = new AaudUser { AaudUserId = 1, LoginId = "u" };
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.ManageContentBlocks).Returns(false);
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.CreateContentBlock).Returns(true);
        var files = new List<CmsAttachableFileDto> { new() { FriendlyName = "x" } };
        _blockService.SearchAttachableFilesAsync("report", Arg.Any<CancellationToken>()).Returns(files);

        var result = await _controller.SearchAttachableFiles("report", null, TestContext.Current.CancellationToken);

        Assert.Same(files, result.Value);
    }

    [Fact]
    public async Task CheckBlockFileName_ReturnsBadRequest_WhenBlockHasNoSectionPath()
    {
        // Matches UploadBlockFile: the block exists but is not configured for uploads.
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((true, (string?)null));

        var result = await _controller.CheckBlockFileName(5, "doc.pdf", TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CheckBlockFileName_ReturnsNotFound_WhenBlockMissing()
    {
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((false, (string?)null));

        var result = await _controller.CheckBlockFileName(5, "doc.pdf", TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CheckBlockFileName_Returns403_WhenExistsButCannotEdit()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((true, (string?)null));

        var result = await _controller.CheckBlockFileName(5, "doc.pdf", TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result.Result).StatusCode);
        await _fileService.DidNotReceive().CheckNameAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadBlockFile_BuildsRequestFromBlock()
    {
        _blockService.GetUploadSettingsAsync(5, Arg.Any<CancellationToken>())
            .Returns((true, "students", true, new List<string> { "SVMSecure.View" }));
        _fileService.CreateFileAsync(Arg.Any<CmsFileCreateRequest>(), Arg.Any<IFormFile>(), Arg.Any<CancellationToken>())
            .Returns(new CmsFileDto());
        var file = FakeFormFile("doc.pdf");
        var form = new ContentBlockFileUpload { FileName = "renamed.pdf", Overwrite = true };

        var result = await _controller.UploadBlockFile(5, form, file, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Value);
        // The folder, public flag, and permissions are taken from the block; only name/overwrite
        // come from the client.
        await _fileService.Received(1).CreateFileAsync(
            Arg.Is<CmsFileCreateRequest>(r => r.Folder == "students" && r.AllowPublicAccess == true
                && r.Permissions.Contains("SVMSecure.View") && r.FileName == "renamed.pdf" && r.Overwrite == true),
            file, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadBlockFile_RefusesUpload_WhenBlockHasNoSectionPath()
    {
        // Mirrors CheckBlockFileName: without a section path there is no upload folder, and the
        // request must not fall back to the storage root.
        _blockService.GetUploadSettingsAsync(5, Arg.Any<CancellationToken>())
            .Returns((true, (string?)null, false, new List<string>()));

        var result = await _controller.UploadBlockFile(5, new ContentBlockFileUpload(), FakeFormFile("doc.pdf"),
            TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await _fileService.DidNotReceive().CreateFileAsync(Arg.Any<CmsFileCreateRequest>(), Arg.Any<IFormFile>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadBlockFile_Returns403_WhenCannotEdit()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((true, (string?)null));

        var result = await _controller.UploadBlockFile(5, new ContentBlockFileUpload(), FakeFormFile("doc.pdf"),
            TestContext.Current.CancellationToken);

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
        await _fileService.DidNotReceive().CreateFileAsync(Arg.Any<CmsFileCreateRequest>(), Arg.Any<IFormFile>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadBlockFile_Returns404_WhenBlockMissing()
    {
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((false, (string?)null));

        var result = await _controller.UploadBlockFile(5, new ContentBlockFileUpload(), FakeFormFile("doc.pdf"),
            TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
        await _fileService.DidNotReceive().CreateFileAsync(Arg.Any<CmsFileCreateRequest>(), Arg.Any<IFormFile>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBlockFile_SoftDeletes_WhenServiceReportsEligible()
    {
        var fileGuid = Guid.NewGuid();
        _blockService.IsFileRollbackDeletableAsync(5, fileGuid, Arg.Any<CancellationToken>()).Returns(true);
        _fileService.RollbackDeleteFileAsync(fileGuid, 5, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _controller.DeleteBlockFile(5, fileGuid, TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
        // The recheck-and-delete go through the single rollback method, not a bare SoftDeleteFileAsync.
        await _fileService.Received(1).RollbackDeleteFileAsync(fileGuid, 5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBlockFile_Returns409_WhenFileBecameSharedInWindow()
    {
        // Eligible at the check, but RollbackDeleteFileAsync's recheck finds it attached elsewhere now.
        var fileGuid = Guid.NewGuid();
        _blockService.IsFileRollbackDeletableAsync(5, fileGuid, Arg.Any<CancellationToken>()).Returns(true);
        _fileService.RollbackDeleteFileAsync(fileGuid, 5, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _controller.DeleteBlockFile(5, fileGuid, TestContext.Current.CancellationToken);

        var obj = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, obj.StatusCode);
    }

    [Fact]
    public async Task DeleteBlockFile_Returns403_WhenServiceReportsIneligible()
    {
        // The service returns false for someone-else's file / another folder / attached-elsewhere.
        var fileGuid = Guid.NewGuid();
        _blockService.IsFileRollbackDeletableAsync(5, fileGuid, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _controller.DeleteBlockFile(5, fileGuid, TestContext.Current.CancellationToken);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
        await _fileService.DidNotReceive().RollbackDeleteFileAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBlockFile_Returns404_WhenFileMissing()
    {
        var fileGuid = Guid.NewGuid();
        _blockService.IsFileRollbackDeletableAsync(5, fileGuid, Arg.Any<CancellationToken>()).Returns((bool?)null);

        var result = await _controller.DeleteBlockFile(5, fileGuid, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result);
        await _fileService.DidNotReceive().RollbackDeleteFileAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBlockFile_Returns403_WhenCannotEdit()
    {
        var fileGuid = Guid.NewGuid();
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((true, (string?)null));

        var result = await _controller.DeleteBlockFile(5, fileGuid, TestContext.Current.CancellationToken);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
        await _blockService.DidNotReceive().IsFileRollbackDeletableAsync(Arg.Any<int>(), Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBlockFile_Returns404_WhenBlockMissing()
    {
        var fileGuid = Guid.NewGuid();
        _blockService.CanEditAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _blockService.GetSectionPathAsync(5, Arg.Any<CancellationToken>()).Returns((false, (string?)null));

        var result = await _controller.DeleteBlockFile(5, fileGuid, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result);
        await _blockService.DidNotReceive().IsFileRollbackDeletableAsync(Arg.Any<int>(), Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
