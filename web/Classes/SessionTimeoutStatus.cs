namespace Viper.Classes
{
    /// <summary>
    /// Session expiry contract polled by the session timeout dialog. Key names and shape match the
    /// legacy ColdFusion endpoint this replaces, so the client code is unchanged.
    /// </summary>
    public class SessionTimeoutStatus
    {
        public string SessionTimeoutDateTime { get; set; } = string.Empty;

        public int SecondsUntilTimeout { get; set; }
    }
}
