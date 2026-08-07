using System.Globalization;

namespace Viper.Models
{
    /// <summary>
    /// Session expiry contract polled by the session timeout dialog.
    /// </summary>
    public class SessionTimeoutStatus
    {
        public SessionTimeoutStatus(DateTime sessionTimeout, int secondsUntilTimeout)
        {
            // The column is a bare local datetime; carry the offset so a client in another timezone shows the right time.
            DateTimeOffset local = new(DateTime.SpecifyKind(sessionTimeout, DateTimeKind.Local));
            SessionTimeoutDateTime = local.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
            SecondsUntilTimeout = secondsUntilTimeout;
        }

        public string SessionTimeoutDateTime { get; }

        public int SecondsUntilTimeout { get; }
    }
}
