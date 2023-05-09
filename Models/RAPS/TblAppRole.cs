using System;
using System.Collections.Generic;

namespace Viper.Models.RAPS;

public partial class TblAppRole
{
    public int AppRoleId { get; set; }

    public int RoleId { get; set; }

    public virtual TblRole Role { get; set; } = null!;
    public virtual TblRole AppRole { get; set; } = null!;
}
