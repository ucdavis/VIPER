using Microsoft.AspNetCore.Mvc;

namespace Viper.Views.Shared.Components.VueCdn
{
    [ViewComponent(Name = "VueCdnInit")]
    public class VueCdnInit : ViewComponent
    {
        public VueCdnInit()
        {
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return await Task.Run(() => View("~/Views/Shared/Components/VueCdn/VueCdnInit.cshtml"));
        }

    }
}
