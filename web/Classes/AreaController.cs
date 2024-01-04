using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using Viper.Classes.Utilities;

namespace Viper.Classes
{
    public class AreaController : Controller
    {
        public AreaController() { }

        /// <summary>
        /// For Area Controller actions, update the session timeout in the database
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public override async Task OnActionExecutionAsync(ActionExecutingContext context,
                                         ActionExecutionDelegate next)
        {
            SessionTimeoutService.UpdateSessionTimeout();
            await next();
        }

        protected void ConvertNavLinksForDevelopment(NavMenu menu)
        {
            if (HttpHelper.Environment?.EnvironmentName == "Development" && menu?.MenuItems != null)
            {
                foreach (var item in menu.MenuItems.Where(item => item.MenuItemURL.Length > 0 && item.MenuItemURL.Substring(0, 1) == "/"))
                {
                    item.MenuItemURL = "http://localhost" + item.MenuItemURL;
                }
            }
        }
        //TODO: Handle 403 and 500 errors here? 
    }
}
