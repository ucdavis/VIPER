namespace Viper.Models.VIPER;

public class SessionTimeout
{
    public string LoginId { get; set; } = null!;
    public DateTime SessionTimeoutDateTime { get; set; }
    public string Service { get; set; } = null!;
}
