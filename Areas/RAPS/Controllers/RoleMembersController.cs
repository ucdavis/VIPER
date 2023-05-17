using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Data;
using Viper.Areas.RAPS.Dtos;
using Viper.Areas.RAPS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{instance=VIPER}/")]
    [Authorize(Roles = "VMDO SVM-IT,RAPS Delegate Users", Policy = "2faAuthentication")]
    public class RoleMembersController : ApiController
    {
        private readonly RAPSContext _context;
        private RAPSSecurityService _securityService;
        private RAPSAuditService _auditService;

        public RoleMembersController(RAPSContext context)
        {
            _context = context;
            _securityService = new RAPSSecurityService(_context);
            _auditService = new RAPSAuditService(_context);
        }

        // GET: Roles/5/Members
        [HttpGet("Roles/{roleId}/Members")]
        public async Task<ActionResult<IEnumerable<TblRoleMember>>> GetTblRoleMembers(string instance, int roleId)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            var tblRole = GetRoleInInstance(instance, roleId);
            if (tblRole == null)
            {
                return NotFound();
            }
            if (!_securityService.IsAllowedTo("EditRoleMembers", instance, tblRole))
            {
                return Forbid();
            }

            List<TblRoleMember> TblRoleMembers = await _context.TblRoleMembers
                    .Include(rm => rm.Role)
                    .Include(rm => rm.AaudUser)
                    .Where(rm => rm.RoleId == roleId)
                    .Where(rm => rm.ViewName == null)
                    .OrderBy(rm => rm.AaudUser.DisplayLastName + ", " + rm.AaudUser.DisplayFirstName)
                    .ToListAsync();

            return TblRoleMembers;
        }

        //POST: Roles/5/Members
        [HttpPost("Roles/{roleId}/Members")]
        public async Task<ActionResult<IEnumerable<TblRoleMember>>> PostTblRoleMembers(string instance, int roleId, RoleMemberCreateUpdate roleMemberCreateUpdate)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            var tblRole = GetRoleInInstance(instance, roleId);
            if (tblRole == null)
            {
                return NotFound();
            }

            if (!_securityService.IsAllowedTo("EditRoleMembers", instance, tblRole))
            {
                return Forbid();
            }

            var tblRoleMemberExists = await _context.TblRoleMembers.FindAsync(roleId, roleMemberCreateUpdate.MemberId);
            if (tblRoleMemberExists != null)
            {
                //TODO: Duplicate record error response
                return BadRequest();
            }

            using var transaction = _context.Database.BeginTransaction();
            TblRoleMember tblRoleMember = new TblRoleMember() { RoleId = roleId, MemberId = roleMemberCreateUpdate.MemberId };
            UpdateTblRoleMemberWithDto(tblRoleMember, roleMemberCreateUpdate);
            _context.TblRoleMembers.Add(tblRoleMember);
            _context.SaveChanges();
            _auditService.AuditRoleMemberChange(tblRoleMember, RAPSAuditService.AuditActionType.Create, roleMemberCreateUpdate.Comment);
            _context.SaveChanges();
            transaction.Commit();

            return CreatedAtAction("GetTblRole", new { roleId = tblRoleMember.RoleId, memberId = tblRoleMember.MemberId }, tblRoleMember);
        }

        //PUT: Roles/5/Members/12345678
        [HttpPut("Roles/{roleId}/Members/{memberId}")]
        public async Task<ActionResult<IEnumerable<TblRoleMember>>> PutTblRoleMembers(string instance, int roleId, string memberId, RoleMemberCreateUpdate roleMemberCreateUpdate)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            var tblRole = GetRoleInInstance(instance, roleId);
            if (tblRole == null)
            {
                return NotFound();
            }

            if (!_securityService.IsAllowedTo("EditRoleMembers", instance, tblRole))
            {
                return Forbid();
            }

            var tblRoleMember = await _context.TblRoleMembers.FindAsync(roleId, memberId);
            if (roleId != roleMemberCreateUpdate.RoleId || memberId != roleMemberCreateUpdate.MemberId || tblRoleMember == null)
            {
                return NotFound();
            }

            UpdateTblRoleMemberWithDto(tblRoleMember, roleMemberCreateUpdate);
            _context.TblRoleMembers.Update(tblRoleMember);
            _auditService.AuditRoleMemberChange(tblRoleMember, RAPSAuditService.AuditActionType.Update, roleMemberCreateUpdate.Comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        //DELETE: Roles/5/Members/12345678
        [HttpDelete("Roles/{roleId}/Members/{memberId}")]
        public async Task<IActionResult> DeleteTblRoleMembers(string instance, int roleId, string memberId, string? comment)
        {
            if (_context.TblRoleMembers == null)
            {
                return NotFound();
            }
            var tblRole = GetRoleInInstance(instance, roleId);
            if (tblRole == null)
            {
                return NotFound();
            }

            var tblRoleMember = await _context.TblRoleMembers.FindAsync(roleId, memberId);
            if (tblRoleMember == null)
            {
                return NotFound();
            }
            if (!_securityService.IsAllowedTo("EditRoleMembers", instance, tblRole))
            {
                return Forbid();
            }

            _context.TblRoleMembers.Remove(tblRoleMember);
            _auditService.AuditRoleMemberChange(tblRoleMember, RAPSAuditService.AuditActionType.Delete, comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private void UpdateTblRoleMemberWithDto(TblRoleMember tblRoleMember, RoleMemberCreateUpdate roleMemberCreateUpdate) 
        {
            tblRoleMember.StartDate = roleMemberCreateUpdate.StartDate == null ? null : roleMemberCreateUpdate.StartDate.Value.ToDateTime(new TimeOnly(0, 0, 0));
            tblRoleMember.EndDate = roleMemberCreateUpdate.EndDate == null ? null : roleMemberCreateUpdate.EndDate.Value.ToDateTime(new TimeOnly(0, 0, 0));
            tblRoleMember.ModTime = DateTime.Now;
            tblRoleMember.ModBy = UserHelper.GetCurrentUser()?.LoginId;
        }

        private TblRole? GetRoleInInstance(string instance, int roleId) {
            var tblRole = _context.TblRoles.Find(roleId);
            return tblRole != null && _securityService.RoleBelongsToInstance(instance, tblRole) 
                ? tblRole 
                : null;
        }
    }
}
