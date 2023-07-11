using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Viper.Areas.RAPS.Models;
using Viper.Areas.RAPS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;
using Web.Authorization;

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{instance=VIPER}/groups")]
    [ApiController]
    [Authorize(Roles = "VMDO SVM-IT", Policy = "2faAuthentication")]
    [Permission(Allow = "RAPS.Admin,RAPS.OUGroupsView")]
    [SupportedOSPlatform("windows")]
    public class AdGroupsController : ApiController
    {
        private readonly RAPSContext _context;
        private readonly RAPSSecurityService _securityService;
        private readonly RAPSAuditService _auditService;
        private readonly OuGroupService _ouGroupService;

        public AdGroupsController(RAPSContext context)
        {
            _context = context;
            _securityService = new RAPSSecurityService(_context);
            _auditService = new RAPSAuditService(_context);
            _ouGroupService = new OuGroupService(_context);
        }

        private List<LdapGroup> GetOuGroups()
        {
            return new LdapService().GetGroups();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Models.Group>>> GetOuGroups(string instance, string? search = null)
        {
            if (_context.OuGroups == null)
            {
                return NotFound();
            }
            var result = await _context.OuGroups
                .Where(g => search == null || g.Name.Contains(search))
                .Include(g => g.OuGroupRoles)
                .ThenInclude(gr => gr.Role)
                .ThenInclude(r => r.TblRoleMembers)
                .ToListAsync();
            List<LdapGroup> ldapGroups = GetOuGroups();

            List<Models.Group> groups = new();
            foreach(OuGroup group in result)
            {
                OuGroupRole? role = group.OuGroupRoles.FirstOrDefault(g => g.IsGroupRole);
                LdapGroup? matchingGroup = ldapGroups.FirstOrDefault(lg => string.Format("CN={0}", lg.Cn).ToLower() == group.Name.ToLower());
                bool BoxSync = ldapGroups.FirstOrDefault(lg => lg.DistinguishedName.ToLower() == group.Name.ToLower())
                    ?.ExtensionAttribute3?.ToLower() == "ucdboxsync";
                Models.Group g = new(group)
                {
                    BoxSyncEnabled = BoxSync
                };
                groups.Add(g);
            }
            groups.Sort((g1, g2) => g1.Name.CompareTo(g2.Name));
            return groups;
        }

        [HttpGet("{groupId}")]
        public async Task<ActionResult<Models.Group>> GetOuGroup(string instance, int groupId)
        {
            if (_context.OuGroups == null)
            {
                return NotFound();
            }

            var group = await _context.OuGroups
                .Include(g => g.OuGroupRoles)
                .ThenInclude(gr => gr.Role)
                .ThenInclude(r => r.TblRoleMembers)
                .Where(g => g.OugroupId == groupId)
                .FirstOrDefaultAsync();
            return group != null 
                ? new Models.Group(group)
                : NotFound();
        }

        [HttpGet("OU")]
        public ActionResult<LdapGroup> GetLdapGroups()
        {
            List<LdapGroup> ouGroups = GetOuGroups();
            return Ok(ouGroups);
        }

        // PUT: groups/5
        [HttpPut("{groupId}")]
        public async Task<IActionResult> UpdateGroup(int groupId, GroupAddEdit group)
        {
            if(groupId != group.GroupId)
            {
                BadRequest();
            }
           
            OuGroup? ouGroup = await _context.OuGroups.FindAsync(groupId);
            if(ouGroup == null)
            {
                return NotFound();
            }

            await _ouGroupService.UpdateOuGroup(ouGroup, group.Name, group.Description);
            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<OuGroup>> CreateGroup(GroupAddEdit group)
        {
            if (_context.OuGroups == null)
            {
                return NotFound();
            }
            OuGroup? ouGroup = await _context.OuGroups
                .Where(g => g.Name == group.Name)
                .FirstOrDefaultAsync();
            if(ouGroup != null)
            {
                return ValidationProblem("Group is already managed by RAPS");
            }

            OuGroup newOuGroup = await _ouGroupService.CreateOuGroup(group.Name, group.Description);
            return CreatedAtAction("CreateGroup", new { id = newOuGroup.OugroupId}, newOuGroup);
        }

        [HttpDelete("{groupId}")]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            if (_context.OuGroups == null)
            {
                return NotFound();
            }
            OuGroup? ouGroup = await _context.OuGroups.FindAsync(groupId);
            if (ouGroup == null)
            {
                return NotFound();
            }

            await _ouGroupService.DeleteOuGroup(ouGroup);
            return NoContent();
        }

        [HttpGet("{groupId}/Members")]
        public async Task<ActionResult<List<GroupMember>>> GetAllMembers(int groupId)
        {
            OuGroup? group = await _context.OuGroups.FindAsync(groupId);
            if(group == null)
            {
                return NotFound();
            }
            List<GroupMember> members = await _ouGroupService.GetAllMembers(groupId);
            Dictionary<string, bool> membersInRoles = new();
            foreach(GroupMember m in  members)
            {
                membersInRoles[m.LoginId] = true;
            }

            Dictionary<string, bool> membersInGroupLookup = new();
            List<LdapUser> usersInGroup = new LdapService().GetGroupMembership(group.Name);
            foreach(LdapUser user in usersInGroup)
            {
                membersInGroupLookup[user.SamAccountName] = true;
                //Add this "member" if they are in the ou group but not in any role that would qualify them for them membership
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
            foreach(GroupMember member in members)
            {
                member.IsInGroup = membersInGroupLookup.ContainsKey(member.LoginId);
            }
            
            return members;
        }
    }
}
