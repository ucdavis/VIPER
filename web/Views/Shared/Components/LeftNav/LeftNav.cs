using Microsoft.AspNetCore.Mvc;
using Viper.Models.AAUD;

namespace Viper.Views.Shared.Components.LeftNav
{
    [ViewComponent(Name = "LeftNav")]
    public class LeftNavViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(AaudUser user, int nav)
        {



            return View("Default", user);
        }

    }
}
