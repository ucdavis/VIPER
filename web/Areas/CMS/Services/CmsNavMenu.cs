using Viper.Areas.CMS.Constants;
using Viper.Classes;
using Viper.Classes.SQLContext;

namespace Viper.Areas.CMS.Services
{
    public class CmsNavMenu
    {
        private readonly RAPSContext _rapsContext;
        public CmsNavMenu(RAPSContext context)
        {
            _rapsContext = context;
        }

        public NavMenu Nav()
        {
            UserHelper userHelper = new();
            var user = userHelper.GetCurrentUser();
            bool canManageBlocks = userHelper.HasPermission(_rapsContext, user, CmsPermissions.ManageContentBlocks);
            bool canCreateBlocks = userHelper.HasPermission(_rapsContext, user, CmsPermissions.CreateContentBlock);
            bool canManageFiles = userHelper.HasPermission(_rapsContext, user, CmsPermissions.AllFiles);
            bool canManageNav = userHelper.HasPermission(_rapsContext, user, CmsPermissions.ManageNavigation);

            var nav = new List<NavMenuItem>();

            if (canManageBlocks || canCreateBlocks || canManageFiles || canManageNav)
            {
                nav.Add(new NavMenuItem { MenuItemText = "Home", MenuItemURL = "Home" });
            }

            if (canManageBlocks || canCreateBlocks)
            {
                nav.Add(new NavMenuItem { MenuItemText = "Content Blocks", IsHeader = true });
                if (canManageBlocks)
                {
                    nav.Add(new NavMenuItem { MenuItemText = "Manage Content Blocks", MenuItemURL = "ManageContentBlocks", IndentLevel = 1 });
                }
                if (canCreateBlocks)
                {
                    nav.Add(new NavMenuItem { MenuItemText = "Add Content Block", MenuItemURL = "ManageContentBlocks/Edit", IndentLevel = 1 });
                }
                if (canManageBlocks)
                {
                    nav.Add(new NavMenuItem { MenuItemText = "Manage Link Collections", MenuItemURL = "ManageLinkCollections", IndentLevel = 1 });
                }
            }

            if (canManageFiles)
            {
                nav.Add(new NavMenuItem { MenuItemText = "Files", IsHeader = true });
                nav.Add(new NavMenuItem { MenuItemText = "Manage Files", MenuItemURL = "ManageFiles", IndentLevel = 1 });
                nav.Add(new NavMenuItem { MenuItemText = "Upload File", MenuItemURL = "ManageFiles?upload=1", IndentLevel = 1 });
            }

            if (canManageNav)
            {
                nav.Add(new NavMenuItem { MenuItemText = "Left Navigation Menus", IsHeader = true });
                nav.Add(new NavMenuItem { MenuItemText = "Manage Left-Nav Menus", MenuItemURL = "ManageLeftNav", IndentLevel = 1 });
                nav.Add(new NavMenuItem { MenuItemText = "Add Left-Nav Menu", MenuItemURL = "ManageLeftNav/Edit", IndentLevel = 1 });
            }

            return new NavMenu("Content Management System", nav);
        }
    }
}
