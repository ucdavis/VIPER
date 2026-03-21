namespace Viper.Models
{
    public class SessionTimeoutCheck
    {
        public DateTime SessionTimeout { get; set; }
        public int SecondsUntilTimeout { get; set; }
        public string LoginId { get; set; } = null!;
    }
}
