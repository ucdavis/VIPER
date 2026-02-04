using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Runtime.Versioning;
using Viper.Areas.RAPS.Models;
using Viper.Areas.RAPS.Models.Uinform;
using Viper.Areas.RAPS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models.RAPS;
using Web.Authorization;

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{instance=VIPER}/Groups")]
    [ApiController]
    [Authorize(Roles = "VMDO SVM-IT", Policy = "2faAuthentication")]
    [Permission(Allow = "RAPS.Admin,RAPS.OUGroupsView")]
    [SupportedOSPlatform("windows")]
    public class AdGroupsController : ApiController
    {
        private readonly RAPSContext _context;
        private readonly OuGroupService _ouGroupService;

        public AdGroupsController(RAPSContext context)
        {
            _context = context;
            _ouGroupService = new OuGroupService(_context);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Models.Group>>> GetOuGroups(string? search = null)
        {
            if (_context.OuGroups == null)
            {
                return NotFound();
            }

            return await _ouGroupService.GetAllGroups(search);
        }

        [HttpGet("{groupId}")]
        public async Task<ActionResult<Models.Group>> GetOuGroup(int groupId)
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
            List<LdapGroup> ouGroups = ActiveDirectoryService.GetGroups();
            return Ok(ouGroups);
        }

        [HttpGet("AD")]
        public async Task<ActionResult<LdapGroup>> GetAdGroups()
        {
            List<ManagedGroup> adGroups = await new UinformService().GetManagedGroups();
            adGroups.Sort((g1, g2) => (g1.DisplayName ?? g1.SamAccountName).CompareTo(g2.DisplayName ?? g2.SamAccountName));
            return Ok(adGroups);
        }

        // PUT: groups/5
        [HttpPut("{groupId}")]
        public async Task<IActionResult> UpdateGroup(int groupId, GroupAddEdit group)
        {
            if (groupId != group.GroupId)
            {
                BadRequest();
            }

            OuGroup? ouGroup = await _context.OuGroups.FindAsync(groupId);
            if (ouGroup == null)
            {
                return NotFound();
            }

            await _ouGroupService.UpdateRapsGroup(ouGroup, group.Name, group.Description);
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
            if (ouGroup != null)
            {
                return ValidationProblem("A group with this name is already managed by RAPS");
            }

            try
            {
                OuGroup newOuGroup = await _ouGroupService.CreateRapsGroup(group.Name, group.Description);
                return CreatedAtAction("CreateGroup", new { id = newOuGroup.OugroupId }, newOuGroup);
            }
            catch (Exception ex)
            {
                //Exception message could be indication user is trying to create a group that exists or is invalid.
                return ValidationProblem(ex.Message);
            }
        }

        [HttpPost("Managed")]
        public async Task<ActionResult> CreateAdGroup(GroupAddEdit group)
        {
            await new UinformService().CreateManagedGroup(group.Name, group.DisplayName ?? group.Name, group.Description ?? "");
            return NoContent();
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
            if (group == null)
            {
                return NotFound();
            }
            List<GroupMember> members = await _ouGroupService.GetAllMembers(groupId, group.Name);

            return members;
        }

        [HttpPost("{groupId}/Sync")]
        public async Task<ActionResult> SyncGroup(int groupId)
        {
            OuGroup? group = await _context.OuGroups.FindAsync(groupId);
            if (group == null)
            {
                return NotFound();
            }
            await _ouGroupService.Sync(groupId, group.Name);
            return Accepted();
        }
    }
}
