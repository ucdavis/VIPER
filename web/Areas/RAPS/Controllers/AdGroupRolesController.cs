using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Viper.Areas.RAPS.Models;
using Viper.Areas.RAPS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;
using Web.Authorization;

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{instance=VIPER}/")]
    [ApiController]
    [Authorize(Roles = "VMDO SVM-IT", Policy = "2faAuthentication")]
    [Permission(Allow = "RAPS.Admin,RAPS.OUGroupsView")]
    public class AdGroupRolesController : ApiController
    {
        private readonly RAPSContext _context;
        private readonly RAPSAuditService _auditService;

        public AdGroupRolesController(RAPSContext context)
        {
            _context = context;
            _auditService = new RAPSAuditService(_context);
        }

        private bool GroupExists(int groupId)
        {
            var group = _context.OuGroups
                .Where(gr => gr.OugroupId == groupId)
                .FirstOrDefault();
            return group != null;
        }

        private bool GroupRoleExists(int groupId, int roleId)
        {
            var groupRole = _context.OuGroupRoles.Find(groupId, roleId);
            return groupRole != null;
        }

        //GET: Groups/5/Roles
        [HttpGet("Groups/{groupId}/Roles")]
        public async Task<ActionResult<IEnumerable<OuGroupRole>>> GetGroupRoles(int groupId)
        {
            if (_context.OuGroupRoles == null)
            {
                return NotFound();
            }
            if (!GroupExists(groupId))
            {
                return NotFound();
            }
            return await _context.OuGroupRoles
                .Include(gr => gr.Role)
                .Where(gr => gr.OugroupId == groupId)
                .OrderBy(gr => gr.Role.DisplayName ?? gr.Role.Role)
                .ToListAsync();
        }

        //POST: Group/5/Roles
        [HttpPost("Groups/{groupId}/Roles")]
        public async Task<ActionResult<IEnumerable<OuGroupRole>>> AddRoleToGroup(int groupId, int roleId)
        {
            if (_context.OuGroupRoles == null)
            {
                return NotFound();
            }
            if (!GroupExists(groupId))
            {
                return NotFound();
            }
            if(GroupRoleExists(groupId, roleId))
            {
                return BadRequest("Role is already linked to the group.");
            }

            using var transaction = _context.Database.BeginTransaction();
            OuGroupRole groupRole = new() { OugroupId = groupId, RoleId = roleId };
            _context.OuGroupRoles.Add(groupRole);
            await _context.SaveChangesAsync();
            _auditService.AuditOuGroupRoleChange(groupRole, RAPSAuditService.AuditActionType.Create);
            await _context.SaveChangesAsync();
            transaction.Commit();

            return CreatedAtAction("AddRoleToGroup", new { groupId, roleId }, groupRole);
        }

        //DELETE: Groups/5/Roles/8
        [HttpDelete("Groups/{groupId}/Roles/{roleId}")]
        public async Task<IActionResult> DeleteTblRoleMembers(int groupId, int roleId)
        {
            OuGroupRole? groupRole = _context.OuGroupRoles.Find(groupId, roleId);
            if (_context.OuGroupRoles == null || !GroupExists(groupId) || groupRole == null)
            {
                return NotFound();
            }

            //check that this isn't the group membership role
            OuGroupRole? groupMemberRole = _context.OuGroupRoles
                .FirstOrDefault(gr => gr.OugroupId == groupId && gr.IsGroupRole);
            if(groupMemberRole != null && groupMemberRole.RoleId == roleId)
            {
                return BadRequest("You cannot remove the group membership role.");
            }

            _context.OuGroupRoles.Remove(groupRole);
            _auditService.AuditOuGroupRoleChange(groupRole, RAPSAuditService.AuditActionType.Delete);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
