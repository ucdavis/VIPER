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
                cloneObjects.CompareRoleMembers(source, target);                
            }

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
                    cloneObjects.CompareMemberPermissions(source, target);
                }
            }

            return cloneObjects;
        }
    }
}
