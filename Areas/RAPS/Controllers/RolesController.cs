using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using Viper.Areas.RAPS.Dtos;
using Viper.Areas.RAPS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{instance=VIPER}/[controller]")]
    [Authorize(Roles = "VMDO SVM-IT,RAPS Delegate Users", Policy = "2faAuthentication")]
    public class RolesController : ApiController
    {
        private readonly RAPSContext _context;
        private RAPSSecurityService _securityService;

        private static Expression<Func<TblRole, bool>> FilterToInstance(string instance)
        {
            return r =>
                instance.ToUpper() == "VIPER"
                ? !r.Role.ToUpper().StartsWith("VMACS.") && !r.Role.ToUpper().StartsWith("VIPERFORMS")
                : r.Role.StartsWith(instance);
        }

        public RolesController(RAPSContext context)
        {
            _context = context;
            _securityService = new RAPSSecurityService(_context);
        }

        // GET: Roles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TblRole>>> GetTblRoles(string instance, int? Application)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }

            if(_securityService.IsAllowedTo("ViewAllRoles", instance))
            {
                return await _context.TblRoles
                    .Include(r => r.TblRoleMembers.Where(rm => rm.ViewName == null))
                    .Where((r => Application == null || r.Application == Application))
                    .Where(FilterToInstance(instance))
                    .OrderByDescending(r => r.Application)
                    .ThenBy(r => r.DisplayName == null ? r.Role : r.DisplayName)
                    .ToListAsync();
            }
            else
            {
                List<int> controlledRoleIds = _securityService.GetControlledRoleIds(UserHelper.GetCurrentUser()?.MothraId);
                List<TblRole> List = await _context.TblRoles
                    .Include(r => r.TblRoleMembers.Where(rm => rm.ViewName == null))
                    .Where(r => r.Application == 0)
                    .Where(r => controlledRoleIds.Contains(r.RoleId))
                    .Where(FilterToInstance(instance))
                    .OrderBy(r => r.DisplayName == null ? r.Role : r.DisplayName)
                    .ToListAsync();

                return List;
            }
        }

        // GET: Roles/5
        [HttpGet("{roleId}")]
        public async Task<ActionResult<TblRole>> GetTblRole(string instance, int roleId)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            var tblRole = await _context.TblRoles.FindAsync(roleId);

            if (tblRole == null)
            {
                return NotFound();
            }

            return tblRole;
        }

        // PUT: Roles/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{roleId}")]
        public async Task<IActionResult> PutTblRole(string instance, int roleId, TblRole tblRole)
        {
            if (roleId != tblRole.RoleId)
            {
                return BadRequest();
            }

            _context.Entry(tblRole).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TblRoleExists(roleId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: Roles
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TblRole>> PostTblRole(string instance, TblRole tblRole)
        {
            if (_context.TblRoles == null)
            {
                return Problem("Entity set 'RAPSContext.TblRoles'  is null.");
            }
            _context.TblRoles.Add(tblRole);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTblRole", new { id = tblRole.RoleId }, tblRole);
        }

        // DELETE: Roles/5
        [HttpDelete("{roleId}")]
        public async Task<IActionResult> DeleteTblRole(string instance, int roleId)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            var tblRole = await _context.TblRoles.FindAsync(roleId);
            if (tblRole == null)
            {
                return NotFound();
            }

            _context.TblRoles.Remove(tblRole);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: Roles/5/Members
        [HttpGet("{roleId}/Members")]
        public async Task<ActionResult<IEnumerable<TblRoleMember>>> GetTblRoleMembers(string instance, int roleId)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            var tblRole = await _context.TblRoles.FindAsync(roleId);

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

        //POST: Roles/5/Members/12345678
        [HttpPost("{roleId}/Members/{memberId}")]
        public async Task<ActionResult<IEnumerable<TblRoleMember>>> PostTblRoleMembers(int roleId, String memberId, RoleMemberCreateUpdate RoleMemberCreateUpdate)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            var tblRole = await _context.TblRoles.FindAsync(roleId);

            if (tblRole == null)
            {
                return NotFound();
            }

            var tblRoleMemberExists = await _context.TblRoleMembers.FindAsync(roleId, memberId);
            if(tblRoleMemberExists != null)
            {
                //TODO: Duplicate record error response
                return BadRequest();
            }

            TblRoleMember tblRoleMember = new TblRoleMember();
            tblRoleMember.RoleId = roleId;
            tblRoleMember.MemberId = memberId;
            tblRoleMember.StartDate = RoleMemberCreateUpdate.StartDate == null ? null : RoleMemberCreateUpdate.StartDate.Value.ToDateTime(new TimeOnly(0, 0, 0));
            tblRoleMember.EndDate = RoleMemberCreateUpdate.EndDate == null ? null : RoleMemberCreateUpdate.EndDate.Value.ToDateTime(new TimeOnly(0, 0, 0));
            _context.TblRoleMembers.Add(tblRoleMember);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTblRole", new { roleId = tblRoleMember.RoleId, memberId=tblRoleMember.MemberId }, tblRoleMember);
        }

        //PUT: Roles/5/Members/12345678
        [HttpPut("{roleId}/Members/{memberId}")]
        public async Task<ActionResult<IEnumerable<TblRoleMember>>> PutTblRoleMembers(int roleId, String memberId, RoleMemberCreateUpdate RoleMemberCreateUpdate)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            var tblRole = await _context.TblRoles.FindAsync(roleId);

            if (tblRole == null)
            {
                return NotFound();
            }

            var tblRoleMember= await _context.TblRoleMembers.FindAsync(roleId, memberId);
            if (tblRoleMember == null)
            {
                return NotFound();
            }

            tblRoleMember.StartDate = RoleMemberCreateUpdate.StartDate == null ? null : RoleMemberCreateUpdate.StartDate.Value.ToDateTime(new TimeOnly(0, 0, 0));
            tblRoleMember.EndDate = RoleMemberCreateUpdate.EndDate == null ? null : RoleMemberCreateUpdate.EndDate.Value.ToDateTime(new TimeOnly(0, 0, 0));
            _context.TblRoleMembers.Update(tblRoleMember);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        //DELETE: Roles/5/Members/12345678
        [HttpDelete("{roleId}/Members/{memberId}")]
        public async Task<IActionResult> DeleteTblRoleMembers(int roleId, String memberId, string? comment)
        {
            if (_context.TblRoleMembers == null)
            {
                return NotFound();
            }
            var tblRoleMember = await _context.TblRoleMembers.FindAsync(roleId, memberId);
            if (tblRoleMember == null)
            {
                return NotFound();
            }

            _context.TblRoleMembers.Remove(tblRoleMember);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TblRoleExists(int roleId)
        {
            return (_context.TblRoles?.Any(e => e.RoleId == roleId)).GetValueOrDefault();
        }
    }
}
