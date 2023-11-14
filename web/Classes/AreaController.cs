using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Viper.Classes
{
    public class AreaController : Controller
    {
        public AreaController() { }

        protected void ConvertNavLinksForDevelopment(NavMenu menu)
        {
            if (HttpHelper.Environment?.EnvironmentName == "Development" && menu?.MenuItems != null)
            {
                foreach (var item in (menu.MenuItems).Where(item => item.MenuItemURL.Length > 0 && item.MenuItemURL.Substring(0, 1) == "/"))
                {
                    item.MenuItemURL = "http://localhost" + item.MenuItemURL;
                }
            }
        }
        //TODO: Handle 403 and 500 errors here? 
    }
}
