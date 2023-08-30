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
    [Permission(Allow = "RAPS.Admin,RAPS.ManageAllPermissions")]
    public class RolePermissionsController : ApiController
    {
        private readonly RAPSContext _context;
        private readonly RAPSSecurityService _securityService;
        private readonly RAPSAuditService _auditService;

        public RolePermissionsController(RAPSContext context)
        {
            _context = context;
            _securityService = new RAPSSecurityService(_context);
            _auditService = new RAPSAuditService(_context);
        }

        private ActionResult? CheckRoleAndPermissionParams(string instance, int? roleId, int? permissionId)
        {
            if(roleId == null && permissionId == null) 
            { 
                return BadRequest(); 
            }
            if(roleId != null)
            {
                TblRole? tblRole = _securityService.GetRoleInInstance(instance, (int)roleId);
                if (tblRole == null)
                {
                    return NotFound();
                }
            }
            else if(permissionId != null)
            {
                TblPermission? tblPermission = _securityService.GetPermissionInInstance(instance, (int)permissionId);
                if(tblPermission == null)
                {
                    return NotFound();
                }
            }

            return null;
        }

        // GET Roles/5/Permissions
        // GET Permissions/5/Roles
        [HttpGet("Roles/{roleId}/Permissions")]
        [HttpGet("Permissions/{permissionId}/Roles")]
        public async Task<ActionResult<List<TblRolePermission>>> GetTblRolePermissions(string instance, int? roleId, int? permissionId)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            ActionResult? errorResult = CheckRoleAndPermissionParams(instance, roleId, permissionId);
            if(errorResult != null)
            {
                return errorResult;
            }
            List<TblRolePermission> tblRolePermissions = await _context.TblRolePermissions
                    .Include(rp => rp.Role)
                    .Include(rp => rp.Permission)
                    .Where(rp => (roleId == null || rp.RoleId == roleId))
                    .Where(rp => (permissionId == null || rp.PermissionId == permissionId))
                    .OrderBy(rp => rp.Permission.Permission)
                    .ThenBy(rp => rp.Role.DisplayName ?? rp.Role.Role)
                    .ToListAsync();

            return tblRolePermissions;
        }

        // POST Roles/5/Permissions
        // POST Permissions/5/Roles
        [HttpPost("Roles/{roleId}/Permissions")]
        [HttpPost("Permissions/{permissionId}/Roles")]
        public async Task<ActionResult<TblRolePermission>> PostTblRolePermission(string instance, int? roleId, int? permissionId, RolePermissionCreateUpdate rolePermission)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            ActionResult? errorResult = CheckRoleAndPermissionParams(instance, roleId, permissionId);
            if (errorResult != null)
            {
                return errorResult;
            }
            if ((roleId != null && rolePermission.RoleId != roleId)
                || (permissionId != null && rolePermission.PermissionId != permissionId))
            {
                return BadRequest();
            }
            roleId = rolePermission.RoleId;
            permissionId = rolePermission.PermissionId;
            
            TblRolePermission? tblRolePermissionExists = await _context.TblRolePermissions.FindAsync(roleId, permissionId);
            if (tblRolePermissionExists != null)
            {
                return BadRequest("Role already contains permission");
            }

            using var transaction = _context.Database.BeginTransaction();
            TblRolePermission tblRolePermission = new();
            UpdateTblRolePermissionsWithDto(tblRolePermission, rolePermission);
            _context.TblRolePermissions.Add(tblRolePermission);
            _context.SaveChanges();
            _auditService.AuditRolePermissionChange(tblRolePermission, RAPSAuditService.AuditActionType.Create);
            _context.SaveChanges();
            transaction.Commit();

            return CreatedAtAction("GetTblRole", new { roleId, permissionId, tblRolePermission.Access }, tblRolePermission);
        }

        // DELETE Roles/5/Permissions/123
        // DELETE Permissions/5/Roles/123
        [HttpDelete("Roles/{roleId}/Permissions/{permissionId}")]
        [HttpDelete("Permissions/{permissionId}/Roles/{roleId}")]
        public async Task<ActionResult<TblPermission>> DeleteTblRolePermission(string instance, int roleId, int permissionId)
        {
            if (_context.TblRolePermissions == null)
            {
                return NotFound();
            }
            ActionResult? errorResult = CheckRoleAndPermissionParams(instance, roleId, permissionId);
            if (errorResult != null)
            {
                return errorResult;
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
            tblRolePermission.RoleId = rolePermissionCreateUpdate.RoleId;
            tblRolePermission.PermissionId = rolePermissionCreateUpdate.PermissionId;
            tblRolePermission.Access = rolePermissionCreateUpdate.Access;
            tblRolePermission.ModTime = DateTime.Now;
            tblRolePermission.ModBy = new UserHelper().GetCurrentUser()?.LoginId;
        }
    }
}
