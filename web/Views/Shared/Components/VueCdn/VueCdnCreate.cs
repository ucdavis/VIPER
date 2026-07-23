using Microsoft.AspNetCore.Mvc;

namespace Viper.Views.Shared.Components.VueCdn
{
    [ViewComponent(Name = "VueCdnCreate")]
    public class VueCdnCreate : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Views/Shared/Components/VueCdn/VueCdnCreate.cshtml");
        }
    }
}
