using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class Role
{
    public int RoleId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<BundleCompetency> BundleCompetencies { get; set; } = new List<BundleCompetency>();

    public virtual ICollection<BundleRole> BundleRoles { get; set; } = new List<BundleRole>();

    public virtual ICollection<CourseRole> CourseRoles { get; set; } = new List<CourseRole>();

    public virtual ICollection<SessionCompetency> SessionCompetencies { get; set; } = new List<SessionCompetency>();
}
