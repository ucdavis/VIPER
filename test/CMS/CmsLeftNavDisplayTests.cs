using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.VIPER;
using DataLeftNav = Viper.Areas.CMS.Data.LeftNavMenu;

namespace Viper.test.CMS;

/// <summary>
/// Tests for Data.LeftNavMenu.GetLeftNavMenus display-time permission filtering (legacy CMS
/// left-nav parity): an item with no permissions is visible to any logged-in user, an item is
/// shown when the user holds any one of its permissions and hidden otherwise, and filtering is
/// bypassed for management callers. Permission scenarios are driven by mocking IUserHelper.
/// </summary>
public sealed class CmsLeftNavDisplayTests : IDisposable
{
    private readonly VIPERContext _context;
    private readonly RAPSContext _rapsContext;
    private readonly IUserHelper _userHelper;

    public CmsLeftNavDisplayTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _rapsContext = new RAPSContext(new DbContextOptionsBuilder<RAPSContext>()
            .UseInMemoryDatabase("RAPS_" + Guid.NewGuid()).Options);
        _userHelper = Substitute.For<IUserHelper>();
    }

    public void Dispose()
    {
        _context.Dispose();
        _rapsContext.Dispose();
    }

    private async Task<string> SeedMixedMenuAsync()
    {
        var friendlyName = "display-menu-" + Guid.NewGuid().ToString("N")[..8];
        var menu = new LeftNavMenu
        {
            MenuHeaderText = "Display Menu",
            System = "Viper",
            FriendlyName = friendlyName,
            ModifiedOn = DateTime.Now,
            ModifiedBy = "test"
        };
        menu.LeftNavItems.Add(Item("Public", 1));                          // no permissions
        menu.LeftNavItems.Add(Item("Allowed", 2, "SVMSecure.Allowed"));    // user holds this
        menu.LeftNavItems.Add(Item("Forbidden", 3, "SVMSecure.Forbidden")); // user lacks this
        _context.LeftNavMenus.Add(menu);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return friendlyName;
    }

    private static LeftNavItem Item(string text, int order, params string[] permissions)
    {
        var item = new LeftNavItem { MenuItemText = text, IsHeader = false, Url = "/" + text, DisplayOrder = order };
        foreach (var p in permissions)
        {
            item.LeftNavItemToPermissions.Add(new LeftNavItemToPermission { Permission = p });
        }
        return item;
    }

    [Fact]
    public async Task GetLeftNavMenus_HidesItemsTheUserLacksPermissionFor()
    {
        var friendlyName = await SeedMixedMenuAsync();
        var user = new AaudUser { AaudUserId = 1, LoginId = "viewer" };
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), "SVMSecure.Allowed").Returns(true);
        // HasPermission for "SVMSecure.Forbidden" defaults to false.

        var menus = new DataLeftNav(_context, _rapsContext, _userHelper)
            .GetLeftNavMenus(friendlyName: friendlyName);

        var items = Assert.Single(menus!).MenuItems!;
        // No-permission item and the held-permission item are visible; the unheld one is hidden.
        // (Assert membership, not sequence — the in-memory provider does not honor the include OrderBy.)
        Assert.Equal(new[] { "Allowed", "Public" }, items.Select(i => i.MenuItemText).OrderBy(t => t));
    }

    [Fact]
    public async Task GetLeftNavMenus_AnonymousUser_HidesPermissionLessItems()
    {
        var friendlyName = await SeedMixedMenuAsync();
        // An anonymous request has no signed-in user; GetCurrentUser returns null.
        _userHelper.GetCurrentUser().Returns((AaudUser?)null);

        var menus = new DataLeftNav(_context, _rapsContext, _userHelper)
            .GetLeftNavMenus(friendlyName: friendlyName);

        var items = Assert.Single(menus!).MenuItems!;
        // Permission-less items are visible only to logged-in users, so an anonymous request sees
        // none of them; holding no permissions, it sees the permissioned items too - nothing shows.
        Assert.DoesNotContain("Public", items.Select(i => i.MenuItemText));
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetLeftNavMenus_ReturnsAllItems_WhenFilteringDisabled()
    {
        var friendlyName = await SeedMixedMenuAsync();
        _userHelper.GetCurrentUser().Returns(new AaudUser { AaudUserId = 1, LoginId = "viewer" });
        // HasPermission would return false for every permission, but management callers bypass filtering.

        var menus = new DataLeftNav(_context, _rapsContext, _userHelper)
            .GetLeftNavMenus(friendlyName: friendlyName, filterItemsByPermissions: false);

        var items = Assert.Single(menus!).MenuItems!;
        Assert.Equal(new[] { "Allowed", "Forbidden", "Public" }, items.Select(i => i.MenuItemText).OrderBy(t => t));
    }
}
