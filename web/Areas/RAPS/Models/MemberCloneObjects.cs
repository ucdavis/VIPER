using NuGet.Protocol.Plugins;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    public class MemberCloneObjects
    {
        public List<RoleClone> Roles = new();
        public List<PermissionClone> Permissions = new();
    }
}
