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
            UserHelper userHelper = new UserHelper();

            var nav = new List<NavMenuItem>
            {
            };

            if (userHelper.HasPermission(_rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CMS.ManageContentBlocks"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Manage Link Collections", MenuItemURL = "ManageLinkCollections" });
            }
            
            return new NavMenu("Content Management System", nav);
        }
    }
}
