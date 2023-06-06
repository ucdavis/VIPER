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

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{instance=VIPER}/")]
    [ApiController]
    [Authorize(Roles = "VMDO SVM-IT", Policy = "2faAuthentication")]
    [Permission(Allow = "RAPS.Admin,RAPS.ManageAllPermissions")]
    public class RolePermissionsController : ApiController
    {
        private readonly RAPSContext _context;
        private RAPSSecurityService _securityService;
        private RAPSAuditService _auditService;

        public RolePermissionsController(RAPSContext context)
        {
            _context = context;
            _securityService = new RAPSSecurityService(_context);
            _auditService = new RAPSAuditService(_context);
        }

        
        // GET Roles/5/Permissions
        [HttpGet("Roles/{roleId}/Permissions")]
        public async Task<ActionResult<List<TblRolePermission>>> GetTblRolePermissions(string instance, int roleId)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            TblRole? tblRole = GetRoleInInstance(instance, roleId);
            if (tblRole == null)
            {
                return NotFound();
            }

            List<TblRolePermission> tblRolePermissions = await _context.TblRolePermissions
                    .Include(rp => rp.Role)
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.RoleId == roleId)
                    .OrderBy(rp => rp.Permission.Permission)
                    .ToListAsync();

            return tblRolePermissions;
        }

        // POST Roles/5/Permissions
        [HttpPost("Roles/{roleId}/Permissions")]
        public async Task<ActionResult<TblRolePermission>> PostTblRolePermission(string instance, int roleId, RolePermissionCreateUpdate rolePermission)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            TblRole? tblRole = GetRoleInInstance(instance, roleId);
            if (tblRole == null)
            {
                return NotFound();
            }

            TblRolePermission? tblRolePermissionExists = await _context.TblRolePermissions.FindAsync(roleId, rolePermission.PermissionId);
            if (tblRolePermissionExists != null)
            {
                return BadRequest("Role already contains permission");
            }

            using var transaction = _context.Database.BeginTransaction();
            TblRolePermission tblRolePermission = new TblRolePermission() { RoleId = roleId, PermissionId = rolePermission.PermissionId };
            UpdateTblRolePermissionsWithDto(tblRolePermission, rolePermission);
            _context.TblRolePermissions.Add(tblRolePermission);
            _context.SaveChanges();
            _auditService.AuditRolePermissionChange(tblRolePermission, RAPSAuditService.AuditActionType.Create);
            _context.SaveChanges();
            transaction.Commit();

            return CreatedAtAction("GetTblRole", new { roleId = tblRolePermission.RoleId, permissionId = tblRolePermission.PermissionId}, tblRolePermission);
        }

        // PUT Roles/5/Permissions/123
        [HttpPut("Roles/{roleId}/Permissions/{permissionId}")]
        public async Task<ActionResult<TblRolePermission>> PutTblRolePermission(string instance, int roleId, int permissionId, RolePermissionCreateUpdate rolePermission)
        {
            if (_context.TblRolePermissions == null)
            {
                return NotFound();
            }
            TblRole? tblRole = GetRoleInInstance(instance, roleId);
            if (tblRole == null)
            {
                return NotFound();
            }

            TblRolePermission? tblRolePermission = await _context.TblRolePermissions.FindAsync(roleId, permissionId);
            if (roleId != rolePermission.RoleId || permissionId != rolePermission.PermissionId || tblRolePermission == null)
            {
                return NotFound();
            }

            UpdateTblRolePermissionsWithDto(tblRolePermission, rolePermission);
            _context.TblRolePermissions.Update(tblRolePermission);
            _auditService.AuditRolePermissionChange(tblRolePermission, RAPSAuditService.AuditActionType.Update);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE Roles/5/Permissions/123
        [HttpDelete("Roles/{roleId}/Permissions/{permissionId}")]
        public async Task<ActionResult<TblPermission>> DeleteTblRolePermission(string instance, int roleId, int permissionId)
        {
            if (_context.TblRolePermissions == null)
            {
                return NotFound();
            }
            TblRole? tblRole = GetRoleInInstance(instance, roleId);
            if (tblRole == null)
            {
                return NotFound();
            }

            TblRolePermission? tblRolePermission = await _context.TblRolePermissions.FindAsync(roleId, permissionId);
            if (tblRolePermission == null)
            {
                return NotFound();
            }

            _context.TblRolePermissions.Remove(tblRolePermission);
            _auditService.AuditRolePermissionChange(tblRolePermission, RAPSAuditService.AuditActionType.Delete);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static void UpdateTblRolePermissionsWithDto(TblRolePermission tblRolePermission, RolePermissionCreateUpdate rolePermissionCreateUpdate)
        {
            tblRolePermission.Access = rolePermissionCreateUpdate.Access;
            tblRolePermission.ModTime = DateTime.Now;
            tblRolePermission.ModBy = UserHelper.GetCurrentUser()?.LoginId;
        }

        private TblRole? GetRoleInInstance(string instance, int roleId)
        {
            var tblRole = _context.TblRoles.Find(roleId);
            return tblRole != null && _securityService.RoleBelongsToInstance(instance, tblRole)
                ? tblRole
                : null;
        }
    }
}
