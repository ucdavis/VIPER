using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    public class MemberCloneObjects
    {
        public List<RoleClone> Roles = new List<RoleClone>();
        public List<PermissionClone> Permissions = new List<PermissionClone>();

        /// <summary>
        /// Compare a role member object from a source user to a target user, set create/update/delete, and add to the roles list
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public void CompareRoleMembers(TblRoleMember? source, TblRoleMember? target)
        {
            if (source != null && target != null)
            {
                //in both source and target, but dates are different
                if (source.StartDate != target.StartDate || source.EndDate != target.EndDate)
                {
                    Roles.Add(new RoleClone()
                    {
                        Action = RoleClone.CloneAction.Update,
                        Source = CreateRoleMember(source),
                        Target = CreateRoleMember(target)
                    });
                }
            }
            else if (source == null && target != null)
            {
                Roles.Add(new RoleClone()
                {
                    Action = RoleClone.CloneAction.Delete,
                    Target = CreateRoleMember(target)
                });
            }
            else if (source != null && target == null)
            {
                Roles.Add(new RoleClone()
                {
                    Action = RoleClone.CloneAction.Create,
                    Source = CreateRoleMember(source)
                });
            }
        }

        /// <summary>
        /// Compare a member permission object from a source user to a target user, set create/update/delete, and add to the permission list
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public void CompareMemberPermissions(TblMemberPermission? source, TblMemberPermission? target)
        {
            if (source != null && target != null)
            {
                //in both source and target, but dates are different
                bool datesDiffer = source.StartDate != target.StartDate || source.EndDate != target.EndDate;
                bool accessDiffers = source.Access != target.Access;
                if ( datesDiffer || accessDiffers )
                {
                    PermissionClone.CloneAction action = PermissionClone.CloneAction.Update;
                    if(accessDiffers)
                    {
                        action = datesDiffer ? PermissionClone.CloneAction.UpdateAndAccessFlag : PermissionClone.CloneAction.AccessFlag;
                    }
                    Permissions.Add(new PermissionClone()
                    {
                        Action = action,
                        Source = CreateMemberPermission(source),
                        Target = CreateMemberPermission(target)
                    });
                }
            }
            else if (source == null && target != null)
            {
                Permissions.Add(new PermissionClone()
                {
                    Action = PermissionClone.CloneAction.Delete,
                    Target = CreateMemberPermission(target)
                });
            }
            else if (source != null && target == null)
            {
                Permissions.Add(new PermissionClone()
                {
                    Action = PermissionClone.CloneAction.Create,
                    Source = CreateMemberPermission(source)
                });
            }
        }

        private static RoleMemberCreateUpdate CreateRoleMember(TblRoleMember roleMember)
        {
            return new RoleMemberCreateUpdate()
            {
                MemberId = roleMember.MemberId,
                RoleId = roleMember.RoleId,
                StartDate = roleMember.StartDate != null ? DateOnly.FromDateTime((System.DateTime)roleMember.StartDate) : null,
                EndDate = roleMember.EndDate != null ? DateOnly.FromDateTime((System.DateTime)roleMember.EndDate) : null
            };
        }

        private static MemberPermissionCreateUpdate CreateMemberPermission(TblMemberPermission permissionMember)
        {
            return new MemberPermissionCreateUpdate()
            {
                PermissionId = permissionMember.PermissionId,
                MemberId = permissionMember.MemberId,
                Access = permissionMember.Access,
                StartDate = permissionMember.StartDate,
                EndDate = permissionMember.EndDate
            };
        }
    }
}
