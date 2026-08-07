using Microsoft.EntityFrameworkCore;
using NLog;
using Viper.Classes.SQLContext;
using Viper.Models;
using Viper.Models.AAUD;
using Viper.Models.VIPER;

namespace Viper.Classes.Utilities
{
    public static class SessionTimeoutService
    {
        internal const int SessionTimeoutSeconds = (29 * 60) + 30;

        public static void UpdateSessionTimeout(VIPERContext context)
        {
            string loggedInUserId = GetLoggedInUserId();
            string service = GetService();
            var logger = LogManager.GetCurrentClassLogger();

            if (!string.IsNullOrEmpty(loggedInUserId) && context != null)
            {
                SessionTimeout? record = context.SessionTimeouts.Find(loggedInUserId, service);
                if (record != null)
                {
                    record.SessionTimeoutDateTime = DateTime.Now.AddSeconds(SessionTimeoutSeconds);
                    context.Update(record);
                }
                else
                {
                    context.Add(new SessionTimeout
                    {
                        LoginId = loggedInUserId,
                        SessionTimeoutDateTime = DateTime.Now.AddSeconds(SessionTimeoutSeconds),
                        Service = service
                    });
                }
                context.SaveChanges();
                logger.Debug(
                    string.Format("Updated session timeout. LoggedInUserId: {0}", loggedInUserId)
                );
            }
            else
            {
                logger.Warn(
                    string.Format("Could not update session timeout. Context {0} LoggedInUserId: {1}", context == null ? "is null" : "is not null", loggedInUserId)
                );
            }
        }

        // cookieExpiry: pass it only from a request that cannot renew the cookie (the poll). RefreshSession
        // may renew it, and the old expiry would then understate the session it just extended.
        public static SessionTimeoutStatus GetStatus(VIPERContext context, DateTime? cookieExpiry = null)
        {
            // The cookie's login id, not GetCurrentUser(): that swallows a failed AAUD lookup and returns null,
            // which would read as "no row" and report a full window over a live warning.
            HttpContext? httpContext = HttpHelper.HttpContext;
            string? loggedInUserId = httpContext?.User.Identity?.Name;
            string service = GetService();
            SessionTimeout? record = string.IsNullOrEmpty(loggedInUserId)
                ? null
                : context.SessionTimeouts.AsNoTracking()
                    .FirstOrDefault(s => s.LoginId == loggedInUserId && s.Service == service);
            return ToStatus(record, httpContext?.User.Identity?.IsAuthenticated == true, cookieExpiry);
        }

        // Only AreaController, ApiSessionUpdateFilter and RefreshSession write the row, so a page served by a
        // plain Controller leaves a valid session with no row: grant a full window rather than report it expired.
        // Either way the sign-in cookie's own expiry caps it, so the warning still shows before that runs out
        // (no row, or a row kept fresh from another browser).
        internal static SessionTimeoutStatus ToStatus(SessionTimeout? record, bool authenticated, DateTime? cookieExpiry = null)
        {
            DateTime now = DateTime.Now;
            if (record == null && !authenticated)
            {
                return new(now, 0);
            }
            DateTime expiry = record?.SessionTimeoutDateTime ?? now.AddSeconds(SessionTimeoutSeconds);
            if (cookieExpiry < expiry)
            {
                expiry = cookieExpiry.Value;
            }
            return new(expiry, (int)(expiry - now).TotalSeconds);
        }

        private static string GetService()
        {
            string service = "Viper2";
            if (HttpHelper.Environment?.EnvironmentName == "Development")
            {
                //on development, append development to service, so that sessions with secure-test aren't affected
                service += "-dev";
            }
            return service;

        }

        private static string GetLoggedInUserId()
        {
            UserHelper userHelper = new();
            AaudUser? loggedInUser = userHelper.GetCurrentUser();
            return loggedInUser?.LoginId ?? string.Empty;
        }
    }
}
