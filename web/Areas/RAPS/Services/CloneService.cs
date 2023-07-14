using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Viper.Areas.RAPS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;
using static System.Net.Mime.MediaTypeNames;

namespace Viper.Areas.RAPS.Services
{
    public class CloneService
    {
        private readonly RAPSContext _context;
        private readonly RAPSAuditService _auditService;
        private readonly RAPSSecurityService _securityService;
        private static readonly List<string> restrictedRoles = new()
        {
            "IT Leadership & Supervisors",
            "ITS_Operations",
            "ITS_Programmers",
            "VMDO CATS-Programmers",
            "VMDO CATS-Techs",
            "VMDO SVM-IT"
        };

        public CloneService(RAPSContext context)
        {
            _context = context;
            _auditService = new RAPSAuditService(context);
            _securityService = new RAPSSecurityService(context);
        }

        private async Task<List<TblRoleMember>> GetRoleMembers(string instance, string memberId)
        {
            return (await _context.TblRoleMembers
                .Include(rm => rm.Role)
                .Where(rm => rm.MemberId == memberId)
                .Where(rm => rm.ViewName == null)
                .Where(rm => !restrictedRoles.Contains(rm.Role.Role))
                .OrderBy(rm => rm.Role.DisplayName ?? rm.Role.Role)
                .ToListAsync())
                .FindAll(rm => _securityService.RoleBelongsToInstance(instance, rm.Role));
        }

        private async Task<List<TblMemberPermission>> GetMemberPermissions(string instance, string memberId)
        {
            return (await _context.TblMemberPermissions
                    .Include(mp => mp.Permission)
                    .Where(mp => mp.MemberId == memberId)
                    .Where(mp => !mp.Permission.Permission.StartsWith("RAPS"))
                    .OrderBy(mp => mp.Permission.Permission)
                    .ToListAsync())
                    .FindAll(rp => _securityService.PermissionBelongsToInstance(instance, rp.Permission));

        }

        /// <summary>
        /// Get a comparison of the roles and permissions between two users
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="sourceMemberId"></param>
        /// <param name="targetMemberId"></param>
        /// <returns></returns>
        public async Task<MemberCloneObjects> GetUserComparison(string instance, string sourceMemberId, string targetMemberId)
        {
            MemberCloneObjects cloneObjects = new();
            List<TblRole> roles= await _context.TblRoles
                .Where(RAPSSecurityService.FilterRolesToInstance(instance))
                .Where(r => !restrictedRoles.Contains(r.Role))
                .ToListAsync();
            List<TblRoleMember> sourceMemberRoles = await GetRoleMembers(instance, sourceMemberId);
            List<TblRoleMember> targetMemberRoles = await GetRoleMembers(instance, targetMemberId);

            foreach(TblRole role in roles)
            {
                TblRoleMember? source = sourceMemberRoles.FirstOrDefault(rm => rm.Role.RoleId == role.RoleId);
                TblRoleMember? target = targetMemberRoles.FirstOrDefault(rm => rm.Role.RoleId == role.RoleId);
                RoleClone? r = CompareRoleMembers(source, target);                
                if(r != null)
                {
                    cloneObjects.Roles.Add(r);
                }
            }
            cloneObjects.Roles.Sort((r1, r2) => r1.Role.ToUpper().CompareTo(r2.Role.ToUpper()));

            if(_securityService.IsAllowedTo("ClonePermissions", instance))
            {
                List<TblPermission> permissions = await _context.TblPermissions
                    .Where(RAPSSecurityService.FilterPermissionsToInstance(instance))
                    .ToListAsync();
                List<TblMemberPermission> sourceMemberPermissions = await GetMemberPermissions(instance, sourceMemberId);
                List<TblMemberPermission> targetMemberPermissions = await GetMemberPermissions(instance, targetMemberId);
                foreach (TblPermission permission in permissions)
                {
                    TblMemberPermission? source = sourceMemberPermissions.FirstOrDefault(rp => rp.PermissionId == permission.PermissionId);
                    TblMemberPermission? target = targetMemberPermissions.FirstOrDefault(rp => rp.PermissionId == permission.PermissionId);
                    PermissionClone? p = CompareMemberPermissions(source, target);
                    if(p != null)
                    {
                        cloneObjects.Permissions.Add(p);
                    }
                }
                cloneObjects.Permissions.Sort((p1, p2) => p1.Permission.ToUpper().CompareTo(p2.Permission.ToUpper()));
            }

            return cloneObjects;
        }

        /// <summary>
        /// Compare a role member object from a source user to a target user, set create/update/delete, and add to the roles list
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public RoleClone? CompareRoleMembers(TblRoleMember? source, TblRoleMember? target)
        {
            RoleClone? roleClone = null;
            RoleClone.CloneAction? action = null;
            if (source != null && target != null 
                && (source.StartDate != target.StartDate || source.EndDate != target.EndDate))
            {
                //in both source and target, but dates are different
                action = RoleClone.CloneAction.Update;
            }
            else if (source == null && target != null)
            {
                action = RoleClone.CloneAction.Delete;
            }
            else if (source != null && target == null)
            {
                action = RoleClone.CloneAction.Create;
                
            }
            if (action != null)
            {
                roleClone = new RoleClone()
                {
                    Action = (RoleClone.CloneAction)action,
                    Source = source != null ? RoleMemberCreateUpdate.CreateRoleMember(source) : null,
                    Target = target != null ? RoleMemberCreateUpdate.CreateRoleMember(target) : null,
                    Role = source?.Role.FriendlyName ?? target?.Role.FriendlyName
                };
            }
            return roleClone;
        }

        /// <summary>
        /// Compare a member permission object from a source user to a target user, set create/update/delete, and add to the permission list
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public PermissionClone? CompareMemberPermissions(TblMemberPermission? source, TblMemberPermission? target)
        {
            PermissionClone? permissionClone = null;
            PermissionClone.CloneAction? action = null;
            if (source != null && target != null)
            {
                //in both source and target, but dates are different
                bool datesDiffer = source.StartDate != target.StartDate || source.EndDate != target.EndDate;
                bool accessDiffers = source.Access != target.Access;
                action = datesDiffer && accessDiffers ? PermissionClone.CloneAction.UpdateAndAccessFlag
                    : datesDiffer ? PermissionClone.CloneAction.Update
                    : accessDiffers ? PermissionClone.CloneAction.AccessFlag
                    : null;
            }
            else if (source == null && target != null)
            {
                action = PermissionClone.CloneAction.Delete;
            }
            else if (source != null && target == null)
            {
                action = PermissionClone.CloneAction.Create;
            }

            if(action != null)
            {
                permissionClone = new PermissionClone()
                {
                    Action = (PermissionClone.CloneAction)action,
                    Source = source != null ? MemberPermissionCreateUpdate.CreateMemberPermission(source) : null,
                    Target = target != null ? MemberPermissionCreateUpdate.CreateMemberPermission(target) : null,
                    Permission = source?.Permission.Permission ?? target?.Permission.Permission
                };
            }
            return permissionClone;
        }
    }
}
