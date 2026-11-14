using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Viper.Classes;
using Viper.Controllers;

namespace Viper.test.Controllers
{
    /// <summary>
    /// The session expiry poll must never extend the session, or it would never time out. Both
    /// controller bases in this app write a fresh expiry on every action, so the guarantee rests
    /// entirely on this controller not inheriting either of them. Pin that.
    /// </summary>
    public class SessionTimeoutControllerTests
    {
        [Fact]
        public void Controller_DoesNotInheritASessionExtendingBase()
        {
            Assert.False(typeof(ApiController).IsAssignableFrom(typeof(SessionTimeoutController)));
            Assert.False(typeof(AreaController).IsAssignableFrom(typeof(SessionTimeoutController)));
        }

        [Fact]
        public void Controller_DoesNotCarryTheSessionUpdateFilter()
        {
            Assert.Empty(typeof(SessionTimeoutController)
                .GetCustomAttributes(typeof(ApiSessionUpdateFilterAttribute), inherit: true));

            foreach (MethodInfo action in typeof(SessionTimeoutController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Assert.Empty(action.GetCustomAttributes(typeof(ApiSessionUpdateFilterAttribute), inherit: true));
            }
        }

        [Theory]
        [InlineData(typeof(SessionTimeoutController), false)]
        [InlineData(typeof(HomeController), true)]
        public async Task DoNotSlideCookie_RenewsOnlyForOtherEndpoints(Type controller, bool expectedRenew)
        {
            var httpContext = new DefaultHttpContext();
            var descriptor = new ControllerActionDescriptor { ControllerTypeInfo = controller.GetTypeInfo() };
            httpContext.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(descriptor), controller.Name));
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(), CookieAuthenticationDefaults.AuthenticationScheme);
            var context = new CookieSlidingExpirationContext(httpContext,
                new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler)),
                new CookieAuthenticationOptions(), ticket, TimeSpan.FromHours(7), TimeSpan.FromHours(5))
            { ShouldRenew = true };

            await SessionTimeoutController.DoNotSlideCookie(context);

            Assert.Equal(expectedRenew, context.ShouldRenew);
        }
    }
}
