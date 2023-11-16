using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.RAPS.Models;
using Viper.Areas.RAPS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;
using Web.Authorization;

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{instance=VIPER}")]
    [Authorize(Roles = "VMDO SVM-IT", Policy = "2faAuthentication")]
    [Permission(Allow = "RAPS.Admin,RAPS.EditMemberPermissions")]
    public class MemberPermissionsController : ApiController
    {
        private readonly RAPSContext _context;
        private readonly RAPSSecurityService _securityService;
        private readonly RAPSAuditService _auditService;
        public IUserHelper UserHelper;

        public MemberPermissionsController(RAPSContext context)
        {
            _context = context;
            _securityService = new RAPSSecurityService(_context);
            _auditService = new RAPSAuditService(_context);
            UserHelper = new UserHelper();
        }

        // GET: Members/12345678/Permissions
        // GET: Permissions/5/Members
        [HttpGet("Members/{memberId}/Permissions")]
        [HttpGet("Permissions/{permissionId}/Members")]
        public async Task<ActionResult<IEnumerable<TblMemberPermission>>> GetTblMemberPermissions(string instance, string? memberId, int? permissionId)
        {
            if (_context.TblMemberPermissions == null)
            {
                return NotFound();
            }
            if (memberId != null)
            {
                return await _context.TblMemberPermissions
                    .Include(mp => mp.Permission)
                    .Where(mp => mp.MemberId == memberId)
                    .OrderBy(mp => mp.Permission.Permission)
                    .ToListAsync();
            }
            else if(permissionId != null)
            {
                TblPermission? permission = _securityService.GetPermissionInInstance(instance, (int)permissionId);
                return permission == null 
                    ? NotFound()
                    : await _context.TblMemberPermissions
                        .Include(mp => mp.Member)
                        .Where(mp => mp.PermissionId == permissionId)
                        .OrderBy(mp => mp.Member.DisplayLastName)
                        .ThenBy(mp => mp.Member.DisplayFirstName)
                        .ToListAsync();
            }
            else
            {
                return BadRequest("Member or Permission is required");
            }
        }

        // GET: Permissions/5/AllMembers
        [HttpGet("Permissions/{permissionId}/AllMembers")]
        public async Task<ActionResult<IEnumerable<MemberSearchResult>>> GetAllPermissionMembers(string instance, int? permissionId)
        {
            if (_context.TblMemberPermissions == null)
            {
                return NotFound();
            }
            if (permissionId != null)
            {
                TblPermission? permission = _securityService.GetPermissionInInstance(instance, (int)permissionId);
                if(permission == null)
                {
                    return NotFound();
                }

                //get everyone who currently has the permission. this takes into account deny permissions, deny roles to permission,
                //role member dates, and permission member dates.

                //basic queries with date checking - not checking access yet
                var membersWithRole = _context.TblRolePermissions
                    .Where(rp => rp.PermissionId == permissionId)
                    .Include(rp => rp.Role)
                    .ThenInclude(r => r.TblRoleMembers
                        .Where(rm => rm.StartDate == null || rm.StartDate <= DateTime.Now)
                        .Where(rm => rm.EndDate == null || rm.EndDate >= DateTime.Now));
                var membersWithPermission = _context.TblMemberPermissions
                    .Where(mp => mp.PermissionId == permissionId)
                    .Where(mp => mp.StartDate == null || mp.StartDate <= DateTime.Now)
                    .Where(mp => mp.EndDate == null || mp.EndDate >= DateTime.Now);

                //using the above queries, but checking access flag
                var membersWithRoleGrantingPermission = membersWithRole
                    .Where(rp => rp.Access == 1)
                    .SelectMany(rp => rp.Role.TblRoleMembers.Select(rm => rm.AaudUser));
                var membersWithRoleDenyingPermission = membersWithRole
                    .Where(rp => rp.Access == 0)
                    .SelectMany(rp => rp.Role.TblRoleMembers.Select(rm => rm.AaudUser));
                var membersGrantedPermission = membersWithPermission
                    .Where(mp => mp.Access == 1)
                    .Select(mp => mp.Member);
                var membersDeniedPermission = membersWithPermission
                    .Where(mp => mp.Access == 0)
                    .Select(mp => mp.Member);

                return await membersWithRoleGrantingPermission
                    .Union(membersGrantedPermission)
                    .Except(membersWithRoleDenyingPermission)
                    .Except(membersDeniedPermission)    
                    .Select(aaud => new MemberSearchResult()
                        {
                            MemberId = aaud.MothraId,
                            LoginId = aaud.LoginId,
                            MailId = aaud.MailId,
                            DisplayFirstName = aaud.DisplayFirstName,
                            DisplayLastName = aaud.DisplayLastName,
                            Current = aaud.Current
                        })
                    .OrderBy(result => result.DisplayLastName)
                    .ThenBy(result => result.DisplayFirstName) 
                    .ThenBy(result => result.MemberId)
                    .ToListAsync();
            }
            else
            {
                return BadRequest("Permission is required");
            }
        }

        // GET: Members/12345678/Permissions/5
        // GET: Permissions/5/Members/12345678
        [HttpGet("Members/{memberId}/Permissions/{permissionId}")]
        [HttpGet("Permissions/{permissionId}/Members/{memberId}")]
        public async Task<ActionResult<TblMemberPermission>> GetTblMemberPermission(string instance, string memberId, int permissionId)
        {
            if (_context.TblMemberPermissions == null)
            {
                return NotFound();
            }
            TblPermission? permission = _securityService.GetPermissionInInstance(instance, permissionId);
            if (permission == null)
            {
                return NotFound();
            }
            
            var tblMemberPermission = await _context.TblMemberPermissions.FindAsync(memberId, permissionId);
            if (tblMemberPermission == null)
            {
                return NotFound();
            }

            return tblMemberPermission;
        }

        // PUT: Members/12345678/Permissions/5
        // PUT: Permissions/5/Members/12345678
        [HttpPut("Members/{memberId}/Permissions/{permissionId}")]
        [HttpPut("Permissions/{permissionId}/Members/{memberId}")]
        public async Task<IActionResult> PutTblMemberPermission(string instance, string memberId, int permissionId, MemberPermissionCreateUpdate memberPermission)
        {
            if (memberId != memberPermission.MemberId || permissionId != memberPermission.PermissionId)
            {
                return BadRequest();
            }

            TblMemberPermission? tblMemberPermission = await _context.TblMemberPermissions.FindAsync(memberId, permissionId);
            TblPermission? tblPermission = await _context.TblPermissions.FindAsync(permissionId);
            if (tblMemberPermission == null || tblPermission == null)
            {
                return NotFound();
            }
            if (!_securityService.PermissionBelongsToInstance(instance, tblPermission.Permission))
            {
                return BadRequest();
            }
            UpdateTblMemberPermission(tblMemberPermission, memberPermission);
            //_context.Entry(tblMemberPermission).State = EntityState.Modified;
            
            try
            {
                _context.TblMemberPermissions.Update(tblMemberPermission);
                _auditService.AuditPermissionMemberChange(tblMemberPermission, RAPSAuditService.AuditActionType.Update);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TblMemberPermissionExists(tblMemberPermission.MemberId, tblMemberPermission.PermissionId))
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

        // POST: Members/12345678/Permissions
        // POST: Permissions/5/Members
        [HttpPost("Members/{memberId}/Permissions")]
        [HttpPost("Permissions/{permissionId}/Members")]
        public async Task<ActionResult<TblMemberPermission>> PostTblMemberPermission(string instance, string? memberId, int? permissionId, MemberPermissionCreateUpdate memberPermission)
        {
            if (_context.TblMemberPermissions == null)
            {
                return Problem("Entity set 'RAPSContext.TblMemberPermissions' is null.");
            }
            if((permissionId != null && permissionId != memberPermission.PermissionId) 
                || (memberId != null && memberId != memberPermission.MemberId))
            {
                return NotFound();
            }
            permissionId = memberPermission.PermissionId;
            memberId = memberPermission.MemberId;
            if (_securityService.GetPermissionInInstance(instance, (int)permissionId) == null)
            {
                return NotFound();
            }
            var tblPermissionMemberExists = await _context.TblMemberPermissions.FindAsync(memberId, permissionId);
            if (tblPermissionMemberExists != null)
            {
                return BadRequest("User is already a member of this permission");
            }
            TblMemberPermission tblMemberPermission = new() { MemberId = memberId, PermissionId = (int)permissionId };
            try
            {
                using var transaction = _context.Database.BeginTransaction();
                UpdateTblMemberPermission(tblMemberPermission, memberPermission);
                _context.TblMemberPermissions.Add(tblMemberPermission);
                _context.SaveChanges();
                if (memberPermission.Access == 0)
                {
                    //SaveChanges() changes this to 1 if it's set to 0?
                    tblMemberPermission.Access = 0;
                }
                _auditService.AuditPermissionMemberChange(tblMemberPermission, RAPSAuditService.AuditActionType.Create);
                _context.SaveChanges();
                transaction.Commit();
            }
            catch (DbUpdateException)
            {
                if (TblMemberPermissionExists(memberId, (int)permissionId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetTblMemberPermission", new { memberId, permissionId }, tblMemberPermission);
        }

        // DELETE: Members/12345678/Permissions/5
        // DELETE: Permissions/5/Members/12345678
        [HttpDelete("Members/{memberId}/Permissions/{permissionId}")]
        [HttpDelete("Permissions/{permissionId}/Members/{memberId}")]
        public async Task<IActionResult> DeleteTblMemberPermission(string instance, string memberId, int permissionId)
        {
            if (_context.TblMemberPermissions == null)
            {
                return NotFound();
            }
            if (_securityService.GetPermissionInInstance(instance, permissionId) == null)
            {
                return NotFound();
            }
            var tblMemberPermission = await _context.TblMemberPermissions.FindAsync(memberId, permissionId);
            if (tblMemberPermission == null)
            {
                return NotFound();
            }

            _context.TblMemberPermissions.Remove(tblMemberPermission);
            _auditService.AuditPermissionMemberChange(tblMemberPermission, RAPSAuditService.AuditActionType.Delete);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TblMemberPermissionExists(string memberId, int permissionId)
        {
            return (_context.TblMemberPermissions?.Any(e => e.PermissionId == permissionId && e.MemberId == memberId)).GetValueOrDefault();
        }

        private static void UpdateTblMemberPermission(TblMemberPermission dbMemberPermission, MemberPermissionCreateUpdate inputMemberPermission)
        {
            dbMemberPermission.StartDate = inputMemberPermission.StartDate;
            dbMemberPermission.EndDate = inputMemberPermission.EndDate;
            dbMemberPermission.Access = inputMemberPermission.Access;
            dbMemberPermission.ModTime = DateTime.Now;
            dbMemberPermission.ModBy = new UserHelper().GetCurrentUser()?.LoginId;
            if(dbMemberPermission.AddDate == null)
            {
                dbMemberPermission.AddDate = DateTime.Now;
            }
        }
    }
}
