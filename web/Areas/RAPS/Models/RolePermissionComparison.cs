using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    public class RolePermissionComparison
    {
        public List<RolePermission> Role1Permissions { get; set; } = new List<RolePermission>();
        public List<RolePermission> Role2Permissions { get; set; } = new List<RolePermission>();

        public RolePermissionComparison()
        {

        }

        public RolePermissionComparison(List<TblRolePermission> role1Permissions, List<TblRolePermission> role2Permissions)
        {
            foreach (var p in role1Permissions)
            {
                Role1Permissions.Add(new RolePermission()
                {
                    PermissionId = p.PermissionId,
                    Name = p.Permission.Permission,
                    IsInOtherList = role2Permissions.FindIndex(p2 => p2.PermissionId == p.PermissionId) >= 0
                });
            }

            foreach (var p in role2Permissions)
            {
                Role2Permissions.Add(new RolePermission()
                {
                    PermissionId = p.PermissionId,
                    Name = p.Permission.Permission,
                    IsInOtherList = role1Permissions.FindIndex(p1 => p1.PermissionId == p.PermissionId) >= 0
                });
            }
        }

        public class RolePermission
        {
            public int PermissionId { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsInOtherList { get; set; }
        }
    }
}
