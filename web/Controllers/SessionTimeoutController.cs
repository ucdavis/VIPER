using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Data.SqlClient;
using NLog;
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
    /// </remarks>
    [ApiController]
    [AllowAnonymous]
    [Route("/api/sessionTimeout")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class SessionTimeoutController : ControllerBase
    {
        // Ten minutes rather than zero, so a database blip cannot strand the user behind a warning they cannot dismiss.
        private const int SecondsOnError = 600;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
            try
            {
                return SessionTimeoutService.GetStatus(_viperContext);
            }
            catch (Exception ex) when (ex is SqlException or InvalidOperationException)
            {
                Logger.Error(ex, "Could not read session timeout");
                return new SessionTimeoutStatus(DateTime.Now.AddSeconds(SecondsOnError), SecondsOnError);
            }
        }
    }
}
