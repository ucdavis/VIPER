using NuGet.Protocol.Plugins;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    public class MemberCloneObjects
    {
        public List<RoleClone> Roles = new List<RoleClone>();
        public List<PermissionClone> Permissions = new List<PermissionClone>();
    }
}
