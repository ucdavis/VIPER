namespace Viper.Models.CTS;

public partial class BundleService
{
    public int BundleServiceId { get; set; }

    public int BundleId { get; set; }

    public int ServiceId { get; set; }

    public virtual Bundle Bundle { get; set; } = null!;
    public virtual Service Service { get; set; } = null!;
}
