using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class Bundle
{
    public int BundleId { get; set; }

    public string Name { get; set; } = null!;

    public bool Clinical { get; set; }

    public bool Assessment { get; set; }

    public bool Milestone { get; set; }

    public virtual ICollection<BundleCompetency> BundleCompetencies { get; set; } = new List<BundleCompetency>();

    public virtual ICollection<BundleCompetencyGroup> BundleCompetencyGroups { get; set; } = new List<BundleCompetencyGroup>();

    public virtual ICollection<BundleRole> BundleRoles { get; set; } = new List<BundleRole>();

    public virtual ICollection<BundleService> BundleServices { get; set; } = new List<BundleService>();

    public virtual ICollection<MilestoneLevel> MilestoneLevels { get; set; } = new List<MilestoneLevel>();

    public virtual ICollection<StudentCompetency> StudentCompetencies { get; set; } = new List<StudentCompetency>();
}
