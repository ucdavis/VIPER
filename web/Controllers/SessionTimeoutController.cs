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
    /// Read-only session expiry poll, replacing the legacy ColdFusion endpoint that took the login
    /// id as an unauthenticated query parameter. The user comes from the auth cookie instead.
    /// </summary>
    /// <remarks>
    /// Deliberately does not inherit this app's Viper.Classes.ApiController base class. That base
    /// extends the session on every action via ApiSessionUpdateFilter, so polling it would mean the
    /// session never expired, and it wraps responses in an ApiResponse envelope, which would break
    /// the contract the dialog reads. Viper.Classes.AreaController extends the session for the same
    /// reason, so it is out too. The [ApiController] attribute below is the unrelated MVC attribute
    /// (Microsoft.AspNetCore.Mvc.ApiControllerAttribute) and carries none of that behaviour.
    ///
    /// No [Authorize] on purpose, matching RefreshSession. Attribute-routed actions do not pick up
    /// the RequireAuthorization on the conventional routes, and this endpoint takes no parameters
    /// and reads identity from the cookie, so an anonymous caller learns only that it has no
    /// session. That is the answer we want: the dialog then offers a log in rather than retrying a
    /// call that would 401.
    /// </remarks>
    [ApiController]
    [Route("/api/sessionTimeout")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class SessionTimeoutController : ControllerBase
    {
        // Legacy returned ten minutes on a database error rather than zero, so a blip cannot strand
        // the user behind a warning dialog they cannot dismiss.
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

                // Authenticated but untracked. Only AreaController, ApiController and RefreshSession
                // write the row, so a page served by a plain Controller leaves a valid session with
                // no row. Legacy could not tell that apart from a dead session and called it expired;
                // with the cookie we can, and telling this user to log in would be wrong.
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
