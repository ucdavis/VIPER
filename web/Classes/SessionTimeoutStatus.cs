namespace Viper.Classes
{
    /// <summary>
    /// Session expiry contract polled by the session timeout dialog.
    /// </summary>
    public class SessionTimeoutStatus
    {
        public string SessionTimeoutDateTime { get; set; } = string.Empty;

        public int SecondsUntilTimeout { get; set; }
    }
}
