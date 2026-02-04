using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Classes.SQLContext;

namespace Viper.Areas.CMS.Data
{
    public class LeftNavMenu
    {
        private readonly VIPERContext? _viperContext;
        private readonly RAPSContext? _rapsContext;

        public IUserHelper UserHelper { get; private set; }

        public LeftNavMenu()
        {
            this._viperContext = (VIPERContext?)HttpHelper.HttpContext?.RequestServices.GetService(typeof(VIPERContext));
            this._rapsContext = (RAPSContext?)HttpHelper.HttpContext?.RequestServices.GetService(typeof(RAPSContext));
            UserHelper = new UserHelper();
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

            var currentUser = UserHelper.GetCurrentUser();
            List<NavMenu> cmsMenus = new();
            foreach (var m in menus)
            {
                //by default, filter items based on user permissions
                List<NavMenuItem> items = new();
                foreach (var item in m.LeftNavItems)
                {
                    bool includeItem = !filterItemsByPermissions;
                    if (filterItemsByPermissions)
                    {
                        foreach (var p in item.LeftNavItemToPermissions)
                        {
                            if (UserHelper.HasPermission(_rapsContext, currentUser, p.Permission))
                            {
                                includeItem = true;
                                break;
                            }
                        }
                    }
                    if (includeItem)
                    {
                        items.Add(new(item));
                    }
                }
                cmsMenus.Add(new(m.MenuHeaderText, items));
            }
            return cmsMenus;
        }
    }
}
