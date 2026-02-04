using Microsoft.AspNetCore.Mvc;

namespace Viper.Views.Shared.Components.SessionTimeout
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
            ViewData["sessionRefreshUrl"] = (onDev ? "http://localhost/" : ("https://" + HttpHelper.HttpContext?.Request.Host.Value + "/"))
                + "/public/timeout/seconds_until_timeout_v2.cfm?id="
                + (loginId ?? "")
                + "&service=" + (onDev ? "Viper2-dev" : "Viper2");
            return await Task.Run(() => View("Default"));
        }

    }
}
