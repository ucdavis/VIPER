using System;
using System.Collections.Generic;

namespace Viper.Models.CTS;

public partial class BundleCompetencyGroup
{
    public int BundleCompetencyGroupId { get; set; }

    public int BundleId { get; set; }

    public string Name { get; set; } = null!;

    public int Order { get; set; }

    public virtual Bundle Bundle { get; set; } = null!;

    public virtual ICollection<BundleCompetency> BundleCompetencies { get; set; } = new List<BundleCompetency>();

    //public virtual ICollection<StudentCompetency> StudentCompetencies { get; set; } = new List<StudentCompetency>();
}
