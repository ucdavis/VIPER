using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Areas.CMS.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.CMS.Controllers
{
    /// <summary>
    /// Left navigation menu management. Display of menus to end users goes through
    /// LayoutController / Data.LeftNavMenu, which filters items by user permission.
    /// </summary>
    [Route("/api/cms/left-navs")]
    [Permission(Allow = CmsPermissions.ManageNavigation)]
    public class CMSLeftNavController : ApiController
    {
        private readonly ICmsLeftNavService _leftNavService;

        public CMSLeftNavController(ICmsLeftNavService leftNavService)
        {
            _leftNavService = leftNavService;
        }

        // GET /api/cms/left-navs
        [HttpGet]
        public async Task<ActionResult<List<LeftNavMenuDto>>> GetMenus(
            string? system, string? viperSectionPath, string? search, CancellationToken ct = default)
        {
            return await _leftNavService.GetMenusAsync(system, viperSectionPath, search, ct);
        }

        // GET /api/cms/left-navs/5
        [HttpGet("{leftNavMenuId:int}")]
        public async Task<ActionResult<LeftNavMenuDto>> GetMenu(int leftNavMenuId, CancellationToken ct = default)
        {
            var menu = await _leftNavService.GetMenuAsync(leftNavMenuId, ct);
            if (menu == null)
            {
                return NotFound();
            }
            return menu;
        }

        // POST /api/cms/left-navs
        [HttpPost]
        public async Task<ActionResult<LeftNavMenuDto>> CreateMenu(LeftNavMenuAddEdit menu, CancellationToken ct = default)
        {
            var created = await _leftNavService.CreateMenuAsync(menu, ct);
            return CreatedAtAction(nameof(GetMenu), new { leftNavMenuId = created.LeftNavMenuId }, created);
        }

        // PUT /api/cms/left-navs/5
        [HttpPut("{leftNavMenuId:int}")]
        public async Task<ActionResult<LeftNavMenuDto>> UpdateMenu(int leftNavMenuId, LeftNavMenuAddEdit menu,
            CancellationToken ct = default)
        {
            var updated = await _leftNavService.UpdateMenuAsync(leftNavMenuId, menu, ct);
            if (updated == null)
            {
                return NotFound();
            }
            return updated;
        }

        // PUT /api/cms/left-navs/5/items — full item list; order follows the array
        [HttpPut("{leftNavMenuId:int}/items")]
        public async Task<ActionResult<LeftNavMenuDto>> SaveItems(int leftNavMenuId, List<LeftNavItemEdit> items,
            CancellationToken ct = default)
        {
            if (items.Where(i => i.LeftNavItemId > 0).GroupBy(i => i.LeftNavItemId).Any(g => g.Count() > 1))
            {
                return BadRequest("Duplicate item ids are not allowed.");
            }

            var updated = await _leftNavService.SaveItemsAsync(leftNavMenuId, items, ct);
            if (updated == null)
            {
                return NotFound();
            }
            return updated;
        }

        // DELETE /api/cms/left-navs/5 — deletes the menu and all items (no soft delete, matching legacy)
        [HttpDelete("{leftNavMenuId:int}")]
        public async Task<IActionResult> DeleteMenu(int leftNavMenuId, CancellationToken ct = default)
        {
            return await _leftNavService.DeleteMenuAsync(leftNavMenuId, ct) ? NoContent() : NotFound();
        }
    }
}
