using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using Viper.Areas.RAPS.Services;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{Instance=VIPER}/[controller]")]
    [ApiController]
    [Authorize(Roles = "IT Leadership & Supervisors,ITS_Operations,ITS_Programmers,VMDO CATS-Programmers,VMDO CATS-Techs,VMDO SVM-IT", Policy = "2faAuthentication")]
    public class RolesController : ControllerBase
    {
        private readonly RAPSContext _context;
        private RAPSSecurityService _securityService;

        private static Expression<Func<TblRole, bool>> FilterToInstance(string Instance)
        {
            return r =>
                Instance == "VIPER"
                ? !r.Role.StartsWith("VMACS.") && !r.Role.StartsWith("VIPERForms")
                : r.Role.StartsWith(Instance);
        }

        public RolesController(RAPSContext context)
        {
            _context = context;
            _securityService = new RAPSSecurityService(_context);
        }

        // GET: Roles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TblRole>>> GetTblRoles(string Instance, int? Application)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }

            if(_securityService.IsAllowedTo("ViewAllRoles", Instance))
            {
                return await _context.TblRoles
                    .Include(r => r.TblRoleMembers.Where(rm => rm.ViewName == null))
                    .Where((r => Application == null || r.Application == Application))
                    .Where(FilterToInstance(Instance))
                    .OrderByDescending(r => r.Application)
                    .ThenBy(r => r.DisplayName == null ? r.Role : r.DisplayName)
                    .ToListAsync();
            }
            else
            {
                List<int> controlledRoleIds = _securityService.GetControlledRoleIds(UserHelper.GetCurrentUser().MothraId);
                List<TblRole> List = await _context.TblRoles
                    .Include(r => r.TblRoleMembers.Where(rm => rm.ViewName == null))
                    .Where(r => r.Application == 0)
                    .Where(r => controlledRoleIds.Contains(r.RoleId))
                    .Where(FilterToInstance(Instance))
                    .OrderBy(r => r.DisplayName == null ? r.Role : r.DisplayName)
                    .ToListAsync();

                return List;
            }
        }

        // GET: Roles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TblRole>> GetTblRole(int id)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            var tblRole = await _context.TblRoles.FindAsync(id);

            if (tblRole == null)
            {
                return NotFound();
            }

            return tblRole;
        }

        // PUT: Roles/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTblRole(int id, TblRole tblRole)
        {
            if (id != tblRole.RoleId)
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
                if (!TblRoleExists(id))
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
        public async Task<ActionResult<TblRole>> PostTblRole(TblRole tblRole)
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
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTblRole(int id)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            var tblRole = await _context.TblRoles.FindAsync(id);
            if (tblRole == null)
            {
                return NotFound();
            }

            _context.TblRoles.Remove(tblRole);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TblRoleExists(int id)
        {
            return (_context.TblRoles?.Any(e => e.RoleId == id)).GetValueOrDefault();
        }
    }
}
