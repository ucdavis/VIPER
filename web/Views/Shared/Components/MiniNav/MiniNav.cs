using Microsoft.AspNetCore.Mvc;
using Viper.Models.AAUD;

namespace Viper.Views.Shared.Components.MiniNav
{
    [ViewComponent(Name = "MiniNav")]
    public class MiniNavViewComponent : ViewComponent
    {
        private readonly string oldViperURL = HttpHelper.GetOldViperRootURL();

        public IViewComponentResult Invoke(AaudUser user)
        {
            ViewData["OldViperURL"] = oldViperURL;
            return View("Default", user);
        }

    }
}
