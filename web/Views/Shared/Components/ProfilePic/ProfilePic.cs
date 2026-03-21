using Microsoft.AspNetCore.Mvc;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.Views.Shared.Components.ProfilePic
{
    [ViewComponent(Name = "ProfilePic")]
    public class ProfilePicViewComponent : ViewComponent
    {
        private readonly AAUDContext _AAUDContext;

        public ProfilePicViewComponent(AAUDContext context)
        {
            _AAUDContext = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? userName)
        {
            IUserHelper UserHelper = new UserHelper();
            AaudUser? user = UserHelper.GetByLoginId(_AAUDContext, userName);

            return await Task.Run(() => View("Default", user));
        }

    }
}
