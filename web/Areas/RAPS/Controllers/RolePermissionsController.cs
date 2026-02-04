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
    public class RolePermissionsController : ApiController
    {
        private readonly RAPSContext _context;
        private readonly RAPSSecurityService _securityService;
        private readonly RAPSAuditService _auditService;
        private readonly RAPSCacheService _rapsCacheService;

        public RolePermissionsController(RAPSContext context, AAUDContext aaudContext)
        {
            _context = context;
            _securityService = new RAPSSecurityService(_context);
            _auditService = new RAPSAuditService(_context);
            _rapsCacheService = new RAPSCacheService(_context, aaudContext);
        }

        private ActionResult? CheckRoleAndPermissionParams(string instance, int? roleId, int? permissionId)
        {
            if (roleId == null && permissionId == null)
            {
                return BadRequest();
            }
            if (roleId != null)
            {
                TblRole? tblRole = _securityService.GetRoleInInstance(instance, (int)roleId);
                if (tblRole == null)
                {
                    return NotFound();
                }
            }
            else if (permissionId != null)
            {
                TblPermission? tblPermission = _securityService.GetPermissionInInstance(instance, (int)permissionId);
                if (tblPermission == null)
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
            if (errorResult != null)
            {
                return errorResult;
            }
            if (roleId != null)
            {
                TblRole? role = await _context.TblRoles.FindAsync(roleId);
                if (role == null)
                {
                    return NotFound();
                }
                if (!_securityService.IsAllowedTo("ViewRolePermissions", instance, role))
                {
                    return Forbid();
                }
            }
            else if (permissionId != null && !_securityService.IsAllowedTo("ViewAllRoles", instance))
            {
                return Forbid();
            }

            List<TblRolePermission> tblRolePermissions = await _context.TblRolePermissions
                    .Include(rp => rp.Role)
                    .Include(rp => rp.Permission)
                    .Where(rp => (roleId == null || rp.RoleId == roleId))
                    .Where(rp => (permissionId == null || rp.PermissionId == permissionId))
                    .OrderBy(rp => rp.Permission.Permission)
                    .ThenBy(rp => rp.Role.DisplayName ?? rp.Role.Role)
                    .ToListAsync();
            tblRolePermissions = tblRolePermissions
                .Where(rp => _securityService.RoleBelongsToInstance(instance, rp.Role))
                .ToList();
            return tblRolePermissions;
        }


        [HttpGet("Roles/{role1Id}/{role2Id}/PermissionComparison")]
        [Permission(Allow = "RAPS.Admin,RAPS.EditRoleMembership")]
        public async Task<ActionResult<RolePermissionComparison>> CompareRolePermissions(string instance, int role1Id, int role2Id)
        {
            TblRole? role1 = _securityService.GetRoleInInstance(instance, role1Id);
            TblRole? role2 = _securityService.GetRoleInInstance(instance, role2Id);
            if (role1 == null || role2 == null)
            {
                return NotFound();
            }

            if (!_securityService.IsAllowedTo("EditRoleMembership", instance) ||
                !_securityService.IsAllowedTo("ViewRolePermissions", instance, role1) ||
                !_securityService.IsAllowedTo("ViewRolePermissions", instance, role2))
            {
                return Forbid();
            }

            List<TblRolePermission> role1Permissions = await _context.TblRolePermissions
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.RoleId == role1Id)
                    .OrderBy(rp => rp.Permission.Permission)
                    .ToListAsync();
            List<TblRolePermission> role2Permissions = await _context.TblRolePermissions
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.RoleId == role2Id)
                    .OrderBy(rp => rp.Permission.Permission)
                    .ToListAsync();


            RolePermissionComparison result = new(role1Permissions, role2Permissions);
            return result;
        }

        // POST Roles/5/Permissions
        // POST Permissions/5/Roles
        [HttpPost("Roles/{roleId}/Permissions")]
        [HttpPost("Permissions/{permissionId}/Roles")]
        [Permission(Allow = "RAPS.Admin,RAPS.ManageAllPermissions")]
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

            using var transaction = await _context.Database.BeginTransactionAsync();
            TblRolePermission tblRolePermission = new();
            UpdateTblRolePermissionsWithDto(tblRolePermission, rolePermission);
            _context.TblRolePermissions.Add(tblRolePermission);
            await _context.SaveChangesAsync();
            _auditService.AuditRolePermissionChange(tblRolePermission, RAPSAuditService.AuditActionType.Create);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await ClearCacheForAllRoleMembers(rolePermission.RoleId);

            return CreatedAtAction("GetTblRole", new { roleId, permissionId, tblRolePermission.Access }, tblRolePermission);
        }

        // DELETE Roles/5/Permissions/123
        // DELETE Permissions/5/Roles/123
        [HttpDelete("Roles/{roleId}/Permissions/{permissionId}")]
        [HttpDelete("Permissions/{permissionId}/Roles/{roleId}")]
        [Permission(Allow = "RAPS.Admin,RAPS.ManageAllPermissions")]
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

            await ClearCacheForAllRoleMembers(roleId);

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

        private async Task ClearCacheForAllRoleMembers(int roleId)
        {
            var roleMembers = await _context.TblRoleMembers.Where(rm => rm.RoleId == roleId).ToListAsync();
            foreach (var member in roleMembers)
            {
                _rapsCacheService.ClearCachedRolesAndPermissionsForUser(member.MemberId);
            }
        }
    }
}
