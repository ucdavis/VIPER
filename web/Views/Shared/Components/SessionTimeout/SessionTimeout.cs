using Microsoft.AspNetCore.Mvc;
using Viper.Models.AAUD;

namespace Viper.Views.Shared.Components.ProfilePic
{
    [ViewComponent(Name = "SessionTimeout")]
    public class SessionTimeout : ViewComponent
    {
        public SessionTimeout()
        {

        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            UserHelper userHelper = new UserHelper();
            string? loginId = userHelper?.GetCurrentUser()?.LoginId;
            bool onDev = HttpHelper.Environment?.EnvironmentName == "Development";
            ViewData["sessionRefreshUrl"] = (onDev ? "http://localhost/" : "/")
                + "/public/timeout/seconds_until_timeout_v2.cfm?id="
                + (loginId ?? "")
                + "&service=" + (onDev ? "Viper2-dev" : "Viper2");
            return await Task.Run(() => View("Default"));
        }

    }
}
