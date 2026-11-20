using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models;

namespace Viper.Controllers
{
    /// <summary>
    /// Read-only session expiry poll for the session timeout dialog. The user comes from the auth cookie.
    /// </summary>
    /// <remarks>
    /// Inherits ControllerBase, not Viper.Classes.ApiController or AreaController: both extend the session on
    /// every action. The [ApiController] attribute below is MVC's ApiControllerAttribute and does not.
    /// [AllowAnonymous] so a caller whose cookie is gone gets 0 seconds and a Log in button, not a challenge redirect.
    /// [ApiExceptionFilter] so a failed read is answered here: left unhandled, production re-executes /Error on
    /// HomeController, an AreaController, and the failed poll would extend the session.
    /// </remarks>
    [ApiController]
    [ApiExceptionFilter]
    [AllowAnonymous]
    [Route("/api/sessionTimeout")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class SessionTimeoutController : ControllerBase
    {
        private readonly VIPERContext _viperContext;

        public SessionTimeoutController(VIPERContext viperContext)
        {
            _viperContext = viperContext;
        }

        // A background read must not slide the auth cookie either, or a tab left open on a page with no
        // session row would poll forever and keep the login alive past its 12 hours.
        internal static Task DoNotSlideCookie(CookieSlidingExpirationContext context)
        {
            var action = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (action?.ControllerTypeInfo == typeof(SessionTimeoutController))
            {
                context.ShouldRenew = false;
            }
            return Task.CompletedTask;
        }

        [HttpGet]
        public SessionTimeoutStatus GetSessionTimeout()
        {
            // No catch: the exception filter turns a database failure into a 500, which the client treats as a
            // failed poll and retries.
            // A made-up expiry here would read as a real one and could hide a live warning.
            DateTime? cookieExpiry = HttpContext.Features.Get<IAuthenticateResultFeature>()
                ?.AuthenticateResult?.Properties?.ExpiresUtc?.LocalDateTime;
            return SessionTimeoutService.GetStatus(_viperContext, cookieExpiry);
        }
    }
}
