using Microsoft.AspNetCore.Mvc;

namespace Viper.Views.Shared.Components.EmulationBanner
{
    [ViewComponent(Name = "EmulationBanner")]
    public class EmulationBannerViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            IUserHelper UserHelper = new UserHelper();
            if (!UserHelper.IsEmulating())
            {
                return Content(string.Empty);
            }

            string? displayFullName = UserHelper.GetCurrentUser()?.DisplayFullName;
            return View("Default", displayFullName);
        }
    }
}
