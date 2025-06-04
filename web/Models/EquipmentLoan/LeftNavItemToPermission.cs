using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class LeftNavItemToPermission
{
    public int LeftNavItemPermissionId { get; set; }

    public int LeftNavItemId { get; set; }

    public string Permission { get; set; } = null!;

    public virtual LeftNavItem LeftNavItem { get; set; } = null!;
}
