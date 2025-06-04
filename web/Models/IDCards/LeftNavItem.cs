using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class LeftNavItem
{
    public int LeftNavItemId { get; set; }

    public int LeftNavMenuId { get; set; }

    public string? MenuItemText { get; set; }

    public bool IsHeader { get; set; }

    public int? DisplayOrder { get; set; }

    public string? Url { get; set; }

    public virtual ICollection<LeftNavItemToPermission> LeftNavItemToPermissions { get; set; } = new List<LeftNavItemToPermission>();

    public virtual LeftNavMenu LeftNavMenu { get; set; } = null!;
}
