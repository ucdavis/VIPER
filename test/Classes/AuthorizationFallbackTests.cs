using Microsoft.AspNetCore.Authorization;
using Viper.Areas.CMS.Controllers;
using Viper.Controllers;

namespace Viper.test.Classes
{
    /// <summary>
    /// Asserts that the controllers which answer anonymously by design still carry [AllowAnonymous].
    /// The FallbackPolicy in Program.cs is not asserted here: this only pins the opt-outs, because
    /// losing one would deny the endpoint at runtime and break sign-in or the public nav rather
    /// than fail a build.
    /// </summary>
    public class AuthorizationFallbackTests
    {
        // EntraLogoutController is called by Entra, never by a signed-in browser, so the
        // FallbackPolicy would deny every front-channel logout and users would stay signed in to
        // VIPER after signing out of Entra, with nothing but a 401 in the logs to show for it.
        public static TheoryData<Type> AnonymousControllers() =>
            new(typeof(CMSController), typeof(LayoutController), typeof(LoggedInUserController),
                typeof(EntraLogoutController));

        [Theory]
        [MemberData(nameof(AnonymousControllers))]
        public void AnonymousControllers_CarryAllowAnonymous(Type controller)
        {
            Assert.True(controller.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Length > 0,
                $"{controller.Name} answers anonymously by design and must carry [AllowAnonymous], "
                + "otherwise the FallbackPolicy denies it");
        }
    }
}
