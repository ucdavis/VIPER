using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Areas.CMS.Services;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;

namespace Viper.test.CMS;

/// <summary>
/// Tests for CmsLeftNavService: menu CRUD with cascade delete and the batch item save
/// (add/update/delete/reorder in one call, replacing the legacy DataTables editor).
/// </summary>
public sealed class CmsLeftNavServiceTests : IDisposable
{
    private readonly VIPERContext _context;
    private readonly CmsLeftNavService _service;

    public CmsLeftNavServiceTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _service = new CmsLeftNavService(_context, Substitute.For<IUserHelper>());
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private async Task<LeftNavMenu> SeedMenuAsync(Action<LeftNavMenu>? customize = null)
    {
        var menu = new LeftNavMenu
        {
            MenuHeaderText = "Seeded Menu",
            System = "Viper",
            ViperSectionPath = "cats",
            FriendlyName = "seeded-menu-" + Guid.NewGuid().ToString("N")[..8],
            ModifiedOn = DateTime.Now.AddDays(-1),
            ModifiedBy = "test"
        };
        customize?.Invoke(menu);
        _context.LeftNavMenus.Add(menu);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return menu;
    }

    private static LeftNavItem MakeItem(string text, int order, bool isHeader = false, string? url = null, params string[] permissions)
    {
        var item = new LeftNavItem
        {
            MenuItemText = text,
            IsHeader = isHeader,
            Url = url,
            DisplayOrder = order
        };
        foreach (var p in permissions)
        {
            item.LeftNavItemToPermissions.Add(new LeftNavItemToPermission { Permission = p });
        }
        return item;
    }

    [Fact]
    public async Task GetMenus_FiltersBySystemAndSearch()
    {
        await SeedMenuAsync();
        await SeedMenuAsync(m =>
        {
            m.MenuHeaderText = "Public Menu";
            m.System = "Public";
        });

        var viperMenus = await _service.GetMenusAsync("Viper", null, null, TestContext.Current.CancellationToken);
        var searched = await _service.GetMenusAsync(null, null, "Public", TestContext.Current.CancellationToken);

        Assert.Single(viperMenus);
        Assert.Single(searched);
        Assert.Equal("Public Menu", searched[0].MenuHeaderText);
    }

