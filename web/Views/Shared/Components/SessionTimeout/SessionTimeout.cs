using Microsoft.AspNetCore.Mvc;

namespace Viper.Views.Shared.Components.SessionTimeout
{
    [ViewComponent(Name = "SessionTimeout")]
    public class SessionTimeout : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            // The layout renders this on public pages too. An anonymous visitor has no session, and
            // the poll would report zero seconds left and tell them it had expired.
            return UserClaimsPrincipal?.Identity?.IsAuthenticated == true
                ? View("Default")
                : Content(string.Empty);
        }

    }
}
