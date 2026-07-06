using Microsoft.EntityFrameworkCore;
using Viper.Areas.RAPS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Services
{
    public class CloneService
    {
        private readonly RAPSContext _context;
        private readonly RAPSAuditService _auditService;
        private readonly RAPSSecurityService _securityService;
        public IUserHelper UserHelper { get; private set; }
        private static readonly List<string> restrictedRoles = new()
        {
            "IT Leadership & Supervisors",
            "ITS_Operations",
            "ITS_Programmers",
            "VMDO CATS-Programmers",
            "VMDO CATS-Techs",
            "VMDO SVM-IT"
        };

        public CloneService(RAPSContext context, IUserHelper? userHelper = null)
        {
            _context = context;
            UserHelper = userHelper ?? new UserHelper();
            _auditService = new RAPSAuditService(context, UserHelper);
            _securityService = new RAPSSecurityService(context, UserHelper);
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
                .FindAll(rm => RAPSSecurityService.RoleBelongsToInstance(instance, rm.Role));
        }

        private async Task<List<TblMemberPermission>> GetMemberPermissions(string instance, string memberId)
        {
            return (await _context.TblMemberPermissions
                    .Include(mp => mp.Permission)
                    .Where(mp => mp.MemberId == memberId)
                    .Where(mp => !mp.Permission.Permission.StartsWith("RAPS"))
                    .OrderBy(mp => mp.Permission.Permission)
                    .ToListAsync())
                    .FindAll(rp => RAPSSecurityService.PermissionBelongsToInstance(instance, rp.Permission));

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
            List<TblRole> roles = await _context.TblRoles
                .Where(RAPSSecurityService.FilterRolesToInstance(instance))
                .Where(r => !restrictedRoles.Contains(r.Role))
                .ToListAsync();
            List<TblRoleMember> sourceMemberRoles = await GetRoleMembers(instance, sourceMemberId);
            List<TblRoleMember> targetMemberRoles = await GetRoleMembers(instance, targetMemberId);

            cloneObjects.Roles.AddRange(roles
                .Select(role =>
                {
                    TblRoleMember? source = sourceMemberRoles.FirstOrDefault(rm => rm.Role.RoleId == role.RoleId);
                    TblRoleMember? target = targetMemberRoles.FirstOrDefault(rm => rm.Role.RoleId == role.RoleId);
                    return CompareRoleMembers(source, target);
                })
                .OfType<RoleClone>());
            cloneObjects.Roles.Sort((r1, r2) => r1.Role.ToUpper().CompareTo(r2.Role.ToUpper()));

            if (_securityService.IsAllowedTo("ClonePermissions", instance))
            {
                List<TblPermission> permissions = await _context.TblPermissions
                    .Where(RAPSSecurityService.FilterPermissionsToInstance(instance))
                    .ToListAsync();
                List<TblMemberPermission> sourceMemberPermissions = await GetMemberPermissions(instance, sourceMemberId);
                List<TblMemberPermission> targetMemberPermissions = await GetMemberPermissions(instance, targetMemberId);
                cloneObjects.Permissions.AddRange(permissions
                    .Select(permission =>
                    {
                        TblMemberPermission? source = sourceMemberPermissions.FirstOrDefault(rp => rp.PermissionId == permission.PermissionId);
                        TblMemberPermission? target = targetMemberPermissions.FirstOrDefault(rp => rp.PermissionId == permission.PermissionId);
                        return CompareMemberPermissions(source, target);
                    })
                    .OfType<PermissionClone>());
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
                roleClone = new RoleClone
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
                if (datesDiffer && accessDiffers)
                    action = PermissionClone.CloneAction.UpdateAndAccessFlag;
                else if (datesDiffer)
                    action = PermissionClone.CloneAction.Update;
                else if (accessDiffers)
                    action = PermissionClone.CloneAction.AccessFlag;
            }
            else if (source == null && target != null)
            {
                action = PermissionClone.CloneAction.Delete;
            }
            else if (source != null && target == null)
            {
                action = PermissionClone.CloneAction.Create;
            }

            if (action != null)
            {
                permissionClone = new PermissionClone
                {
                    Action = (PermissionClone.CloneAction)action,
                    Source = source != null ? MemberPermissionCreateUpdate.CreateMemberPermission(source) : null,
                    Target = target != null ? MemberPermissionCreateUpdate.CreateMemberPermission(target) : null,
                    Permission = source?.Permission.Permission ?? target?.Permission.Permission
                };
            }
            return permissionClone;
        }

        public async Task Clone(string instance, string sourceMemberId, string targetMemberId, CloneConfirm objectsToClone)
        {
            //GetUserComparison loads the target's existing rows into the change tracker, so Update/Delete
            //must act on those tracked instances (via FindAsync) instead of attaching new ones with the same keys
            MemberCloneObjects memberCloneObjects = await GetUserComparison(instance, sourceMemberId, targetMemberId);
            string? modBy = UserHelper.GetCurrentUser()?.LoginId;
            await CloneRoles(memberCloneObjects.Roles, new HashSet<int>(objectsToClone.RoleIds), targetMemberId, modBy);
            await ClonePermissions(memberCloneObjects.Permissions, new HashSet<int>(objectsToClone.PermissionIds), targetMemberId, modBy);

            //single save so role changes, permission changes, and audit logs commit or fail together
            await _context.SaveChangesAsync();
        }

        private async Task CloneRoles(List<RoleClone> roles, HashSet<int> roleIds, string targetMemberId, string? modBy)
        {
            foreach (RoleClone role in roles
                .Where(role => roleIds.Contains(role.RoleId)))
            {
                DateTime? startDate = role.Source?.StartDate?.ToDateTime(new TimeOnly(0, 0, 0));
                DateTime? endDate = role.Source?.EndDate?.ToDateTime(new TimeOnly(0, 0, 0));
                switch (role.Action)
                {
                    case RoleClone.CloneAction.Create:
                        {
                            TblRoleMember rm = new()
                            {
                                RoleId = role.RoleId,
                                MemberId = targetMemberId,
                                ModBy = modBy,
                                ModTime = DateTime.Now,
                                StartDate = startDate,
                                EndDate = endDate,
                                AddDate = DateTime.Now
                            };
                            _context.TblRoleMembers.Add(rm);
                            _auditService.AuditRoleMemberChange(rm, RAPSAuditService.AuditActionType.Create, null, modBy);
                            break;
                        }
                    case RoleClone.CloneAction.Update:
                        {
                            TblRoleMember? existing = await _context.TblRoleMembers.FindAsync(role.RoleId, targetMemberId);
                            if (existing != null)
                            {
                                existing.StartDate = startDate;
                                existing.EndDate = endDate;
                                existing.ModBy = modBy;
                                existing.ModTime = DateTime.Now;
                                _auditService.AuditRoleMemberChange(existing, RAPSAuditService.AuditActionType.Update, null, modBy);
                            }
                            break;
                        }
                    case RoleClone.CloneAction.Delete:
                        {
                            TblRoleMember? existing = await _context.TblRoleMembers.FindAsync(role.RoleId, targetMemberId);
                            if (existing != null)
                            {
                                _context.TblRoleMembers.Remove(existing);
                                _auditService.AuditRoleMemberChange(existing, RAPSAuditService.AuditActionType.Delete, null, modBy);
                            }
                            break;
                        }
                }
            }
        }

        private async Task ClonePermissions(List<PermissionClone> permissions, HashSet<int> permissionIds, string targetMemberId, string? modBy)
        {
            foreach (PermissionClone permission in permissions
                .Where(p => permissionIds.Contains(p.PermissionId)))
            {
                switch (permission.Action)
                {
                    case PermissionClone.CloneAction.Create:
                        {
                            TblMemberPermission mp = new()
                            {
                                PermissionId = permission.PermissionId,
                                MemberId = targetMemberId,
                                ModBy = modBy,
                                ModTime = DateTime.Now,
                                StartDate = permission.Source?.StartDate,
                                EndDate = permission.Source?.EndDate,
                                Access = permission.Source?.Access ?? 1,
                                AddDate = DateTime.Now
                            };
                            _context.TblMemberPermissions.Add(mp);
                            _auditService.AuditPermissionMemberChange(mp, RAPSAuditService.AuditActionType.Create);
                            break;
                        }
                    case PermissionClone.CloneAction.Delete:
                        {
                            TblMemberPermission? existing = await _context.TblMemberPermissions.FindAsync(targetMemberId, permission.PermissionId);
                            if (existing != null)
                            {
                                _context.TblMemberPermissions.Remove(existing);
                                _auditService.AuditPermissionMemberChange(existing, RAPSAuditService.AuditActionType.Delete);
                            }
                            break;
                        }
                    default: //Update, AccessFlag, UpdateAndAccessFlag
                        {
                            TblMemberPermission? existing = await _context.TblMemberPermissions.FindAsync(targetMemberId, permission.PermissionId);
                            if (existing != null)
                            {
                                existing.StartDate = permission.Source?.StartDate;
                                existing.EndDate = permission.Source?.EndDate;
                                existing.Access = permission.Source?.Access ?? 1;
                                existing.ModBy = modBy;
                                existing.ModTime = DateTime.Now;
                                _auditService.AuditPermissionMemberChange(existing, RAPSAuditService.AuditActionType.Update);
                            }
                            break;
                        }
                }
            }
        }
    }
}
