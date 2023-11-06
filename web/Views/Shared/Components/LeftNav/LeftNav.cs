using Microsoft.AspNetCore.Mvc;
using Viper.Models.AAUD;

namespace Viper.Views.Shared.Components.LeftNav
{
    [ViewComponent(Name = "LeftNav")]
    public class LeftNavViewComponent : ViewComponent
    {
        public LeftNavViewComponent()
        {
        }

        public async Task<IViewComponentResult> InvokeAsync(AaudUser user, int nav)
        {
            


            return await Task.Run(() => View("Default", user));
        }

    }
}
