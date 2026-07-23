using Microsoft.AspNetCore.Mvc;

namespace Viper.Views.Shared.Components.VueCdn
{
    [ViewComponent(Name = "VueCdnInit")]
    public class VueCdnInit : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Views/Shared/Components/VueCdn/VueCdnInit.cshtml");
        }

    }
}
