using Microsoft.AspNetCore.Mvc;

namespace Viper.Views.Shared.Components.VueCdn
{
    [ViewComponent(Name = "VueCdnCreate")]
    public class VueCdnCreate : ViewComponent
    {
        public VueCdnCreate()
        {
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return await Task.Run(() => View("~/Views/Shared/Components/VueCdn/VueCdnCreate.cshtml"));
        }
    }
}
