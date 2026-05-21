using NLog;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.VIPER;

namespace Viper.Classes.Utilities
{
    public static class SessionTimeoutService
    {
        private const int SessionTimeoutSeconds = (29 * 60) + 30;

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

        public static SessionTimeout? GetSessionTimeout(VIPERContext context)
        {
            string loggedInUserId = GetLoggedInUserId();
            string service = GetService();
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
