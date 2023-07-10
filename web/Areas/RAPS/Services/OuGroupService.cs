using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Services
{
    public class OuGroupService
    {
        private readonly RAPSContext _context;
        private readonly RAPSAuditService _auditService;

        private const string _exceptionRolePrefix = "RAPS.Groups.";
        private const string _exceptionRoleDesc = "Automatically created role for group membership by exception.";
        private const string _appRoleName = "OUGroups - Exception Membership";

        public OuGroupService(RAPSContext context)
        {
            _context = context;
            _auditService = new RAPSAuditService(_context);
        }

        /// <summary>
        /// Create an OU group to start managing a group in OU. Also creates a role to manage explicit membership and links it to the
        /// application role.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<OuGroup> CreateOuGroup(string groupName, string? description)
        {
            OuGroup newOuGroup = new()
            {
                Name = groupName,
                Description = description ?? string.Empty
            };

            //First create the ou group in the DB
            using var transaction = _context.Database.BeginTransaction();
            _context.OuGroups.Add(newOuGroup);
            await _context.SaveChangesAsync();
            _auditService.AuditGroupChange(newOuGroup, RAPSAuditService.AuditActionType.Create);
            await _context.SaveChangesAsync();

            //next add a role to manage explicit membership of this group
            TblRole tblRole = new()
            {
                Role = _exceptionRolePrefix + newOuGroup.Name,
                Description = _exceptionRoleDesc,
                Application = 0,
                UpdateFreq = 0
            };
            _context.TblRoles.Add(tblRole);
            await _context.SaveChangesAsync();
            _auditService.AuditRoleChange(tblRole, RAPSAuditService.AuditActionType.Create);
            await _context.SaveChangesAsync();

            //then link new the role to the group
            OuGroupRole groupRole = new()
            {
                OugroupId = newOuGroup.OugroupId,
                RoleId = tblRole.RoleId,
                IsGroupRole = true
            };
            _context.OuGroupRoles.Add(groupRole);
            await _context.SaveChangesAsync();
            _auditService.AuditOuGroupRoleChange(groupRole, RAPSAuditService.AuditActionType.Create);
            await _context.SaveChangesAsync();

            //finally, link the role and the group management app role, so people who can manage groups can see now see this one
            TblRole? AppRole = await _context.TblRoles
                .Where(r => r.Role == _appRoleName)
                .FirstOrDefaultAsync();
            if (AppRole == null)
            {
                throw new Exception("Could not find group management application role.");
            }
            TblAppRole appRole = new()
            {
                AppRoleId = AppRole.RoleId,
                RoleId = tblRole.RoleId
            };
            _context.TblAppRoles.Add(appRole);
            await _context.SaveChangesAsync();
            transaction.Commit();

            return newOuGroup;
        }

        /// <summary>
        /// Update the name and description of an ou group. Also change the role name of the group membership role.
        /// </summary>
        /// <param name="ouGroup"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        public async Task UpdateOuGroup(OuGroup ouGroup, string name, string? description)
        {
            ouGroup.Name = name;
            ouGroup.Description = description;
            _context.Entry(ouGroup).State = EntityState.Modified;
            _auditService.AuditGroupChange(ouGroup, RAPSAuditService.AuditActionType.Update);
            await _context.SaveChangesAsync();

            //find the role for managing explicit membership and update its name
            TblRole role = await GetGroupRole(ouGroup.OugroupId);
            role.Role = _exceptionRolePrefix + ouGroup.Name;
            _context.Entry(role).State = EntityState.Modified;
            _auditService.AuditRoleChange(role, RAPSAuditService.AuditActionType.Update);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Remove an ouGroup, after removing everything depending on it:
        ///     members from explicit group membership role
        ///     the link between the group membership role and the app role
        ///     links to any roles
        ///     the group membership role
        ///     the group
        /// </summary>
        /// <param name="ouGroup">The OuGroup to delete</param>
        public async Task DeleteOuGroup(OuGroup ouGroup)
        {
            //remove members of the group role
            TblRole groupRole = await GetGroupRole(ouGroup.OugroupId);
            List<TblRoleMember> groupRoleMembers = await _context.TblRoleMembers.Where(rm => rm.RoleId == groupRole.RoleId).ToListAsync();
            foreach(TblRoleMember roleMember in groupRoleMembers)
            {
                _context.TblRoleMembers.Remove(roleMember);
                _auditService.AuditRoleMemberChange(roleMember, RAPSAuditService.AuditActionType.Delete, "Membership removed during deletion of group.");
            }
            await _context.SaveChangesAsync();

            //remove the link between the group role and the app role
            TblAppRole? appRole = await _context.TblAppRoles.FirstOrDefaultAsync(ap => ap.RoleId == groupRole.RoleId);
            if(appRole != null)
            {
                _context.TblAppRoles.Remove(appRole);
            }

            //remove any role links to this group
            List<OuGroupRole> groupRoles = await _context.OuGroupRoles.Where(gr => gr.OugroupId == ouGroup.OugroupId).ToListAsync();
            foreach (OuGroupRole role in groupRoles)
            {
                _context.OuGroupRoles.Remove(role);
                _auditService.AuditOuGroupRoleChange(role, RAPSAuditService.AuditActionType.Delete);
            }
            await _context.SaveChangesAsync();

            //remove the group membership role
            _context.TblRoles.Remove(groupRole);
            _auditService.AuditRoleChange(groupRole, RAPSAuditService.AuditActionType.Delete);
            await _context.SaveChangesAsync();

            //finally remove the group
            _context.OuGroups.Remove(ouGroup);
            _auditService.AuditGroupChange(ouGroup, RAPSAuditService.AuditActionType.Delete);
            await _context.SaveChangesAsync();
        }

        private async Task<TblRole> GetGroupRole(int groupId)
        {
            TblRole? role = await _context.OuGroupRoles
                .Where(gr => gr.OugroupId == groupId)
                .Where(gr => gr.IsGroupRole)
                .Select(gr => gr.Role)
                .FirstOrDefaultAsync();
            if (role == null)
            {
                throw new Exception("Could not find exception role for group.");
            }
            return role;
        }
    }
}
