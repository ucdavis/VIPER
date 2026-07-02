using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Viper.Areas.CMS.Models;
using Viper.Areas.CMS.Services;
using Viper.Classes.SQLContext;
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
    private readonly IHtmlSanitizerService _sanitizer;
    private readonly CmsContentBlockService _service;

    public CmsContentBlockServiceTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _sanitizer = Substitute.For<IHtmlSanitizerService>();
        _sanitizer.Sanitize(Arg.Any<string>()).Returns(callInfo => callInfo.ArgAt<string>(0));
        // Pass-through so diff tests assert on the real htmldiff.net markers, not a sanitized copy.
        _sanitizer.SanitizeDiff(Arg.Any<string>()).Returns(callInfo => callInfo.ArgAt<string>(0));

        _service = new CmsContentBlockService(_context, _sanitizer, Substitute.For<IUserHelper>());
    }

    public void Dispose()
    {
        _context.Dispose();
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

        var active = await _service.GetContentBlocksAsync("active", null, null, null, ct: TestContext.Current.CancellationToken);
        var deleted = await _service.GetContentBlocksAsync("deleted", null, null, null, ct: TestContext.Current.CancellationToken);
        var all = await _service.GetContentBlocksAsync("all", null, null, null, ct: TestContext.Current.CancellationToken);

        Assert.Single(active);
        Assert.Single(deleted);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetContentBlocks_ListOmitsContent()
    {
        await SeedBlockAsync();

        var list = await _service.GetContentBlocksAsync("active", null, null, null, ct: TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, list[0].Content);
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
            TestContext.Current.CancellationToken);

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
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v2</p>", block.ModifiedOn, TestContext.Current.CancellationToken);
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v3</p>", block.ModifiedOn, TestContext.Current.CancellationToken);

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
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v2</p>", block.ModifiedOn, TestContext.Current.CancellationToken);
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v3</p>", block.ModifiedOn, TestContext.Current.CancellationToken);

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
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v2</p>", block.ModifiedOn, TestContext.Current.CancellationToken);

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
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v2</p>", block.ModifiedOn, TestContext.Current.CancellationToken);

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
        await _service.UpdateContentOnlyAsync(block.ContentBlockId, "<p>v2</p>", block.ModifiedOn, TestContext.Current.CancellationToken);

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
}
