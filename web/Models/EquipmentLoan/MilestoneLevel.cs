using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class MilestoneLevel
{
    public int MilestoneLevelId { get; set; }

    public int LevelId { get; set; }

    public int BundleId { get; set; }

    public string Description { get; set; } = null!;

    public virtual Bundle Bundle { get; set; } = null!;

    public virtual Level Level { get; set; } = null!;
}
