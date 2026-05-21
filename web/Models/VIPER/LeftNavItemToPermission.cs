namespace Viper.Models.VIPER;

public class LeftNavItemToPermission
{
    public int LeftNavItemPermissionId { get; set; }

    public int LeftNavItemId { get; set; }

    public string Permission { get; set; } = null!;

    public virtual LeftNavItem LeftNavItem { get; set; } = null!;
}
