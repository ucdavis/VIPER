using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Viper.Areas.CMS.Controllers;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Areas.CMS.Services;

namespace Viper.test.CMS;

/// <summary>
/// Controller wiring tests for CMSLeftNavController: menu CRUD status mapping, the
/// duplicate-item-id guard on the batch item save, and cascade delete. The batch save/reorder
/// semantics themselves are covered in the CmsLeftNavService tests.
/// </summary>
public sealed class CMSLeftNavControllerTests
{
    private readonly ICmsLeftNavService _leftNavService;
    private readonly CMSLeftNavController _controller;

    public CMSLeftNavControllerTests()
    {
        _leftNavService = Substitute.For<ICmsLeftNavService>();
        _controller = new CMSLeftNavController(_leftNavService);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
        };
    }

    private static LeftNavMenuDto Menu(int id = 1) => new() { LeftNavMenuId = id, MenuHeaderText = "Cats", System = "Viper" };

    #region Get

    [Fact]
    public async Task GetMenus_PassesFiltersThrough()
    {
        var menus = new List<LeftNavMenuDto> { Menu() };
        _leftNavService.GetMenusAsync("Viper", "cats", "search", Arg.Any<CancellationToken>()).Returns(menus);

        var result = await _controller.GetMenus("Viper", "cats", "search", TestContext.Current.CancellationToken);

        Assert.Same(menus, result.Value);
        await _leftNavService.Received(1).GetMenusAsync("Viper", "cats", "search", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMenu_ReturnsMenu_WhenFound()
    {
        _leftNavService.GetMenuAsync(5, Arg.Any<CancellationToken>()).Returns(Menu(5));

        var result = await _controller.GetMenu(5, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Value);
        Assert.Equal(5, result.Value!.LeftNavMenuId);
    }

    [Fact]
    public async Task GetMenu_ReturnsNotFound_WhenMissing()
    {
        _leftNavService.GetMenuAsync(999, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _controller.GetMenu(999, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region Create / Update

    [Fact]
    public async Task CreateMenu_ReturnsCreatedAtAction()
    {
        var request = new LeftNavMenuAddEdit { MenuHeaderText = "Cats", System = "Viper" };
        _leftNavService.CreateMenuAsync(request, Arg.Any<CancellationToken>()).Returns(Menu(8));

        var result = await _controller.CreateMenu(request, TestContext.Current.CancellationToken);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(CMSLeftNavController.GetMenu), created.ActionName);
        var dto = Assert.IsType<LeftNavMenuDto>(created.Value);
        Assert.Equal(8, dto.LeftNavMenuId);
    }

    [Fact]
    public async Task UpdateMenu_ReturnsMenu_OnSuccess()
    {
        var request = new LeftNavMenuAddEdit { MenuHeaderText = "Cats", System = "Viper" };
        _leftNavService.UpdateMenuAsync(5, request, Arg.Any<CancellationToken>()).Returns(Menu(5));

        var result = await _controller.UpdateMenu(5, request, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Value);
        Assert.Equal(5, result.Value!.LeftNavMenuId);
    }

    [Fact]
    public async Task UpdateMenu_ReturnsNotFound_WhenMissing()
    {
        var request = new LeftNavMenuAddEdit { MenuHeaderText = "Cats", System = "Viper" };
        _leftNavService.UpdateMenuAsync(5, request, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _controller.UpdateMenu(5, request, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region SaveItems

    private static LeftNavItemsSave ItemsSave(params LeftNavItemEdit[] items) =>
        new() { LastModifiedOn = DateTime.Now, Items = items.ToList() };

    [Fact]
    public async Task SaveItems_ReturnsBadRequest_OnArgumentException()
    {
        // The duplicate-id guard now lives in the service; the controller maps its ArgumentException to 400.
        var request = ItemsSave(new LeftNavItemEdit { LeftNavItemId = 2, MenuItemText = "A" });
        _leftNavService.SaveItemsAsync(5, request, Arg.Any<CancellationToken>())
            .Throws(new ArgumentException("Duplicate item ids are not allowed."));

        var result = await _controller.SaveItems(5, request, TestContext.Current.CancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Duplicate item ids", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task SaveItems_ReturnsConflict_OnStaleItemId()
    {
        var request = ItemsSave(new LeftNavItemEdit { LeftNavItemId = 99, MenuItemText = "Stale" });
        _leftNavService.SaveItemsAsync(5, request, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("One or more items no longer exist in this menu. Reload and try again."));

        var result = await _controller.SaveItems(5, request, TestContext.Current.CancellationToken);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("no longer exist", conflict.Value?.ToString());
    }

    [Fact]
    public async Task SaveItems_ReturnsConflict_OnStaleStamp()
    {
        // CmsConcurrencyException derives from InvalidOperationException and must map to 409.
        var request = ItemsSave(new LeftNavItemEdit { LeftNavItemId = 0, MenuItemText = "A" });
        _leftNavService.SaveItemsAsync(5, request, Arg.Any<CancellationToken>())
            .Throws(new CmsConcurrencyException("This menu was modified by someone on 1/1/2026. Reload to get the latest version."));

        var result = await _controller.SaveItems(5, request, TestContext.Current.CancellationToken);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("Reload", conflict.Value?.ToString());
    }

    [Fact]
    public async Task SaveItems_AllowsMultipleNewItems_WithZeroIds()
    {
        var request = ItemsSave(
            new LeftNavItemEdit { LeftNavItemId = 0, MenuItemText = "New 1" },
            new LeftNavItemEdit { LeftNavItemId = 0, MenuItemText = "New 2" });
        _leftNavService.SaveItemsAsync(5, request, Arg.Any<CancellationToken>()).Returns(Menu(5));

        var result = await _controller.SaveItems(5, request, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Value);
        await _leftNavService.Received(1).SaveItemsAsync(5, request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveItems_ReturnsNotFound_WhenMenuMissing()
    {
        var request = ItemsSave(new LeftNavItemEdit { LeftNavItemId = 0, MenuItemText = "A" });
        _leftNavService.SaveItemsAsync(5, request, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _controller.SaveItems(5, request, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task DeleteMenu_ReturnsNoContent_WhenDeleted()
    {
        _leftNavService.DeleteMenuAsync(5, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _controller.DeleteMenu(5, TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteMenu_ReturnsNotFound_WhenMissing()
    {
        _leftNavService.DeleteMenuAsync(5, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _controller.DeleteMenu(5, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    #endregion
}
