using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Models;
using Viper.Areas.CMS.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;
using Viper.Models.VIPER;
using Viper.Services;

namespace Viper.test.CMS;

/// <summary>
/// Tests for CmsContentBlockService: filtering, create/update with permission and file deltas,
/// history semantics (previous version is stored, stamped with its original author/time),
/// concurrency conflicts, soft delete/restore, and permanent delete cascade.
/// </summary>
public sealed class CmsContentBlockServiceTests : IDisposable
{
    private readonly VIPERContext _context;
    private readonly RAPSContext _rapsContext;
    private readonly IHtmlSanitizerService _sanitizer;
    private readonly IUserHelper _userHelper;
    private readonly CmsContentBlockService _service;

    public CmsContentBlockServiceTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _rapsContext = new RAPSContext(new DbContextOptionsBuilder<RAPSContext>()
            .UseInMemoryDatabase("RAPS_" + Guid.NewGuid()).Options);
        _sanitizer = Substitute.For<IHtmlSanitizerService>();
        _sanitizer.Sanitize(Arg.Any<string>()).Returns(callInfo => callInfo.ArgAt<string>(0));
        // Pass-through so diff tests assert on the real htmldiff.net markers, not a sanitized copy.
        _sanitizer.SanitizeDiff(Arg.Any<string>()).Returns(callInfo => callInfo.ArgAt<string>(0));
        _userHelper = Substitute.For<IUserHelper>();

