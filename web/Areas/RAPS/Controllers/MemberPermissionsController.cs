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
        private RAPSSecurityService _securityService;
        private RAPSAuditService _auditService;

        public MemberPermissionsController(RAPSContext context)
        {
            _context = context;
            _securityService = new RAPSSecurityService(_context);
            _auditService = new RAPSAuditService(_context);
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
                        .ToListAsync();
            }
            else
            {
                return BadRequest("Member or Permission is required");
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
            if (tblMemberPermission == null)
            {
                return NotFound();
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
            TblMemberPermission tblMemberPermission = new TblMemberPermission() { MemberId = memberId, PermissionId = (int)permissionId };
            try
            {
                using var transaction = _context.Database.BeginTransaction();
                UpdateTblMemberPermission(tblMemberPermission, memberPermission);
                _context.TblMemberPermissions.Add(tblMemberPermission);
                _context.SaveChanges();
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

        private void UpdateTblMemberPermission(TblMemberPermission dbMemberPermission, MemberPermissionCreateUpdate inputMemberPermission)
        {
            dbMemberPermission.StartDate = inputMemberPermission.StartDate;
            dbMemberPermission.EndDate = inputMemberPermission.EndDate;
            dbMemberPermission.Access = inputMemberPermission.Access;
            dbMemberPermission.ModTime = DateTime.Now;
            dbMemberPermission.ModBy = UserHelper.GetCurrentUser()?.LoginId;
            if(dbMemberPermission.AddDate == null)
            {
                dbMemberPermission.AddDate = DateTime.Now;
            }
        }
    }
}
