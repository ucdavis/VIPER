using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

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

    public virtual ICollection<BundleCompetencyLevel> BundleCompetencyLevels { get; set; } = new List<BundleCompetencyLevel>();

    public virtual Competency Competency { get; set; } = null!;

    public virtual Role? Role { get; set; }
}