        _service = new CmsContentBlockService(_context, _rapsContext, _sanitizer, _userHelper);
    }

    public void Dispose()
    {
        _context.Dispose();
        _rapsContext.Dispose();
    }

    private async Task<ContentBlock> SeedBlockAsync(Action<ContentBlock>? customize = null)
    {
        var block = new ContentBlock
        {
            Content = "<p>original</p>",
            Title = "Seeded Block",
            System = "Viper",
            ViperSectionPath = "cats",
            Page = "home",
            BlockOrder = 1,
            FriendlyName = "seeded-block-" + Guid.NewGuid().ToString("N")[..8],
            ModifiedOn = DateTime.Now.AddDays(-2),
            ModifiedBy = "originalAuthor"
        };
        customize?.Invoke(block);
        _context.ContentBlocks.Add(block);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return block;
    }

    private static CMSBlockAddEdit MakeRequest(ContentBlock block, Action<CMSBlockAddEdit>? customize = null)
    {
        var request = new CMSBlockAddEdit
        {
            ContentBlockId = block.ContentBlockId,
            Content = block.Content,
            Title = block.Title,
            System = block.System,
            Page = block.Page,
            ViperSectionPath = block.ViperSectionPath,
            BlockOrder = block.BlockOrder,
            FriendlyName = block.FriendlyName,
            AllowPublicAccess = block.AllowPublicAccess,
            LastModifiedOn = block.ModifiedOn
        };
        customize?.Invoke(request);
        return request;
    }

    #region List / Get

    [Fact]
    public async Task GetContentBlocks_FiltersByStatus()
    {
        await SeedBlockAsync();
        await SeedBlockAsync(b => b.DeletedOn = DateTime.Now);

        var active = await _service.GetContentBlocksAsync("active", null, null, null, null, 1, 50, "title", false, TestContext.Current.CancellationToken);
        var deleted = await _service.GetContentBlocksAsync("deleted", null, null, null, null, 1, 50, "title", false, TestContext.Current.CancellationToken);
        var all = await _service.GetContentBlocksAsync("all", null, null, null, null, 1, 50, "title", false, TestContext.Current.CancellationToken);

        Assert.Single(active.Blocks);
        Assert.Single(deleted.Blocks);
        Assert.Equal(2, all.Blocks.Count);
        Assert.Equal(2, all.Total);
    }

    [Fact]
    public async Task GetContentBlocks_PagesSortsAndReturnsTotal()
    {
        await SeedBlockAsync(b => b.Title = "Charlie");
        await SeedBlockAsync(b => b.Title = "Alpha");
        await SeedBlockAsync(b => b.Title = "Bravo");

        var page1 = await _service.GetContentBlocksAsync("active", null, null, null, null, 1, 2, "title", false,
            TestContext.Current.CancellationToken);
        var page2 = await _service.GetContentBlocksAsync("active", null, null, null, null, 2, 2, "title", false,
            TestContext.Current.CancellationToken);

        // Total counts all matches; the page returns only its slice, sorted by title across pages.
        Assert.Equal(3, page1.Total);
        Assert.Equal(new[] { "Alpha", "Bravo" }, page1.Blocks.Select(b => b.Title));
        Assert.Single(page2.Blocks);
        Assert.Equal("Charlie", page2.Blocks[0].Title);

        var desc = await _service.GetContentBlocksAsync("active", null, null, null, null, 1, 1, "title", true,
            TestContext.Current.CancellationToken);
        Assert.Equal("Charlie", desc.Blocks[0].Title);
    }

    [Fact]
    public async Task GetContentBlocks_PageZero_ClampsToFirstPage()
    {
        // ApiPagination admits page=0; Skip(-perPage) would throw, so the service clamps.
        await SeedBlockAsync(b => b.Title = "Alpha");

        var result = await _service.GetContentBlocksAsync("active", null, null, null, null, 0, 0, "title", false,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, result.Total);
        Assert.Single(result.Blocks);
    }

    [Fact]
    public async Task GetContentBlocks_ListOmitsContent()
    {
        await SeedBlockAsync();

        var list = await _service.GetContentBlocksAsync("active", null, null, null, null, 1, 50, "title", false, TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, list.Blocks[0].Content);
    }

    [Fact]
    public async Task GetContentBlock_ReturnsContentAndRelations()
    {
        var block = await SeedBlockAsync(b =>
            b.ContentBlockToPermissions.Add(new ContentBlockToPermission { Permission = "SVMSecure.CATS" }));

        var dto = await _service.GetContentBlockAsync(block.ContentBlockId, TestContext.Current.CancellationToken);

        Assert.NotNull(dto);
        Assert.Equal("<p>original</p>", dto.Content);
        Assert.Equal(new List<string> { "SVMSecure.CATS" }, dto.Permissions);
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_SavesBlockWithPermissionsAndNoHistory()
    {
        var request = new CMSBlockAddEdit
        {
            ContentBlockId = 0,
            Content = "<p>new</p>",
            Title = "New Block",
            System = "Viper",
            AllowPublicAccess = false,
            Permissions = new List<string> { "SVMSecure.CATS" }
        };

        var dto = await _service.CreateContentBlockAsync(request, TestContext.Current.CancellationToken);

        Assert.True(dto.ContentBlockId > 0);
        Assert.Equal(new List<string> { "SVMSecure.CATS" }, dto.Permissions);
        // Legacy semantics: history holds previous versions only, so a new block has none.
        Assert.Empty(_context.ContentHistories.Where(h => h.ContentBlockId == dto.ContentBlockId));
    }

    [Fact]
    public async Task Create_DuplicateFriendlyName_Throws()
    {
        var existing = await SeedBlockAsync();
        var request = new CMSBlockAddEdit
        {
            ContentBlockId = 0,
            Content = "x",
            Title = "Dup",
            System = "Viper",
            AllowPublicAccess = false,
            FriendlyName = existing.FriendlyName
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateContentBlockAsync(request, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Create_UnknownFileGuid_Throws()
    {
        var request = new CMSBlockAddEdit
        {
            ContentBlockId = 0,
            Content = "x",
            Title = "Has Bad File",
            System = "Viper",
            AllowPublicAccess = false,
            FileGuids = new List<Guid> { Guid.NewGuid() }
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateContentBlockAsync(request, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_WritesPreviousVersionToHistory()
    {
        var block = await SeedBlockAsync();
        var originalModifiedOn = block.ModifiedOn;
        var request = MakeRequest(block, r => r.Content = "<p>updated</p>");

        var dto = await _service.UpdateContentBlockAsync(block.ContentBlockId, request, TestContext.Current.CancellationToken);

        Assert.Equal("<p>updated</p>", dto!.Content);
        var history = Assert.Single(_context.ContentHistories.Where(h => h.ContentBlockId == block.ContentBlockId));
        Assert.Equal("<p>original</p>", history.ContentBlockContent);
        Assert.Equal("originalAuthor", history.ModifiedBy);
        Assert.Equal(originalModifiedOn, history.ModifiedOn);
    }

    [Fact]
    public async Task Update_StaleLastModifiedOn_ThrowsConcurrency()
    {
        var block = await SeedBlockAsync();
        var request = MakeRequest(block, r => r.LastModifiedOn = block.ModifiedOn.AddMinutes(-5));

        await Assert.ThrowsAsync<CmsConcurrencyException>(
            () => _service.UpdateContentBlockAsync(block.ContentBlockId, request, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Update_NullLastModifiedOn_Throws()
    {
        var block = await SeedBlockAsync();
        var request = MakeRequest(block, r => r.LastModifiedOn = null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateContentBlockAsync(block.ContentBlockId, request, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Update_UnknownFileGuid_Throws()
    {
        var block = await SeedBlockAsync();
        var request = MakeRequest(block, r => r.FileGuids = new List<Guid> { Guid.NewGuid() });

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateContentBlockAsync(block.ContentBlockId, request, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Update_AppliesPermissionAndFileDeltas()
    {
        var file = new Models.VIPER.File
        {
            FileGuid = Guid.NewGuid(),
            FilePath = @"C:\FakeRoot\cats\attach.pdf",
            Folder = "cats",
            FriendlyName = "cats-attach.pdf",
            Description = "",
            ModifiedBy = "test",
            ModifiedOn = DateTime.Now
        };
        _context.Files.Add(file);
        var block = await SeedBlockAsync(b =>
            b.ContentBlockToPermissions.Add(new ContentBlockToPermission { Permission = "SVMSecure.Old" }));

        var request = MakeRequest(block, r =>
        {
            r.Permissions = new List<string> { "SVMSecure.New" };
            r.FileGuids = new List<Guid> { file.FileGuid };
        });

        var dto = await _service.UpdateContentBlockAsync(block.ContentBlockId, request, TestContext.Current.CancellationToken);

        Assert.Equal(new List<string> { "SVMSecure.New" }, dto!.Permissions);
        Assert.Single(dto.Files);
        Assert.Equal("cats-attach.pdf", dto.Files[0].FriendlyName);
    }

    [Fact]
    public async Task UpdateContentOnly_PreservesOtherFieldsAndWritesHistory()
    {
        var block = await SeedBlockAsync();

        var dto = await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>quick edit</p>", block.ModifiedOn,
            ct: TestContext.Current.CancellationToken);

        Assert.Equal("<p>quick edit</p>", dto!.Content);
        Assert.Equal("Seeded Block", dto.Title);
        var history = Assert.Single(_context.ContentHistories.Where(h => h.ContentBlockId == block.ContentBlockId));
        Assert.Equal("<p>original</p>", history.ContentBlockContent);
    }

    #endregion

    #region Delete / Restore / History

    [Fact]
    public async Task SoftDeleteAndRestore_ToggleDeletedOn()
    {
        var block = await SeedBlockAsync();

        Assert.True(await _service.SoftDeleteAsync(block.ContentBlockId, TestContext.Current.CancellationToken));
        Assert.NotNull((await _context.ContentBlocks.SingleAsync(TestContext.Current.CancellationToken)).DeletedOn);

        Assert.True(await _service.RestoreAsync(block.ContentBlockId, TestContext.Current.CancellationToken));
        Assert.Null((await _context.ContentBlocks.SingleAsync(TestContext.Current.CancellationToken)).DeletedOn);
    }

    [Fact]
    public async Task PermanentDelete_RemovesBlockAndChildren()
    {
        var block = await SeedBlockAsync(b =>
        {
            b.ContentBlockToPermissions.Add(new ContentBlockToPermission { Permission = "SVMSecure" });
            b.ContentHistories.Add(new ContentHistory
            {
                ContentBlockContent = "old",
                ModifiedOn = DateTime.Now.AddDays(-3),
                ModifiedBy = "x"
            });
        });

        var result = await _service.PermanentlyDeleteAsync(block.ContentBlockId, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Empty(await _context.ContentBlocks.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await _context.ContentHistories.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await _context.ContentBlockToPermissions.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task History_ListAndVersionRetrieval()
    {
        var block = await SeedBlockAsync();
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v2</p>", block.ModifiedOn, ct: TestContext.Current.CancellationToken);
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v3</p>", block.ModifiedOn, ct: TestContext.Current.CancellationToken);

        var history = await _service.GetHistoryAsync(block.ContentBlockId, TestContext.Current.CancellationToken);
        Assert.Equal(2, history.Count);

        var oldest = await _service.GetHistoryVersionAsync(block.ContentBlockId, history[^1].ContentHistoryId,
            TestContext.Current.CancellationToken);
        Assert.Equal("<p>original</p>", oldest!.Content);
    }

    #endregion

    #region Cross-block edit history

    private async Task<ContentBlock> SeedBlockWithHistoryAsync(string title, string page, bool deleted,
        params (string author, DateTime when)[] versions)
    {
        var block = await SeedBlockAsync(b =>
        {
            b.Title = title;
            b.Page = page;
            b.DeletedOn = deleted ? DateTime.Now : null;
            foreach (var (author, when) in versions)
            {
                b.ContentHistories.Add(new ContentHistory
                {
                    ContentBlockContent = $"<p>{author}@{when:o}</p>",
                    ModifiedOn = when,
                    ModifiedBy = author
                });
            }
        });
        return block;
    }

    [Fact]
    public async Task GetHistoryEntries_ReturnsAcrossBlocks_NewestFirst_WithBlockInfo()
    {
        var now = DateTime.Now;
        await SeedBlockWithHistoryAsync("Alpha", "home", deleted: false,
            ("editorX", now.AddDays(-3)), ("editorY", now.AddDays(-1)));
        await SeedBlockWithHistoryAsync("Beta", "about", deleted: true,
            ("editorX", now.AddDays(-2)));

        var entries = await _service.GetHistoryEntriesAsync(new CmsContentHistoryFilter(), 1, 50,
            TestContext.Current.CancellationToken);

        Assert.Equal(3, entries.Count);
        // Newest first: editorY@-1 (Alpha), editorX@-2 (Beta), editorX@-3 (Alpha).
        Assert.Equal("editorY", entries[0].ModifiedBy);
        Assert.Equal("Alpha", entries[0].Title);
        Assert.Equal("home", entries[0].Page);
        Assert.False(entries[0].BlockDeleted);
        Assert.Equal("Beta", entries[1].Title);
        Assert.True(entries[1].BlockDeleted);
    }

    [Fact]
    public async Task GetHistoryEntries_FiltersByEditorBlockSearchAndDate()
    {
        var now = DateTime.Now;
        var alpha = await SeedBlockWithHistoryAsync("Alpha", "home", deleted: false,
            ("editorX", now.AddDays(-3)), ("editorY", now.AddDays(-1)));
        await SeedBlockWithHistoryAsync("Beta", "about", deleted: false,
            ("editorX", now.AddDays(-2)));

        var byEditor = await _service.GetHistoryEntriesAsync(new CmsContentHistoryFilter { ModifiedBy = "editorX" },
            1, 50, TestContext.Current.CancellationToken);
        Assert.Equal(2, byEditor.Count);
        Assert.All(byEditor, e => Assert.Equal("editorX", e.ModifiedBy));

        var byBlock = await _service.GetHistoryEntriesAsync(new CmsContentHistoryFilter { ContentBlockId = alpha.ContentBlockId },
            1, 50, TestContext.Current.CancellationToken);
        Assert.Equal(2, byBlock.Count);
        Assert.All(byBlock, e => Assert.Equal(alpha.ContentBlockId, e.ContentBlockId));

        var bySearch = await _service.GetHistoryEntriesAsync(new CmsContentHistoryFilter { Search = "Beta" },
            1, 50, TestContext.Current.CancellationToken);
        Assert.Single(bySearch);
        Assert.Equal("Beta", bySearch[0].Title);

        // To is inclusive through end of the given day; From excludes older rows.
        var byDate = await _service.GetHistoryEntriesAsync(
            new CmsContentHistoryFilter { From = now.AddDays(-2).Date, To = now.Date },
            1, 50, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(byDate, e => e.ModifiedOn < now.AddDays(-2).Date);
    }

    [Fact]
    public async Task GetHistoryEntries_PaginatesAndCounts()
    {
        var now = DateTime.Now;
        await SeedBlockWithHistoryAsync("Alpha", "home", deleted: false,
            ("a", now.AddDays(-3)), ("b", now.AddDays(-2)), ("c", now.AddDays(-1)));

        var filter = new CmsContentHistoryFilter();
        var firstPage = await _service.GetHistoryEntriesAsync(filter, 1, 2, TestContext.Current.CancellationToken);
        var secondPage = await _service.GetHistoryEntriesAsync(filter, 2, 2, TestContext.Current.CancellationToken);
        var total = await _service.GetHistoryEntryCountAsync(filter, TestContext.Current.CancellationToken);

        Assert.Equal(2, firstPage.Count);
        Assert.Single(secondPage);
        Assert.Equal(3, total);
    }

    #endregion

    #region Version diff

    [Fact]
    public async Task GetHistoryVersionDiff_AgainstPreviousVersion_MarksChangesAndReSanitizes()
    {
        var block = await SeedBlockAsync();
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v2</p>", block.ModifiedOn, ct: TestContext.Current.CancellationToken);
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v3</p>", block.ModifiedOn, ct: TestContext.Current.CancellationToken);

        // History newest-first is [v2, original]; v2 has a predecessor (original) to diff against.
        var history = await _service.GetHistoryAsync(block.ContentBlockId, TestContext.Current.CancellationToken);
        var v2 = history[0];

        var diff = await _service.GetHistoryVersionDiffAsync(block.ContentBlockId, v2.ContentHistoryId,
            TestContext.Current.CancellationToken);

        Assert.NotNull(diff);
        Assert.True(diff.HasComparison);
        Assert.True(diff.HasChanges);
        Assert.Equal("originalAuthor", diff.OldModifiedBy);
        Assert.Contains("<ins", diff.Content);
        Assert.Contains("<del", diff.Content);
        Assert.Contains("v2", diff.Content);
        // The merged diff is re-sanitized (not the raw library output) before it leaves the service.
        _sanitizer.Received().SanitizeDiff(Arg.Any<string>());
    }

    [Fact]
    public async Task GetHistoryVersionDiff_OriginalVersion_HasNoComparison()
    {
        var block = await SeedBlockAsync();
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v2</p>", block.ModifiedOn, ct: TestContext.Current.CancellationToken);

        var history = await _service.GetHistoryAsync(block.ContentBlockId, TestContext.Current.CancellationToken);
        var original = history[^1];

        var diff = await _service.GetHistoryVersionDiffAsync(block.ContentBlockId, original.ContentHistoryId,
            TestContext.Current.CancellationToken);

        Assert.NotNull(diff);
        Assert.False(diff.HasComparison);
        Assert.Equal("<p>original</p>", diff.Content);
        Assert.DoesNotContain("<ins", diff.Content);
    }

    [Fact]
    public async Task GetHistoryVersionDiff_UnknownVersion_ReturnsNull()
    {
        var block = await SeedBlockAsync();

        var diff = await _service.GetHistoryVersionDiffAsync(block.ContentBlockId, 999999,
            TestContext.Current.CancellationToken);

        Assert.Null(diff);
    }

    [Fact]
    public async Task DiffContentAgainstHistory_ComparesDraftToSelectedVersion()
    {
        var block = await SeedBlockAsync();
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v2</p>", block.ModifiedOn, ct: TestContext.Current.CancellationToken);

        var history = await _service.GetHistoryAsync(block.ContentBlockId, TestContext.Current.CancellationToken);
        var original = history[^1];

        var diff = await _service.DiffContentAgainstHistoryAsync(block.ContentBlockId, original.ContentHistoryId,
            "<p>current draft</p>", TestContext.Current.CancellationToken);

        Assert.NotNull(diff);
        Assert.True(diff.HasComparison);
        Assert.True(diff.HasChanges);
        Assert.Equal("originalAuthor", diff.OldModifiedBy);
        Assert.Contains("<ins", diff.Content);
        Assert.Contains("current draft", diff.Content);
    }

    [Fact]
    public async Task DiffContentAgainstHistory_IdenticalContent_ReportsNoChanges()
    {
        var block = await SeedBlockAsync();
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v2</p>", block.ModifiedOn, ct: TestContext.Current.CancellationToken);

        var history = await _service.GetHistoryAsync(block.ContentBlockId, TestContext.Current.CancellationToken);
        var original = history[^1]; // holds "<p>original</p>"

        // Current draft is byte-identical to the selected version: htmldiff emits no ins/del.
        var diff = await _service.DiffContentAgainstHistoryAsync(block.ContentBlockId, original.ContentHistoryId,
            "<p>original</p>", TestContext.Current.CancellationToken);

        Assert.NotNull(diff);
        Assert.True(diff.HasComparison);
        Assert.False(diff.HasChanges);
        Assert.DoesNotContain("<ins", diff.Content);
        Assert.DoesNotContain("<del", diff.Content);
    }

    #endregion

    #region Delegated edit authorization (CanEditAsync)

    private static AaudUser DelegateUser() => new() { AaudUserId = 10, LoginId = "delegate", MothraId = "m10" };

    private void SignInAs(AaudUser user, bool isManager, params string[] permissions)
    {
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.ManageContentBlocks).Returns(isManager);
        _userHelper.HasPermission(_rapsContext, user, "SVMSecure")
            .Returns(permissions.Contains("SVMSecure"));
        _userHelper.GetAllPermissions(_rapsContext, user)
            .Returns(permissions.Select(p => new TblPermission { Permission = p }).ToList());
    }

    private async Task<Models.VIPER.File> SeedFileAsync(string friendlyName, string folder = "cats",
        string modifiedBy = "test", Action<Models.VIPER.File>? customize = null)
    {
        var file = new Models.VIPER.File
        {
            FileGuid = Guid.NewGuid(),
            FilePath = $@"C:\FakeRoot\{folder}\{friendlyName}",
            Folder = folder,
            FriendlyName = friendlyName,
            Description = "",
            ModifiedBy = modifiedBy,
            ModifiedOn = DateTime.Now
        };
        customize?.Invoke(file);
        _context.Files.Add(file);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return file;
    }

    [Fact]
    public async Task CanEdit_Manager_CanEditAnyBlock()
    {
        // Manager holds no edit permission and the block delegates none, yet manage overrides.
        var block = await SeedBlockAsync();
        SignInAs(DelegateUser(), isManager: true);

        Assert.True(await _service.CanEditAsync(block.ContentBlockId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CanEdit_HolderOfEditPermission_CanEdit()
    {
        var block = await SeedBlockAsync(b =>
            b.ContentBlockToEditPermissions.Add(new ContentBlockToEditPermission { Permission = "SVMSecure.Editors" }));
        SignInAs(DelegateUser(), isManager: false, "SVMSecure.Editors");

        Assert.True(await _service.CanEditAsync(block.ContentBlockId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CanEdit_HolderOfViewPermissionOnly_CannotEdit()
    {
        var block = await SeedBlockAsync(b =>
        {
            b.ContentBlockToPermissions.Add(new ContentBlockToPermission { Permission = "SVMSecure.Viewers" });
            b.ContentBlockToEditPermissions.Add(new ContentBlockToEditPermission { Permission = "SVMSecure.Editors" });
        });
        SignInAs(DelegateUser(), isManager: false, "SVMSecure.Viewers");

        Assert.False(await _service.CanEditAsync(block.ContentBlockId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CanEdit_Anonymous_CannotEdit()
    {
        var block = await SeedBlockAsync(b =>
            b.ContentBlockToEditPermissions.Add(new ContentBlockToEditPermission { Permission = "SVMSecure.Editors" }));
        _userHelper.GetCurrentUser().Returns((AaudUser?)null);

        Assert.False(await _service.CanEditAsync(block.ContentBlockId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CanEdit_EmptyEditList_IsManagerOnly()
    {
        // Empty edit list means manager-only, NOT the view-list's empty-means-all-SVMSecure rule.
        var block = await SeedBlockAsync();
        SignInAs(DelegateUser(), isManager: false, "SVMSecure.Editors");

        Assert.False(await _service.CanEditAsync(block.ContentBlockId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CanEdit_SoftDeletedBlock_NotEditableByDelegate()
    {
        var block = await SeedBlockAsync(b =>
        {
            b.DeletedOn = DateTime.Now;
            b.ContentBlockToEditPermissions.Add(new ContentBlockToEditPermission { Permission = "SVMSecure.Editors" });
        });
        SignInAs(DelegateUser(), isManager: false, "SVMSecure.Editors");

        Assert.False(await _service.CanEditAsync(block.ContentBlockId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CanEdit_MatchIsCaseInsensitive()
    {
        var block = await SeedBlockAsync(b =>
            b.ContentBlockToEditPermissions.Add(new ContentBlockToEditPermission { Permission = "SVMSecure.Editors" }));
        SignInAs(DelegateUser(), isManager: false, "svmsecure.editors");

        Assert.True(await _service.CanEditAsync(block.ContentBlockId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CanEdit_MissingBlock_ReturnsFalseEvenForManager()
    {
        SignInAs(DelegateUser(), isManager: true);

        Assert.False(await _service.CanEditAsync(999999, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Content-only update: file deltas + edit permissions

    [Fact]
    public async Task UpdateContentOnly_WithFileGuids_ReplacesAttachments()
    {
        var keep = await SeedFileAsync("keep.pdf");
        var add = await SeedFileAsync("add.pdf");
        var block = await SeedBlockAsync(b =>
            b.ContentBlockToFiles.Add(new ContentBlockToFile { FileGuid = keep.FileGuid }));
        SignInAs(DelegateUser(), isManager: false, "SVMSecure");

        var dto = await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>edit</p>", block.ModifiedOn,
            new List<Guid> { add.FileGuid }, TestContext.Current.CancellationToken);

        Assert.NotNull(dto);
        var file = Assert.Single(dto.Files);
        Assert.Equal("add.pdf", file.FriendlyName);
    }

    [Fact]
    public async Task UpdateContentOnly_WithUnknownFileGuid_Throws()
    {
        var block = await SeedBlockAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateContentOnlyAsync(
            block.ContentBlockId, "<p>edit</p>", block.ModifiedOn, new List<Guid> { Guid.NewGuid() },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateContentOnly_WithFileGuids_LeavesSettingsUntouched()
    {
        var file = await SeedFileAsync("cats-attach.pdf");
        var block = await SeedBlockAsync(b =>
        {
            b.ContentBlockToPermissions.Add(new ContentBlockToPermission { Permission = "SVMSecure.Viewers" });
            b.ContentBlockToEditPermissions.Add(new ContentBlockToEditPermission { Permission = "SVMSecure.Editors" });
        });
        SignInAs(DelegateUser(), isManager: false, "SVMSecure");

        var dto = await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>edit</p>", block.ModifiedOn,
            new List<Guid> { file.FileGuid }, TestContext.Current.CancellationToken);

        Assert.NotNull(dto);
        // The content-only path never touches title/system/permission fields.
        Assert.Equal("Seeded Block", dto.Title);
        Assert.Equal("Viper", dto.System);
        Assert.Equal(new List<string> { "SVMSecure.Viewers" }, dto.Permissions);
        Assert.Equal(new List<string> { "SVMSecure.Editors" }, dto.EditPermissions);
    }

    [Fact]
    public async Task UpdateContentOnly_WithoutFileGuids_LeavesAttachmentsAlone()
    {
        var file = await SeedFileAsync("cats-attach.pdf");
        var block = await SeedBlockAsync(b =>
            b.ContentBlockToFiles.Add(new ContentBlockToFile { FileGuid = file.FileGuid }));

        var dto = await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>edit</p>", block.ModifiedOn,
            ct: TestContext.Current.CancellationToken);

        Assert.NotNull(dto);
        var attached = Assert.Single(dto.Files);
        Assert.Equal("cats-attach.pdf", attached.FriendlyName);
    }

    [Fact]
    public async Task UpdateContentOnly_RejectsAttachingFileTheUserCannotDownload()
    {
        // A delegate who guesses a restricted file's GUID must not be able to attach it (the
        // attachment list would leak its name); same rules as the attachable-files search.
        var restricted = await SeedFileAsync("restricted.pdf", customize: f =>
            f.FileToPermissions.Add(new FileToPermission { Permission = "SVMSecure.SchoolAdmin" }));
        var block = await SeedBlockAsync();
        SignInAs(DelegateUser(), isManager: false, "SVMSecure");

        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateContentOnlyAsync(
            block.ContentBlockId, "<p>edit</p>", block.ModifiedOn, new List<Guid> { restricted.FileGuid },
            TestContext.Current.CancellationToken));
        Assert.Empty(_context.ContentBlockToFiles.Where(f => f.ContentBlockId == block.ContentBlockId));
    }

    [Fact]
    public async Task UpdateContentOnly_AllowsAttachingFileTheDelegateUploaded()
    {
        // An inline upload inherits the block's VIEW permission, which a delegate need not hold; they
        // must still be able to attach the file they just uploaded (ModifiedBy = the delegate) INTO
        // this block's folder ("cats" for both the seeded block and file), even though its permission
        // would otherwise fail the download-access check.
        var uploaded = await SeedFileAsync("uploaded.pdf", folder: "cats", modifiedBy: "delegate", customize: f =>
            f.FileToPermissions.Add(new FileToPermission { Permission = "SVMSecure.SchoolAdmin" }));
        var block = await SeedBlockAsync();
        SignInAs(DelegateUser(), isManager: false, "SVMSecure");

        var dto = await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>edit</p>", block.ModifiedOn,
            new List<Guid> { uploaded.FileGuid }, TestContext.Current.CancellationToken);

        Assert.NotNull(dto);
        var attached = Assert.Single(dto.Files);
        Assert.Equal("uploaded.pdf", attached.FriendlyName);
    }

    [Fact]
    public async Task UpdateContentOnly_RejectsAttachingUploadedFileFromAnotherFolder()
    {
        // The uploader exception is scoped to the block's own folder: a delegate cannot take a
        // restricted file they uploaded for a different section ("faculty") and attach it to a block
        // in another section ("cats"), which would leak the file's name/URL through the target block's
        // attachment list. Mirrors the folder scope on the rollback-delete rule.
        var uploaded = await SeedFileAsync("faculty-secret.pdf", folder: "faculty", modifiedBy: "delegate",
            customize: f => f.FileToPermissions.Add(new FileToPermission { Permission = "SVMSecure.SchoolAdmin" }));
        var block = await SeedBlockAsync();
        SignInAs(DelegateUser(), isManager: false, "SVMSecure");

        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateContentOnlyAsync(
            block.ContentBlockId, "<p>edit</p>", block.ModifiedOn, new List<Guid> { uploaded.FileGuid },
            TestContext.Current.CancellationToken));
        Assert.Empty(_context.ContentBlockToFiles.Where(f => f.ContentBlockId == block.ContentBlockId));
    }

    [Fact]
    public async Task UpdateContentOnly_RejectsAttachingDeletedFile()
    {
        // The search filters deleted files out; attaching one by a known GUID would resurrect
        // it into an active block, so the attach guard treats deleted as not attachable.
        var deleted = await SeedFileAsync("deleted.pdf", customize: f => f.DeletedOn = DateTime.Now);
        var block = await SeedBlockAsync();
        SignInAs(DelegateUser(), isManager: false, "SVMSecure");

        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateContentOnlyAsync(
            block.ContentBlockId, "<p>edit</p>", block.ModifiedOn, new List<Guid> { deleted.FileGuid },
            TestContext.Current.CancellationToken));
        Assert.Empty(_context.ContentBlockToFiles.Where(f => f.ContentBlockId == block.ContentBlockId));
    }

    [Fact]
    public async Task UpdateContentOnly_KeepsExistingRestrictedAttachment()
    {
        // A restricted file a manager already attached must not fail the delegate's save when
        // the client resends the unchanged attachment set.
        var restricted = await SeedFileAsync("restricted.pdf", customize: f =>
            f.FileToPermissions.Add(new FileToPermission { Permission = "SVMSecure.SchoolAdmin" }));
        var block = await SeedBlockAsync(b =>
            b.ContentBlockToFiles.Add(new ContentBlockToFile { FileGuid = restricted.FileGuid }));
        SignInAs(DelegateUser(), isManager: false, "SVMSecure");

        var dto = await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>edit</p>", block.ModifiedOn,
            new List<Guid> { restricted.FileGuid }, TestContext.Current.CancellationToken);

        Assert.NotNull(dto);
        var kept = Assert.Single(dto.Files);
        Assert.Equal("restricted.pdf", kept.FriendlyName);
    }

    [Fact]
    public async Task UpdateContentOnly_StaleLastModifiedOn_ThrowsConcurrency()
    {
        var block = await SeedBlockAsync();

        await Assert.ThrowsAsync<CmsConcurrencyException>(() => _service.UpdateContentOnlyAsync(
            block.ContentBlockId, "<p>edit</p>", block.ModifiedOn.AddMinutes(-5),
            ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Update_AppliesEditPermissionDeltas()
    {
        var block = await SeedBlockAsync(b =>
            b.ContentBlockToEditPermissions.Add(new ContentBlockToEditPermission { Permission = "SVMSecure.OldEditors" }));
        var request = MakeRequest(block, r => r.EditPermissions = new List<string> { "SVMSecure.NewEditors" });

        var dto = await _service.UpdateContentBlockAsync(block.ContentBlockId, request, TestContext.Current.CancellationToken);

        Assert.Equal(new List<string> { "SVMSecure.NewEditors" }, dto!.EditPermissions);
    }

    [Fact]
    public async Task CreateThenGet_RoundTripsEditPermissions()
    {
        var request = new CMSBlockAddEdit
        {
            ContentBlockId = 0,
            Content = "<p>new</p>",
            Title = "Delegated Block",
            System = "Viper",
            AllowPublicAccess = false,
            EditPermissions = new List<string> { "SVMSecure.Editors" }
        };

        var created = await _service.CreateContentBlockAsync(request, TestContext.Current.CancellationToken);
        // Drop the tracked graph so the fetch verifies GetContentBlockAsync actually loads the
        // edit-permission navigation from the store, not a still-tracked instance from the create.
        _context.ChangeTracker.Clear();
        var fetched = await _service.GetContentBlockAsync(created.ContentBlockId, TestContext.Current.CancellationToken);

        Assert.Equal(new List<string> { "SVMSecure.Editors" }, created.EditPermissions);
        Assert.Equal(new List<string> { "SVMSecure.Editors" }, fetched!.EditPermissions);
    }

    #endregion

    #region Editable listing + attachable search

    [Fact]
    public async Task GetEditableBlocks_FiltersByIntersection_AndExcludesDeleted()
    {
        await SeedBlockAsync(b =>
        {
            b.Title = "Mine";
            b.ContentBlockToEditPermissions.Add(new ContentBlockToEditPermission { Permission = "SVMSecure.Editors" });
        });
        await SeedBlockAsync(b =>
        {
            b.Title = "NotMine";
            b.ContentBlockToEditPermissions.Add(new ContentBlockToEditPermission { Permission = "SVMSecure.Others" });
        });
        await SeedBlockAsync(b =>
        {
            b.Title = "DeletedMine";
            b.DeletedOn = DateTime.Now;
            b.ContentBlockToEditPermissions.Add(new ContentBlockToEditPermission { Permission = "SVMSecure.Editors" });
        });
        SignInAs(DelegateUser(), isManager: false, "SVMSecure.Editors");

        var blocks = await _service.GetEditableBlocksAsync(TestContext.Current.CancellationToken);

        var only = Assert.Single(blocks);
        Assert.Equal("Mine", only.Title);
        Assert.Equal(string.Empty, only.Content);
    }

    [Fact]
    public async Task GetEditableBlocks_Anonymous_ReturnsEmpty()
    {
        await SeedBlockAsync(b =>
            b.ContentBlockToEditPermissions.Add(new ContentBlockToEditPermission { Permission = "SVMSecure.Editors" }));
        _userHelper.GetCurrentUser().Returns((AaudUser?)null);

        var blocks = await _service.GetEditableBlocksAsync(TestContext.Current.CancellationToken);

        Assert.Empty(blocks);
    }

    [Fact]
    public async Task GetEditableBlocks_Manager_ReturnsEmpty()
    {
        // Documented contract: delegated matches only. Managers work from the full list page,
        // even when their own permissions happen to intersect a block's edit list.
        await SeedBlockAsync(b =>
            b.ContentBlockToEditPermissions.Add(new ContentBlockToEditPermission { Permission = "SVMSecure.Editors" }));
        SignInAs(DelegateUser(), isManager: true, "SVMSecure.Editors");

        var blocks = await _service.GetEditableBlocksAsync(TestContext.Current.CancellationToken);

        Assert.Empty(blocks);
    }

    [Fact]
    public async Task SearchAttachableFiles_ShortTerm_ReturnsEmpty()
    {
        await SeedFileAsync("report.pdf");

        var results = await _service.SearchAttachableFilesAsync("r", TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAttachableFiles_MatchesFriendlyName_MinimalDto()
    {
        var file = await SeedFileAsync("annual-report.pdf");
        await SeedFileAsync("unrelated.pdf");
        SignInAs(DelegateUser(), isManager: false, "SVMSecure");

        var results = await _service.SearchAttachableFilesAsync("report", TestContext.Current.CancellationToken);

        var match = Assert.Single(results);
        Assert.Equal(file.FileGuid, match.FileGuid);
        Assert.Equal("annual-report.pdf", match.FriendlyName);
    }

    [Fact]
    public async Task SearchAttachableFiles_CapsAt25_AndExcludesDeleted()
    {
        // A deleted file that would sort first must still be excluded from the picker.
        var deleted = await SeedFileAsync("aaa-match.pdf");
        deleted.DeletedOn = DateTime.Now;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        for (int i = 0; i < 30; i++)
        {
            await SeedFileAsync($"doc-{i:00}-match.pdf");
        }
        SignInAs(DelegateUser(), isManager: false, "SVMSecure");

        var results = await _service.SearchAttachableFilesAsync("match", TestContext.Current.CancellationToken);

        Assert.Equal(25, results.Count);
        Assert.DoesNotContain(results, r => r.FriendlyName == "aaa-match.pdf");
    }

    [Fact]
    public async Task SearchAttachableFiles_Anonymous_ReturnsEmpty()
    {
        await SeedFileAsync("report-match.pdf");
        _userHelper.GetCurrentUser().Returns((AaudUser?)null);

        Assert.Empty(await _service.SearchAttachableFilesAsync("match", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SearchAttachableFiles_HidesFilesTheUserCannotDownload()
    {
        // The picker is reachable by delegated editors, so it must not leak names/guids of
        // files the user could not download. Same rules as downloads: public, unrestricted
        // (any SVMSecure user), permission match (case-insensitive), or explicit person grant.
        await SeedFileAsync("open-match.pdf");
        await SeedFileAsync("public-match.pdf", customize: f =>
        {
            f.AllowPublicAccess = true;
            f.FileToPermissions.Add(new FileToPermission { Permission = "SVMSecure.SchoolAdmin" });
        });
        await SeedFileAsync("granted-match.pdf", customize: f =>
            f.FileToPermissions.Add(new FileToPermission { Permission = "svmsecure.editors" }));
        await SeedFileAsync("person-match.pdf", customize: f =>
        {
            f.FileToPermissions.Add(new FileToPermission { Permission = "SVMSecure.SchoolAdmin" });
            f.FileToPeople.Add(new FileToPerson { IamId = "iam-10" });
        });
        await SeedFileAsync("restricted-match.pdf", customize: f =>
            f.FileToPermissions.Add(new FileToPermission { Permission = "SVMSecure.SchoolAdmin" }));
        var user = DelegateUser();
        user.IamId = "iam-10";
        SignInAs(user, isManager: false, "SVMSecure", "SVMSecure.Editors");

        var results = await _service.SearchAttachableFilesAsync("match", TestContext.Current.CancellationToken);

        var names = results.Select(r => r.FriendlyName).ToList();
        Assert.Contains("open-match.pdf", names);
        Assert.Contains("public-match.pdf", names);
        Assert.Contains("granted-match.pdf", names);
        Assert.Contains("person-match.pdf", names);
        Assert.DoesNotContain("restricted-match.pdf", names);
    }

    #endregion

    #region Inline-upload rollback eligibility (IsFileRollbackDeletableAsync)

    [Fact]
    public async Task IsFileRollbackDeletable_True_WhenUploaderSameFolderNotSharedElsewhere()
    {
        var block = await SeedBlockAsync(); // ViperSectionPath = "cats"
        var file = await SeedFileAsync("cats-x.pdf", folder: "cats", modifiedBy: "delegate");
        _context.ContentBlockToFiles.Add(new ContentBlockToFile { ContentBlockId = block.ContentBlockId, FileGuid = file.FileGuid });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _userHelper.GetCurrentUser().Returns(DelegateUser());

        var result = await _service.IsFileRollbackDeletableAsync(block.ContentBlockId, file.FileGuid,
            TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task IsFileRollbackDeletable_False_WhenUploadedBySomeoneElse()
    {
        var block = await SeedBlockAsync();
        var file = await SeedFileAsync("cats-x.pdf", folder: "cats", modifiedBy: "someoneElse");
        _userHelper.GetCurrentUser().Returns(DelegateUser());

        var result = await _service.IsFileRollbackDeletableAsync(block.ContentBlockId, file.FileGuid,
            TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task IsFileRollbackDeletable_False_WhenFileInAnotherFolder()
    {
        var block = await SeedBlockAsync(); // cats
        var file = await SeedFileAsync("faculty-x.pdf", folder: "faculty", modifiedBy: "delegate");
        _userHelper.GetCurrentUser().Returns(DelegateUser());

        var result = await _service.IsFileRollbackDeletableAsync(block.ContentBlockId, file.FileGuid,
            TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task IsFileRollbackDeletable_False_WhenAttachedToAnotherBlock()
    {
        var block = await SeedBlockAsync();
        var other = await SeedBlockAsync();
        var file = await SeedFileAsync("cats-x.pdf", folder: "cats", modifiedBy: "delegate");
        _context.ContentBlockToFiles.Add(new ContentBlockToFile { ContentBlockId = other.ContentBlockId, FileGuid = file.FileGuid });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _userHelper.GetCurrentUser().Returns(DelegateUser());

        var result = await _service.IsFileRollbackDeletableAsync(block.ContentBlockId, file.FileGuid,
            TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task IsFileRollbackDeletable_Null_WhenFileMissing()
    {
        var block = await SeedBlockAsync();
        _userHelper.GetCurrentUser().Returns(DelegateUser());

        var result = await _service.IsFileRollbackDeletableAsync(block.ContentBlockId, Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    #endregion
}
