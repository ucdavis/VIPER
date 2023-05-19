using Microsoft.AspNetCore.Mvc;
using Viper.Models.AAUD;

namespace Viper.Views.Shared.Components.MainNav
{
    [ViewComponent(Name = "MainNav")]
    public class MainNavViewComponent : ViewComponent
    {
        private readonly string oldViperURL = HttpHelper.GetOldViperRootURL();

        public MainNavViewComponent()
        {
        }

        public async Task<IViewComponentResult> InvokeAsync(AaudUser user)
        {
            ViewData["OldViperURL"] = oldViperURL;

            return await Task.Run(() => View("Default", user));
        }

    }
}
