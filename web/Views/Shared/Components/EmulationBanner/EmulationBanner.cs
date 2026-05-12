using Microsoft.AspNetCore.Mvc;

namespace Viper.Views.Shared.Components.EmulationBanner
{
    [ViewComponent(Name = "EmulationBanner")]
    public class EmulationBannerViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            IUserHelper UserHelper = new UserHelper();
            if (!UserHelper.IsEmulating())
            {
                return await Task.Run(() => (IViewComponentResult)Content(string.Empty));
            }

            string? displayFullName = UserHelper.GetCurrentUser()?.DisplayFullName;
            return await Task.Run(() => View("Default", displayFullName));
        }
    }
}
