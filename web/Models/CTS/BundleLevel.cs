using System;
using System.Collections.Generic;

namespace Viper.Models.CTS;

public partial class BundleLevel
{
    public int BundleLevelId { get; set; }

    public int BundleId { get; set; }

    public int LevelId { get; set; }

    public virtual Bundle Bundle { get; set; } = null!;
    public virtual Level Level { get; set; } = null!;
}
