using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class LeftNavMenu
{
    public int LeftNavMenuId { get; set; }

    public string System { get; set; } = null!;

    public string? Application { get; set; }

    public string? Page { get; set; }

    public string? ViperSectionPath { get; set; }

    public string? FriendlyName { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public string? MenuHeaderText { get; set; }

    public virtual ICollection<LeftNavItem> LeftNavItems { get; set; } = new List<LeftNavItem>();
}
