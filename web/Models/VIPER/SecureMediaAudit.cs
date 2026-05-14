namespace Viper.Models.VIPER;

public class SecureMediaAudit
{
    public int Id { get; set; }

    public string? Action { get; set; }

    public string? Whoby { get; set; }

    public DateTime? Whotime { get; set; }
}
