using System.Data;
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
    [Route("raps/{instance}/[controller]")]
    [Permission(Allow = "RAPS.Admin,RAPS.ViewAuditTrail")]
    public class AuditController : ApiController
    {
        private readonly RAPSContext _context;
        private readonly RAPSAuditService _auditService;

        public AuditController(RAPSContext context)
        {
            _context = context;
            _auditService = new(context);
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
            return await _auditService.GetAuditEntries(
                startDate: startDate, endDate: endDate, auditType: auditType,
                modBy: modBy, modifiedUser: modifiedUser, roleId: roleId,
                search: search, permissionId: permissionId
            );
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
