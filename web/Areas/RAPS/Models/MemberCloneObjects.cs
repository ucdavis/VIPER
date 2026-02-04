namespace Viper.Areas.RAPS.Models
{
    public class MemberCloneObjects
    {
        public List<RoleClone> Roles { get; set; } = new();
        public List<PermissionClone> Permissions { get; set; } = new();
    }
}
