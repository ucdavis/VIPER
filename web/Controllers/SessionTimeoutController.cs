using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NLog;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;

namespace Viper.Controllers
{
    /// <summary>
    /// Read-only session expiry poll for the session timeout dialog. Takes no parameters: the user
    /// comes from the auth cookie.
    /// </summary>
    /// <remarks>
    /// Inherits ControllerBase, not Viper.Classes.ApiController or Viper.Classes.AreaController.
    /// Both of those extend the session on every action, which would stop the session ever expiring,
    /// and ApiController also wraps responses in an ApiResponse envelope the dialog cannot read. The
    /// [ApiController] attribute below is Microsoft.AspNetCore.Mvc.ApiControllerAttribute and
    /// carries none of that behaviour.
    ///
    /// No [Authorize]: attribute-routed actions do not pick up RequireAuthorization from the
    /// conventional routes, and the action takes no parameters and reads identity from the cookie,
    /// so an anonymous caller learns only that it has no session. The dialog then offers a log in
    /// rather than retrying a call that would 401.
    /// </remarks>
    [ApiController]
    [Route("/api/sessionTimeout")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class SessionTimeoutController : ControllerBase
    {
        // Ten minutes rather than zero, so a database blip cannot strand the user behind a warning
        // dialog they cannot dismiss.
        private const int SecondsOnError = 600;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly VIPERContext _viperContext;

        public SessionTimeoutController(VIPERContext viperContext)
        {
            _viperContext = viperContext;
        }

        [HttpGet]
        public ActionResult<SessionTimeoutStatus> GetSessionTimeout()
        {
            try
            {
                Models.VIPER.SessionTimeout? record = SessionTimeoutService.GetSessionTimeout(_viperContext);
                if (record != null)
                {
                    return Status(record.SessionTimeoutDateTime,
                        (int)(record.SessionTimeoutDateTime - DateTime.Now).TotalSeconds);
                }

                // Authenticated but untracked. Only AreaController, ApiSessionUpdateFilter and
                // RefreshSession write the row, so a page served by a plain Controller leaves a
                // valid session with no row. Grant a full window rather than report it expired.
                return User.Identity?.IsAuthenticated == true
                    ? Status(DateTime.Now.AddSeconds(SessionTimeoutService.SessionTimeoutSeconds),
                        SessionTimeoutService.SessionTimeoutSeconds)
                    : Status(DateTime.Now, 0);
            }
            catch (SqlException ex)
            {
                return CouldNotRead(ex);
            }
            catch (InvalidOperationException ex)
            {
                return CouldNotRead(ex);
            }
        }

        private static SessionTimeoutStatus CouldNotRead(Exception ex)
        {
            Logger.Error(ex, "Could not read session timeout");
            return Status(DateTime.Now.AddSeconds(SecondsOnError), SecondsOnError);
        }

        // Carry the offset. The column is a bare datetime written from DateTime.Now, so it is local
        // wall-clock; without the offset a client in another timezone reads it as its own and shows
        // the wrong expiry time.
        private static SessionTimeoutStatus Status(DateTime sessionTimeout, int secondsUntilTimeout)
        {
            DateTimeOffset local = new(DateTime.SpecifyKind(sessionTimeout, DateTimeKind.Local));
            return new SessionTimeoutStatus
            {
                SessionTimeoutDateTime = local.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture),
                SecondsUntilTimeout = secondsUntilTimeout
            };
        }
    }
}
