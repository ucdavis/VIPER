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
    [Route("raps/{instance}/[controller]")]
    [Authorize(Roles = "VMDO SVM-IT,RAPS Delegate Users", Policy = "2faAuthentication")]
    public class RolesController : ApiController
    {
        private readonly RAPSContext _context;
        private RAPSSecurityService _securityService;
        private RAPSAuditService _auditService;

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
            _auditService = new RAPSAuditService(_context);
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
        public async Task<IActionResult> PutTblRole(string instance, int roleId, RoleCreateUpdate role)
        {
            TblRole tblRole = CreateTblRoleFromDTO(role);
            if (roleId != tblRole.RoleId)
            {
                return BadRequest();
            }

            _context.Entry(tblRole).State = EntityState.Modified;
            _auditService.AuditRoleChange(tblRole, RAPSAuditService.AuditActionType.Update);

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
        public async Task<ActionResult<TblRole>> PostTblRole(string instance, RoleCreateUpdate role)
        {
            if (_context.TblRoles == null)
            {
                return Problem("Entity set 'RAPSContext.TblRoles'  is null.");
            }

            using var transaction = _context.Database.BeginTransaction();
            TblRole tblRole = CreateTblRoleFromDTO(role);
            _context.TblRoles.Add(tblRole);
            await _context.SaveChangesAsync();
            _auditService.AuditRoleChange(tblRole, RAPSAuditService.AuditActionType.Create);
            await _context.SaveChangesAsync();
            transaction.Commit();

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
            _auditService.AuditRoleChange(tblRole, RAPSAuditService.AuditActionType.Delete);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private TblRole CreateTblRoleFromDTO(RoleCreateUpdate role)
        {
            var tblRole = new TblRole() { Role = role.Role, Description = role.Description, ViewName = role.ViewName, Application = (byte)role.Application };
            if (role.RoleId != null && role.RoleId > 0)
            {
                tblRole.RoleId = (int)role.RoleId;
            }
            return tblRole;
        }

        private bool TblRoleExists(int roleId)
        {
            return (_context.TblRoles?.Any(e => e.RoleId == roleId)).GetValueOrDefault();
        }
    }
}
