using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Classes.SQLContext;

namespace Viper.Areas.CMS.Data
{
    public class LeftNavMenu
    {
        private readonly VIPERContext? _viperContext;
        private readonly RAPSContext? _rapsContext;
        private readonly IUserHelper _userHelper;

        public LeftNavMenu(VIPERContext viperContext, RAPSContext rapsContext, IUserHelper? userHelper = null)
        {
            this._viperContext = viperContext;
            this._rapsContext = rapsContext;
            _userHelper = userHelper ?? new UserHelper();
        }

        /// <summary>
        /// Get one or more left nav
        /// </summary>
        /// <param name="leftNavMenuId">The primary key of the menu</param>
        /// <param name="friendlyName">Friendly name of the menu</param>
        /// <param name="system">System</param>
        /// <param name="viperSectionPath">ViperSectionPath</param>
        /// <param name="page">Page</param>
        /// <param name="filterItemsByPermissions">If true, filter items based on the permission of the logged in user. Should be set to false for CMS management functions.</param>
        /// <returns>List of menus matching the arguments</returns>
        public IEnumerable<NavMenu>? GetLeftNavMenus(int? leftNavMenuId = null, string? friendlyName = null, string? system = null,
                string? viperSectionPath = null, string? page = null, bool filterItemsByPermissions = true)
        {
            var menus = _viperContext?.LeftNavMenus
                .Include(m => m.LeftNavItems
                    .OrderBy(i => i.DisplayOrder))
                .ThenInclude(i => i.LeftNavItemToPermissions)
                .Where(m => leftNavMenuId == null || m.LeftNavMenuId == leftNavMenuId)
                .Where(m => string.IsNullOrEmpty(friendlyName) || m.FriendlyName == friendlyName)
                .Where(m => string.IsNullOrEmpty(system) || m.System == system)
                .Where(m => string.IsNullOrEmpty(viperSectionPath) || m.ViperSectionPath == viperSectionPath)
                .Where(m => string.IsNullOrEmpty(page) || m.Page == page)
                .ToList();
            if (menus == null)
            {
                return null;
            }

            var currentUser = _userHelper.GetCurrentUser();
            List<NavMenu> cmsMenus = new();
            foreach (var m in menus)
            {
                //by default, filter items based on user permissions; items with no
                //permissions are visible to any logged-in user (legacy CMS behavior)
                List<NavMenuItem> items = m.LeftNavItems
                    .Where(item => !filterItemsByPermissions
                        || item.LeftNavItemToPermissions.Count == 0
                        || item.LeftNavItemToPermissions.Any(p => _userHelper.HasPermission(_rapsContext, currentUser, p.Permission)))
                    .Select(item => new NavMenuItem(item))
                    .ToList();
                cmsMenus.Add(new(m.MenuHeaderText, items));
            }
            return cmsMenus;
        }
    }
}
