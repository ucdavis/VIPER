using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NuGet.Protocol.Plugins;
using System.Linq.Dynamic.Core;
using System.Runtime.Versioning;
using Viper.Areas.RAPS.Models;
using Viper.Areas.RAPS.Models.Uinform;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Services
{
    [SupportedOSPlatform("windows")]
    public class OuGroupService
    {
        private readonly RAPSContext _context;
        private readonly RAPSAuditService _auditService;
        private readonly LdapService _ldapService;
        private readonly UinformService _uInformService;

        private const string _exceptionRolePrefix = "RAPS.Groups.";
        private const string _exceptionRoleDesc = "Automatically created role for group membership by exception.";
        private const string _appRoleName = "OUGroups - Exception Membership";

        public OuGroupService(RAPSContext context)
        {
            _context = context;
            _auditService = new RAPSAuditService(_context);
            _uInformService = new();
            _ldapService = new();
        }
        
        /// <summary>
        /// Get all Raps groups, along with info from ou or ad3
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public async Task<List<Group>> GetAllGroups(string? search)
        {
            var result = await _context.OuGroups
                .Where(g => search == null || g.Name.Contains(search))
                .Include(g => g.OuGroupRoles)
                .ThenInclude(gr => gr.Role)
                .ThenInclude(r => r.TblRoleMembers)
                .ToListAsync();
            List<LdapGroup> ldapGroups = _ldapService.GetGroups();
            List<ManagedGroup> managedGroups = await _uInformService.GetManagedGroups();

            List<Group> groups = new();
            foreach (OuGroup group in result)
            {
                OuGroupRole? role = group.OuGroupRoles.FirstOrDefault(g => g.IsGroupRole);
                //for OU groups, look up the box sync attribute
                bool BoxSync = ldapGroups.FirstOrDefault(lg => lg.DistinguishedName.ToLower() == group.Name.ToLower())
                    ?.ExtensionAttribute3?.ToLower() == "ucdboxsync";
                //for AD3 groups, look up display name
                string? displayName = managedGroups.FirstOrDefault(mg => mg.DistinguishedName.ToLower() == group.Name.ToLower())?.DisplayName;
                Group g = new(group)
                {
                    BoxSyncEnabled = BoxSync,
                    DisplayName = displayName
                };
                groups.Add(g);
            }

            groups.Sort((g1, g2) => g1.Name.CompareTo(g2.Name));
            return groups;
        }

        /// <summary>
        /// Create an OU group to start managing a group in OU. Also creates a role to manage explicit membership and links it to the
        /// application role.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<OuGroup> CreateRapsGroup(string groupName, string? description)
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
                .FirstOrDefaultAsync() ?? throw new Exception("Could not find group management application role.");
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
        public async Task UpdateRapsGroup(OuGroup ouGroup, string name, string? description)
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

        /// <summary>
        /// Get everyone that should be in a group, along with the role assignment(s) that cause them to be added to the group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="filterToActive">If true, only active SVM affiliates will be returned</param>
        /// <returns></returns>
        public async Task<List<GroupMember>> GetAllMembers(int groupId, string groupName, bool filterToActive = false)
        {
            List<GroupMember> members = new();         

            //Get members from the DB
            List<TblRoleMember> roleMembers = await GetAllRoleMembers(groupId, filterToActive);

            //Create a list of members that should be in the group, plus a list of the roles that qualify them
            foreach(var roleMember in roleMembers)
            {
                if (members.Count == 0 || members.Last().MemberId != roleMember.MemberId)
                {
                    GroupMember member = CreateGroupMember(roleMember);
                    members.Add(member);
                }
                members.Last().Roles.Add(CreateGroupMemberRole(roleMember));
            }

            //Compare to users in the group in AD. Set a flag if they are in the members list and in the group. If they are in the group in AD,
            //but not in the members list, add an entry so we know to remove them.
            await CompareToGroup(groupName, members);
            
            return members;
        }

        /// <summary>
        /// Take a list of members that belong to one or more roles that qualifies them for membership in a security group. Add a 
        /// flag indicating whether they are in the group already. Add any current members of the group that are not in the given list of members
        /// so that they can be removed
        /// </summary>
        /// <param name="groupName">The group distinguished name</param>
        /// <param name="members">The list of members that should be a part of the group</param>
        /// <returns></returns>
        private async Task CompareToGroup(string groupName, List<GroupMember> members)
        {
            //Create a lookup of loginids of members that should be in the group
            Dictionary<string, bool> membersInRoles = new();
            foreach (GroupMember m in members)
            {
                membersInRoles[m.LoginId] = true;
            }

            //Create a lookup of loginids of people already in the group
            Dictionary<string, bool> membersInGroupLookup = new();

            //For OU groups, use LdapService to get members
            if (IsOuGroup(groupName))
            {
                List<LdapUser> usersInGroup = _ldapService.GetGroupMembership(groupName);
                foreach (LdapUser user in usersInGroup)
                {
                    //Record this user is in the group
                    membersInGroupLookup[user.SamAccountName] = true;
                    //Add this "member" if they are in the group in AD but not in any role that would qualify them for them membership
                    if (!membersInRoles.ContainsKey(user.SamAccountName))
                    {
                        members.Add(new GroupMember()
                        {
                            DisplayFirstName = user.GivenName,
                            DisplayLastName = user.Sn,
                            LoginId = user.SamAccountName
                        });
                    }
                }
            }
            //For AD managed groups, use the uInform API to get members
            else
            {
                ManagedGroup managedGroup = await _uInformService.GetManagedGroup(groupName);
                List<AdUser> usersInGroup = await _uInformService.GetGroupMembers(managedGroup.ObjectGuid);
                foreach(AdUser user in usersInGroup)
                {
                    //Record this user is in the group
                    membersInGroupLookup[user.SamAccountName] = true;
                    //Add this "member" if they are in the group in AD but not in any role that would qualify them for them membership
                    if (!membersInRoles.ContainsKey(user.SamAccountName))
                    {
                        members.Add(new GroupMember()
                        {
                            DisplayName = user.DisplayName,
                            LoginId = user.SamAccountName
                        });
                    }
                }
            }

            //Add a flag indicating whether they are in the group in AD
            foreach (GroupMember member in members)
            {
                member.IsInGroup = membersInGroupLookup.ContainsKey(member.LoginId);
            }
        }

        public async Task Sync(int groupId, string groupName)
        {
            List<GroupMember> members = await GetAllMembers(groupId, groupName, true);
            var toRemove = members.Where(m => m.Roles.Count == 0);
            var toAdd = members.Where(m => !(m.IsInGroup ?? true)); //IsInGroup must be non-null and false
            bool isOu = IsOuGroup(groupName);

            if(IsOuGroup(groupName))
            {
                foreach (GroupMember m in toRemove)
                {
                    UpdateOuGroupMember(groupName, m.LoginId, false);
                }

                foreach (GroupMember m in toAdd)
                {
                    UpdateOuGroupMember(groupName, m.LoginId, true);
                }
            }
            else
            {
                ManagedGroup? managedGroup = await _uInformService.GetManagedGroup(groupName);
                if(managedGroup != null)
                {
                    foreach (GroupMember m in toRemove)
                    {
                        _ = UpdateAdGroupMember(managedGroup.ObjectGuid, m.LoginId, false);
                    }

                    foreach (GroupMember m in toAdd)
                    {
                        _ = UpdateAdGroupMember(managedGroup.ObjectGuid, m.LoginId, true);
                    }
                }
            }
            
        }

        private void UpdateOuGroupMember(string groupName, string loginId, bool add)
        {
            string? dn = null;
            if(HttpHelper.Cache != null)
            {
                dn = HttpHelper.Cache.Get<string>("ou.ad3-distinguishedname-" + loginId);
            }
            if(dn == null)
            {
                dn = _ldapService.GetUser(loginId)?.DistinguishedName;
                if(HttpHelper.Cache != null)
                {
                    HttpHelper.Cache.Set("ou.ad3-distinguishedname-" + loginId, dn, new TimeSpan(0, 20, 0));
                }
            }
            
            if (dn != null)
            {
                if (add)
                {
                    _ldapService.AddUserToGroup(dn, groupName);
                }
                else
                {
                    _ldapService.RemoveUserFromGroup(dn, groupName);
                }
                
            }
        }

        private async Task UpdateAdGroupMember(string groupGuid, string loginId, bool add)
        {
            string? userGuid = null;
            if (HttpHelper.Cache != null)
            {
                userGuid = HttpHelper.Cache.Get<string>("ad3-userGuid-" + loginId);
            }
            if (userGuid == null)
            {
                userGuid = (await _uInformService.GetUser(samAccountName: loginId)).ObjectGuid;
                if (HttpHelper.Cache != null)
                {
                    HttpHelper.Cache.Set("ad3-userGuid-" + loginId, userGuid, new TimeSpan(0, 20, 0));
                }
            }
            
            if (userGuid != null)
            {
                if (add)
                {
                    await _uInformService.AddGroupMember(groupGuid, userGuid);
                }
                else
                {
                    await _uInformService.RemoveGroupMember(groupGuid, userGuid);
                }
            }
        }
        
        /// <summary>
        /// Return true if the group is in ou.ad3.ucdavis.edu, based on the name of the group
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        private static bool IsOuGroup(string groupName)
        {
            return groupName.ToLower().Contains("dc=ou");
        }

        /// <summary>
        /// Get all members of any role linked to the given group
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        private async Task<List<TblRoleMember>> GetAllRoleMembers(int groupId, bool filterToActive)
        {
            List<int> roleIds = await _context.OuGroupRoles
                .Where(gr => gr.OugroupId == groupId)
                .Select(gr => gr.RoleId)
                .ToListAsync();
            return await _context.TblRoleMembers
               .Include(rm => rm.Role)
               .Include(rm => rm.AaudUser)
               .Where(rm => roleIds.Contains(rm.RoleId))
               .Where(rm => !filterToActive || (rm.StartDate == null || rm.StartDate <= DateTime.Now))
               .Where(rm => !filterToActive || (rm.EndDate == null || rm.EndDate > DateTime.Now))
               .Where(rm => !filterToActive || rm.AaudUser.Current || rm.AaudUser.Future)
               .OrderBy(rm => rm.AaudUser.DisplayLastName)
               .ThenBy(rm => rm.AaudUser.DisplayFirstName)
               .ThenBy(rm => rm.MemberId)
               .ToListAsync();
        }

        /// <summary>
        /// Create a GroupMember from a TblRoleMember
        /// </summary>
        /// <param name="roleMember"></param>
        /// <returns></returns>
        private static GroupMember CreateGroupMember(TblRoleMember roleMember)
        {
            return new()
            {
                MemberId = roleMember.MemberId,
                LoginId = roleMember.AaudUser.LoginId,
                MailId = roleMember.AaudUser.MailId,
                DisplayFirstName = roleMember.AaudUser.DisplayFirstName,
                DisplayLastName = roleMember.AaudUser.DisplayLastName,
                DisplayName = roleMember.AaudUser.DisplayLastName + ", " + roleMember.AaudUser.DisplayFirstName,
                Current = roleMember.AaudUser.Current,
                Roles = new List<GroupMemberRole>()
            };
        }

        /// <summary>
        /// Create a GroupMemberRole from a TblRoleMember
        /// </summary>
        /// <param name="roleMember"></param>
        /// <returns></returns>
        private static GroupMemberRole CreateGroupMemberRole(TblRoleMember roleMember)
        {
            return new()
            {
                RoleId = roleMember.RoleId,
                Role = roleMember.Role.FriendlyName,
                AddDate = roleMember.AddDate != null ? DateOnly.FromDateTime((System.DateTime)roleMember.AddDate) : null,
                StartDate = roleMember.StartDate != null ? DateOnly.FromDateTime((System.DateTime)roleMember.StartDate) : null,
                EndDate = roleMember.EndDate != null ? DateOnly.FromDateTime((System.DateTime)roleMember.EndDate) : null,
                ModBy = roleMember.ModBy,
                ModDate = roleMember.ModTime != null ? DateOnly.FromDateTime((System.DateTime)roleMember.ModTime) : null,
                ViewName = roleMember.Role.ViewName
            };
        }

        private async Task<TblRole> GetGroupRole(int groupId)
        {
            TblRole? role = await _context.OuGroupRoles
                .Where(gr => gr.OugroupId == groupId)
                .Where(gr => gr.IsGroupRole)
                .Select(gr => gr.Role)
                .FirstOrDefaultAsync() ?? throw new Exception("Could not find exception role for group.");
            return role;
        }
    }
}
