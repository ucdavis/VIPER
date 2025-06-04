using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class BundleRole
{
    public int BundleRoleId { get; set; }

    public int BundleId { get; set; }

    public int RoleId { get; set; }

    public virtual Bundle Bundle { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