    [Fact]
    public async Task GetMenu_ReturnsItemsInDisplayOrder()
    {
        var menu = await SeedMenuAsync(m =>
        {
            m.LeftNavItems.Add(MakeItem("Second", 2, url: "/two"));
            m.LeftNavItems.Add(MakeItem("First", 1, isHeader: true));
        });

        var dto = await _service.GetMenuAsync(menu.LeftNavMenuId, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "First", "Second" }, dto!.Items.Select(i => i.MenuItemText).ToArray());
        Assert.True(dto.Items[0].IsHeader);
    }

    [Fact]
    public async Task CreateAndUpdateMenu_PersistFields()
    {
        var created = await _service.CreateMenuAsync(new LeftNavMenuAddEdit
        {
            MenuHeaderText = "New Menu",
            System = "Viper",
            ViperSectionPath = "students",
            Page = "home",
            FriendlyName = "new-menu"
        }, TestContext.Current.CancellationToken);

        Assert.True(created.LeftNavMenuId > 0);

        var updated = await _service.UpdateMenuAsync(created.LeftNavMenuId, new LeftNavMenuAddEdit
        {
            MenuHeaderText = "Renamed Menu",
            System = "Viper",
            LastModifiedOn = created.ModifiedOn
        }, TestContext.Current.CancellationToken);

        Assert.Equal("Renamed Menu", updated!.MenuHeaderText);
        Assert.Null(updated.ViperSectionPath);
    }

    [Fact]
    public async Task CreateMenu_DuplicateFriendlyName_ThrowsCaseInsensitive()
    {
        await SeedMenuAsync(m => m.FriendlyName = "cats-home");

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateMenuAsync(new LeftNavMenuAddEdit
        {
            MenuHeaderText = "Dup",
            System = "Viper",
            FriendlyName = "CATS-HOME"
        }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateMenu_DuplicateFriendlyName_Rejected_ButKeepingOwnIsAllowed()
    {
        var alpha = await SeedMenuAsync(m => m.FriendlyName = "alpha");
        var beta = await SeedMenuAsync(m => m.FriendlyName = "beta");

        // Renaming beta onto alpha's friendly name (any case) is rejected.
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateMenuAsync(beta.LeftNavMenuId,
            new LeftNavMenuAddEdit { MenuHeaderText = "B", System = "Viper", FriendlyName = "ALPHA", LastModifiedOn = beta.ModifiedOn },
            TestContext.Current.CancellationToken));

        // Keeping its own friendly name (self-match excluded) is allowed.
        var updated = await _service.UpdateMenuAsync(alpha.LeftNavMenuId,
            new LeftNavMenuAddEdit { MenuHeaderText = "A renamed", System = "Viper", FriendlyName = "ALPHA", LastModifiedOn = alpha.ModifiedOn },
            TestContext.Current.CancellationToken);

        Assert.NotNull(updated);
    }

    [Fact]
    public async Task UpdateMenu_StaleLastModifiedOn_ThrowsConcurrency()
    {
        var menu = await SeedMenuAsync();

        var ex = await Assert.ThrowsAsync<CmsConcurrencyException>(() => _service.UpdateMenuAsync(
            menu.LeftNavMenuId,
            new LeftNavMenuAddEdit
            {
                MenuHeaderText = "Renamed",
                System = "Viper",
                LastModifiedOn = menu.ModifiedOn.AddMinutes(-5)
            },
            TestContext.Current.CancellationToken));

        // Names who saved and when so the user knows whose edit they'd clobber.
        Assert.Contains("modified by test", ex.Message);
    }

    [Fact]
    public async Task UpdateMenu_MissingLastModifiedOn_ThrowsArgumentException()
    {
        var menu = await SeedMenuAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateMenuAsync(
            menu.LeftNavMenuId,
            new LeftNavMenuAddEdit { MenuHeaderText = "Renamed", System = "Viper" },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteMenu_CascadesToItemsAndPermissions()
    {
        var menu = await SeedMenuAsync(m => m.LeftNavItems.Add(MakeItem("Link", 1, url: "/x", permissions: "SVMSecure.CATS")));

        var result = await _service.DeleteMenuAsync(menu.LeftNavMenuId, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Empty(await _context.LeftNavMenus.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await _context.LeftNavItems.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await _context.LeftNavItemToPermissions.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveItems_AddsUpdatesDeletesAndReorders()
    {
        var menu = await SeedMenuAsync(m =>
        {
            m.LeftNavItems.Add(MakeItem("Keep Me", 1, url: "/keep", permissions: "SVMSecure.Old"));
            m.LeftNavItems.Add(MakeItem("Delete Me", 2, url: "/delete"));
        });
        var keepId = menu.LeftNavItems.First(i => i.MenuItemText == "Keep Me").LeftNavItemId;

        var dto = await _service.SaveItemsAsync(menu.LeftNavMenuId, ItemsSave(menu,
            new LeftNavItemEdit { LeftNavItemId = 0, MenuItemText = "New Header", IsHeader = true },
            new LeftNavItemEdit { LeftNavItemId = keepId, MenuItemText = "Keep Me Renamed", Url = "/kept", Permissions = new List<string> { "SVMSecure.New" } }),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, dto!.Items.Count);
        Assert.Equal("New Header", dto.Items[0].MenuItemText);
        Assert.Equal(1, dto.Items[0].DisplayOrder);
        Assert.True(dto.Items[0].IsHeader);
        Assert.Equal("Keep Me Renamed", dto.Items[1].MenuItemText);
        Assert.Equal(2, dto.Items[1].DisplayOrder);
        Assert.Equal(new List<string> { "SVMSecure.New" }, dto.Items[1].Permissions);
        // Deleted item's permissions are gone too
        Assert.Equal(1, await _context.LeftNavItemToPermissions.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveItems_HeaderItems_DropUrl()
    {
        var menu = await SeedMenuAsync();

        var dto = await _service.SaveItemsAsync(menu.LeftNavMenuId, ItemsSave(menu,
            new LeftNavItemEdit { LeftNavItemId = 0, MenuItemText = "Header", IsHeader = true, Url = "/should-be-dropped" }),
            TestContext.Current.CancellationToken);

        Assert.Null(dto!.Items[0].Url);
    }

    [Fact]
    public async Task SaveItems_EmptyHeaderText_IsAllowed()
    {
        // Legacy parity: menus use empty-text headers as spacer rows, and the display side
        // renders them as blank dividers. A menu containing one must stay saveable.
        var menu = await SeedMenuAsync();

        var dto = await _service.SaveItemsAsync(menu.LeftNavMenuId, ItemsSave(menu,
            new LeftNavItemEdit { LeftNavItemId = 0, MenuItemText = "", IsHeader = true },
            new LeftNavItemEdit { LeftNavItemId = 0, MenuItemText = "A Link", IsHeader = false, Url = "/link" }),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, dto!.Items.Count);
        Assert.Equal("", dto.Items[0].MenuItemText);
        Assert.True(dto.Items[0].IsHeader);
    }

    [Fact]
    public async Task SaveItems_EmptyLinkText_Throws()
    {
        var menu = await SeedMenuAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SaveItemsAsync(menu.LeftNavMenuId, ItemsSave(menu,
                new LeftNavItemEdit { LeftNavItemId = 0, MenuItemText = "  ", IsHeader = false, Url = "/link" }),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveItems_EmptyLinkUrl_Throws()
    {
        var menu = await SeedMenuAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SaveItemsAsync(menu.LeftNavMenuId, ItemsSave(menu,
                new LeftNavItemEdit { LeftNavItemId = 0, MenuItemText = "Link", IsHeader = false, Url = "  " }),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveItems_UnknownMenu_ReturnsNull()
    {
        var dto = await _service.SaveItemsAsync(9999,
            new LeftNavItemsSave { Items = new List<LeftNavItemEdit>() }, TestContext.Current.CancellationToken);

        Assert.Null(dto);
    }

    [Fact]
    public async Task SaveItems_UnknownItemId_Throws_AndDoesNotResurrect()
    {
        var menu = await SeedMenuAsync(m => m.LeftNavItems.Add(MakeItem("Keep Me", 1, url: "/keep")));

        // A stale client posts an id that no longer exists in the menu.
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveItemsAsync(menu.LeftNavMenuId,
            ItemsSave(menu, new LeftNavItemEdit { LeftNavItemId = 987654, MenuItemText = "Deleted Elsewhere", Url = "/gone" }),
            TestContext.Current.CancellationToken));

        // Nothing was created; the menu still holds only its original item.
        Assert.Equal(1, await _context.LeftNavItems.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveItems_DuplicateItemIds_ThrowsArgumentException()
    {
        var menu = await SeedMenuAsync(m => m.LeftNavItems.Add(MakeItem("Keep Me", 1, url: "/keep")));
        var keepId = menu.LeftNavItems.First().LeftNavItemId;

        await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveItemsAsync(menu.LeftNavMenuId,
            ItemsSave(menu,
                new LeftNavItemEdit { LeftNavItemId = keepId, MenuItemText = "A" },
                new LeftNavItemEdit { LeftNavItemId = keepId, MenuItemText = "B" }),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveItems_StaleLastModifiedOn_ThrowsConcurrency()
    {
        // Item saves ride on the menu's ModifiedOn; a stale stamp means another editor saved
        // (settings or items) after this client loaded, and the batch must not clobber them.
        var menu = await SeedMenuAsync();

        var request = new LeftNavItemsSave
        {
            LastModifiedOn = menu.ModifiedOn.AddMinutes(-5),
            Items = new List<LeftNavItemEdit> { new() { LeftNavItemId = 0, MenuItemText = "Header", IsHeader = true } }
        };

        var ex = await Assert.ThrowsAsync<CmsConcurrencyException>(() =>
            _service.SaveItemsAsync(menu.LeftNavMenuId, request, TestContext.Current.CancellationToken));
        Assert.Contains("modified by test", ex.Message);
        Assert.Equal(0, await _context.LeftNavItems.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveItems_NullItems_ThrowsArgumentException()
    {
        // "items": null binds past JsonRequired; it must be a 400, not a NullReferenceException,
        // and must not be read as "delete everything".
        var menu = await SeedMenuAsync(m => m.LeftNavItems.Add(MakeItem("Keep Me", 1, url: "/keep")));

        await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveItemsAsync(menu.LeftNavMenuId,
            new LeftNavItemsSave { LastModifiedOn = menu.ModifiedOn, Items = null },
            TestContext.Current.CancellationToken));
        Assert.Equal(1, await _context.LeftNavItems.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveItems_MissingLastModifiedOn_ThrowsArgumentException()
    {
        var menu = await SeedMenuAsync();

        var request = new LeftNavItemsSave
        {
            Items = new List<LeftNavItemEdit> { new() { LeftNavItemId = 0, MenuItemText = "Header", IsHeader = true } }
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SaveItemsAsync(menu.LeftNavMenuId, request, TestContext.Current.CancellationToken));
    }

    private static LeftNavItemsSave ItemsSave(LeftNavMenu menu, params LeftNavItemEdit[] items) =>
        new() { LastModifiedOn = menu.ModifiedOn, Items = items.ToList() };
}
