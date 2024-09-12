using System;
using System.Collections.Generic;

namespace Viper.Models.CTS;

public partial class BundleCompetency
{
    public int BundleCompetencyId { get; set; }

    public int BundleId { get; set; }

    public int? BundleCompetencyGroupId { get; set; }

    public int? RoleId { get; set; }

    public int CompetencyId { get; set; }

    public int Order { get; set; }

    public virtual Bundle Bundle { get; set; } = null!;

    public virtual BundleCompetencyGroup? BundleCompetencyGroup { get; set; }
}
