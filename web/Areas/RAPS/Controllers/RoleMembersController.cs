using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Data;
using Viper.Areas.RAPS.Models;
using Viper.Areas.RAPS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{instance=VIPER}/")]
    [Authorize(Roles = "VMDO SVM-IT,RAPS Users", Policy = "2faAuthentication")]
    public class RoleMembersController : ApiController
    {
        private readonly RAPSContext _context;
        private readonly RAPSSecurityService _securityService;
        private readonly RAPSAuditService _auditService;

        public RoleMembersController(RAPSContext context)
        {
            _context = context;
            _securityService = new RAPSSecurityService(_context);
            _auditService = new RAPSAuditService(_context);
        }

        //GET: Roles/5/Members
        //GET: Members/12345678/Roles
        [HttpGet("Roles/{roleId}/Members")]
        [HttpGet("Members/{memberId}/Roles")]
        public async Task<ActionResult<IEnumerable<TblRoleMember>>> GetTblRoleMembers(string instance, int? roleId, string? memberId, int application=0)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            ActionResult? errorResult = CheckAccess(instance, roleId, memberId);
            if(errorResult != null)
            {
                return errorResult;
            }
            if(!_securityService.IsAllowedTo("ViewAllRoles", instance))
            {
                application = 0;
            }

            var roleMembers = _context.TblRoleMembers
                    .Include(rm => rm.Role)
                    .Include(rm => rm.AaudUser)
                    .Where(rm => rm.ViewName == null)
                    .Where(rm => application == (int)rm.Role.Application);
            if (roleId != null)
            {
                roleMembers = roleMembers
                    .Where(rm => rm.RoleId == roleId)
                    .OrderBy(rm => rm.AaudUser.DisplayLastName + ", " + rm.AaudUser.DisplayFirstName);
            }
            else
            {
                roleMembers = roleMembers
                    .Where(rm => rm.MemberId == memberId)
                    .OrderBy(rm => rm.Role.DisplayName ?? rm.Role.Role);
            }
            
            return (await roleMembers.ToListAsync())
                .FindAll(rm => _securityService.RoleBelongsToInstance(instance, rm.Role));
        }

        //POST: Roles/5/Members
        //POST: Members/12345678/Roles
        [HttpPost("Roles/{roleId}/Members")]
        [HttpPost("members/{memberId}/Roles")]
        public async Task<ActionResult<IEnumerable<TblRoleMember>>> PostTblRoleMembers(string instance, int? roleId, string? memberId, RoleMemberCreateUpdate roleMemberCreateUpdate)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            ActionResult? errorResult = CheckAccess(instance, roleId, memberId);
            if (errorResult != null)
            {
                return errorResult;
            }
            if((roleId != null && roleMemberCreateUpdate.RoleId != roleId)
                || (memberId != null && roleMemberCreateUpdate.MemberId != memberId))
            {
                return BadRequest();
            }

            roleId = roleMemberCreateUpdate.RoleId;
            memberId = roleMemberCreateUpdate.MemberId;            
            string? result = await new RoleMemberService(_context)
                .AddMemberToRole((int)roleId, memberId, roleMemberCreateUpdate.StartDate, roleMemberCreateUpdate.EndDate, roleMemberCreateUpdate.Comment);
            if(result != null)
            {
                return BadRequest(result);
            }

            TblRoleMember? tblRoleMember = await _context.TblRoleMembers.FindAsync(roleId, memberId);
            return CreatedAtAction("GetTblRole", new { roleId, memberId }, tblRoleMember);
        }

        //PUT: Roles/5/Members/12345678
        //PUT: Members/12345678/Roles
        [HttpPut("Roles/{roleId}/Members/{memberId}")]
        [HttpPut("Members/{memberId}/Roles/{roleId}")]
        public async Task<ActionResult<IEnumerable<TblRoleMember>>> PutTblRoleMembers(string instance, int roleId, string memberId, RoleMemberCreateUpdate roleMemberCreateUpdate)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            ActionResult? errorResult = CheckAccess(instance, roleId, memberId);
            if (errorResult != null)
            {
                return errorResult;
            }
            if (roleMemberCreateUpdate.RoleId != roleId ||roleMemberCreateUpdate.MemberId != memberId)
            {
                return BadRequest();
            }

            var tblRoleMember = await _context.TblRoleMembers.FindAsync(roleId, memberId);
            if (tblRoleMember == null)
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
        //DELETE: Members/5/Roles/12345678
        [HttpDelete("Roles/{roleId}/Members/{memberId}")]
        [HttpDelete("Members/{memberId}/Roles/{roleId}")]
        public async Task<IActionResult> DeleteTblRoleMembers(string instance, int roleId, string memberId, string? comment)
        {
            if (_context.TblRoleMembers == null)
            {
                return NotFound();
            }
            ActionResult? errorResult = CheckAccess(instance, roleId, memberId);
            if (errorResult != null)
            {
                return errorResult;
            }
            var tblRoleMember = await _context.TblRoleMembers.FindAsync(roleId, memberId);
            if (tblRoleMember == null)
            {
                return NotFound();
            }
            
            _context.TblRoleMembers.Remove(tblRoleMember);
            _auditService.AuditRoleMemberChange(tblRoleMember, RAPSAuditService.AuditActionType.Delete, comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static void UpdateTblRoleMemberWithDto(TblRoleMember tblRoleMember, RoleMemberCreateUpdate roleMemberCreateUpdate) 
        {
            tblRoleMember.StartDate = roleMemberCreateUpdate.StartDate?.ToDateTime(new TimeOnly(0, 0, 0));
            tblRoleMember.EndDate = roleMemberCreateUpdate.EndDate?.ToDateTime(new TimeOnly(0, 0, 0));
            tblRoleMember.ModTime = DateTime.Now;
            IUserHelper UserHelper = new UserHelper();
            tblRoleMember.ModBy = UserHelper.GetCurrentUser()?.LoginId;
        }

        /// <summary>
        /// Check access to view/edit role members based on the role id or member id
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="roleId"></param>
        /// <param name="memberId"></param>
        /// <returns></returns>
        private ActionResult? CheckAccess(string instance, int? roleId, string? memberId)
        {
            if (roleId == null && memberId == null)
            {
                return BadRequest();
            }
            if (roleId != null)
            {
                //for a role, check that the logged in user can edit role membership for this role.
                var tblRole = _securityService.GetRoleInInstance(instance, (int)roleId);
                if (tblRole == null)
                {
                    return NotFound();
                }
                if (!_securityService.IsAllowedTo("EditRoleMembership", instance, tblRole))
                {
                    return Forbid();
                }
            }
            //for a member, check that the user can edit role membership in this instance
            else if (!_securityService.IsAllowedTo("EditRoleMembership", instance))
            {
                return Forbid();
            }
            return null;
        }
    }
}
