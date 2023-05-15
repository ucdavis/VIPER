using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using Viper.Areas.RAPS.Dtos;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;
using Web.Authorization;

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{instance=VIPER}/[controller]")]
    [ApiController]
    [Authorize(Roles = "VMDO SVM-IT", Policy = "2faAuthentication")]
    public class PermissionsController : ControllerBase
    {
        private readonly RAPSContext _context;

        public PermissionsController(RAPSContext context)
        {
            _context = context;
        }

        private static Expression<Func<TblPermission, bool>> FilterToInstance(string instance)
        {
            return r =>
                instance.ToUpper() == "VIPER"
                ? !r.Permission.ToUpper().StartsWith("VMACS.") && !r.Permission.ToUpper().StartsWith("VIPERFORMS")
                : r.Permission.StartsWith(instance);
        }

        // GET: Permissions
        [HttpGet]
        [Permission(Allow = "RAPS.Admin,RAPS.ViewPermissions")]
        public async Task<ActionResult<IEnumerable<TblPermission>>> GetTblPermissions(string instance)
        {
            if (_context.TblPermissions == null)
            {
                return NotFound();
            }
            return await _context.TblPermissions
                .Include(p => p.TblMemberPermissions)
                .Where(FilterToInstance(instance))
                .OrderBy(p => p.Permission)
                .ToListAsync();
        }

        // GET: Permissions/5
        [HttpGet("{permissionId}")]
        [Permission(Allow = "RAPS.Admin,RAPS.ViewPermissions")]
        public async Task<ActionResult<TblPermission>> GetTblPermission(int permissionId)
        {
          if (_context.TblPermissions == null)
          {
              return NotFound();
          }
            var tblPermission = await _context.TblPermissions.FindAsync(permissionId);

            if (tblPermission == null)
            {
                return NotFound();
            }

            return tblPermission;
        }

        // PUT: Permissions/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{permissionId}")]
        [Permission(Allow = "RAPS.Admin,RAPS.ViewPermissions")]
        public async Task<IActionResult> PutTblPermission(int permissionId, PermissionCreateUpdate permission)
        {
            if (permissionId != permission.PermissionId)
            {
                return BadRequest();
            }
            
            TblPermission existingPermission = GetPermissionByName(permission.Permission);
            if (existingPermission != null && existingPermission.PermissionId != permissionId)
            {
                return ValidationProblem("Permission name must be unique");
            }
            else if(existingPermission != null)
            {
                _context.Entry(existingPermission).State = EntityState.Detached;
            }

            TblPermission tblPermission = new TblPermission();
            tblPermission.PermissionId = permission.PermissionId;
            tblPermission.Permission = permission.Permission;
            tblPermission.Description = permission.Description;
            _context.Entry(tblPermission).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TblPermissionExists(permissionId))
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

        // POST: Permissions
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Permission(Allow = "RAPS.Admin,RAPS.ViewPermissions")]
        public async Task<ActionResult<TblPermission>> PostTblPermission(TblPermission tblPermission)
        {
            if (_context.TblPermissions == null)
            {
                return Problem("Entity set 'RAPSContext.TblPermissions'  is null.");
            }

            TblPermission? existingPermission = GetPermissionByName(tblPermission.Permission);
            if (existingPermission != null) 
            {
                return ValidationProblem("Permission name must be unique");
            }

            _context.TblPermissions.Add(tblPermission);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTblPermission", new { id = tblPermission.PermissionId }, tblPermission);
        }

        // DELETE: Permissions/5
        [HttpDelete("{permissionId}")]
        [Permission(Allow = "RAPS.Admin,RAPS.ViewPermissions")]
        public async Task<IActionResult> DeleteTblPermission(int permissionId)
        {
            if (_context.TblPermissions == null)
            {
                return NotFound();
            }
            var tblPermission = await _context.TblPermissions.FindAsync(permissionId);
            if (tblPermission == null)
            {
                return NotFound();
            }

            _context.TblPermissions.Remove(tblPermission);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private TblPermission? GetPermissionByName(string name)
        {
            if (_context.TblPermissions == null) { return null; }
            return _context.TblPermissions.FirstOrDefault(p => p.Permission == name);
        }

        private bool TblPermissionExists(int permissionId)
        {
            return (_context.TblPermissions?.Any(e => e.PermissionId == permissionId)).GetValueOrDefault();
        }
    }
}
