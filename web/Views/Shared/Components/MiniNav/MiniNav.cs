using Microsoft.AspNetCore.Mvc;
using Viper.Models.AAUD;

namespace Viper.Views.Shared.Components.MiniNav
{
    [ViewComponent(Name = "MiniNav")]
    public class MiniNavViewComponent : ViewComponent
    {
        private readonly string oldViperURL = HttpHelper.GetOldViperRootURL();

        public MiniNavViewComponent()
        {
        }

        public async Task<IViewComponentResult> InvokeAsync(AaudUser user)
        {
            ViewData["OldViperURL"] = oldViperURL;
            return await Task.Run(() => View("Default", user));
        }

    }
}
