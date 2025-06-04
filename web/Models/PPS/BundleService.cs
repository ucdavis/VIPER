using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class BundleService
{
    public int BundleServiceId { get; set; }

    public int BundleId { get; set; }

    public int ServiceId { get; set; }

    public virtual Bundle Bundle { get; set; } = null!;
}
