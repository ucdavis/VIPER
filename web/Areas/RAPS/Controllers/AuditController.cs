using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.RAPS.Models;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;
using Web.Authorization;

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{instance}/[controller]")]
    [Permission(Allow = "RAPS.Admin,RAPS.ViewAuditTrail")]
    public class AuditController : ApiController
    {
        private readonly RAPSContext _context;

        public AuditController(RAPSContext context)
        {
            _context = context;
        }

        // GET: api/Audit
        [HttpGet]
        public async Task<ActionResult<List<AuditLog>>> GetAudit(DateOnly? startDate = null, DateOnly? endDate = null, string? auditType = null,
            string? modBy = null, string? modifiedUser = null, int? roleId = null, string? search = null, int? permissionId = null)
        {
            if (_context.TblLogs == null)
            {
                return NotFound();
            }
            if (search != null)
            {
                search = search.ToLower();
            }
            return await (
                from log in _context.TblLogs
                join modByUser in _context.VwAaudUser
                    on log.ModBy equals modByUser.LoginId
                join tblPermission in _context.TblPermissions
                    on log.PermissionId equals tblPermission.PermissionId
                    into perms from permission in perms.DefaultIfEmpty()
                join tblRole in _context.TblRoles
                    on log.RoleId equals tblRole.RoleId
                    into roles from role in roles.DefaultIfEmpty()
                join vwAaudUser in _context.VwAaudUser
                    on log.MemberId equals vwAaudUser.MothraId
                    into members from member in members.DefaultIfEmpty()
                where (search == null 
                    || ((log.Detail ?? "").Contains(search))
                    || (log.Audit.Contains(search))
                    || ((log.Comment ?? "").Contains(search))
                    || (log.ModBy.Contains(search))
                    || ((modByUser.DisplayFullName ?? "").Contains(search))
                    || ((log.MemberId ?? "").Contains(search))
                    || ((member.DisplayFullName ?? "").Contains(search))
                    || ((role.Role ?? "").Contains(search))
                    || ((permission.Permission ?? "").Contains(search))                    
                    )
                    && (startDate == null || log.ModTime >= ((DateOnly)startDate).ToDateTime(new TimeOnly(0, 0, 0)))
                    && (endDate == null || log.ModTime <= ((DateOnly)endDate).ToDateTime(new TimeOnly(0, 0, 0)))
                    && (auditType == null || log.Audit == auditType)
                    && (modBy == null || log.ModBy == modBy)
                    && (modifiedUser == null || log.MemberId == modifiedUser)
                    && (roleId == null || log.RoleId == roleId)
                    && (permissionId == null || log.PermissionId == permissionId)
                select new AuditLog
                {
                    AuditRecordId = log.AuditRecordId,
                    MemberId = log.MemberId,
                    RoleId = log.RoleId,
                    PermissionId = log.PermissionId,
                    ModTime = log.ModTime,
                    ModBy = log.ModBy,
                    Audit = log.Audit,
                    Comment = log.Comment,
                    Detail = log.Detail,
                    MemberName = member.DisplayLastName != null ? member.DisplayLastName + ", " + member.DisplayFirstName : null,
                    ModByUserName = modByUser.DisplayLastName + ", " + modByUser.DisplayFirstName,
                    Permission = permission.Permission,
                    Role = (!string.IsNullOrEmpty(role.DisplayName) ? role.DisplayName : role.Role)
                } into record
                orderby record.ModTime descending
                select record)
                .Take(1000)
                .ToListAsync();
        }

        [HttpGet("ModifiedByUsers")]
        public async Task<ActionResult<List<ModifiedByUser>>> GetModifiedByUsers()
        {
            if (_context.TblLogs == null)
            {
                return NotFound();
            }
            return await GetAllModifiedByUsers();
        }

        [HttpGet("AuditTypes")]
        public async Task<ActionResult<List<string>>> GetAuditTypes()
        {
            if (_context.TblLogs == null)
            {
                return NotFound();
            }
            return await _context.TblLogs
                .Select(l => l.Audit)
                .Distinct()
                .ToListAsync();
        }

        private async Task<List<ModifiedByUser>> GetAllModifiedByUsers()
        {
            List<string> loginIds = await _context.TblLogs
                .Select(l => l.ModBy)
                .Distinct()
                .ToListAsync();
            List<VwAaudUser> users = await _context.VwAaudUser
                .Where(u => u.LoginId != null && loginIds.Contains(u.LoginId))
                .Distinct()
                .ToListAsync();
            List<ModifiedByUser> modByUsers = new();
            foreach (var user in users)
            {
                modByUsers.Add(new ModifiedByUser()
                {
                    LoginId = user.LoginId,
                    Name = user.DisplayLastName + ", " + user.DisplayFirstName
                });
            }
            modByUsers.Sort((u1, u2) => u1.Name.CompareTo(u2.Name));
            return modByUsers;
        }
    }
}
