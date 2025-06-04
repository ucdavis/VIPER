using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class BundleCompetencyLevel
{
    public int BundleCompetencyLevelId { get; set; }

    public int BundleCompetencyId { get; set; }

    public int LevelId { get; set; }

    public virtual BundleCompetency BundleCompetency { get; set; } = null!;

    public virtual Level Level { get; set; } = null!;
}
