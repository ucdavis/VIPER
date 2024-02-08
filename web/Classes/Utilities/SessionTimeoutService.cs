using Microsoft.IdentityModel.Tokens;
using NLog;
using Polly;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.VIPER;

namespace Viper.Classes.Utilities
{
    public class SessionTimeoutService
    {
        public static void UpdateSessionTimeout()
        {
            string loggedInUserId = GetLoggedInUserId();
            string service = GetService();
            VIPERContext? context = (VIPERContext?)HttpHelper.HttpContext?.RequestServices.GetService(typeof(VIPERContext));
            var logger = LogManager.GetCurrentClassLogger();

            if (!string.IsNullOrEmpty(loggedInUserId) && context != null) {
                SessionTimeout? record = context.SessionTimeouts.Find(loggedInUserId, service);
                if(record != null)
                {
                    record.SessionTimeoutDateTime = DateTime.Now.AddSeconds(29 * 60 + 30);
                    context.Update(record);
                }
                else
                {
                    context.Add(new SessionTimeout()
                    {
                        LoginId = loggedInUserId,
                        SessionTimeoutDateTime = DateTime.Now.AddMinutes(29 * 60 + 30),
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

        public static SessionTimeout? GetSessionTimeout()
        {
            string loggedInUserId = GetLoggedInUserId();
            string service = GetService();
            VIPERContext? context = (VIPERContext?)HttpHelper.HttpContext?.RequestServices.GetService(typeof(VIPERContext));
            if (!string.IsNullOrEmpty(loggedInUserId) && context != null)
            {
                SessionTimeout? record = context.SessionTimeouts.Find(loggedInUserId, service);
                if (record != null)
                {
                    return record;
                }
            }
            return null;
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
