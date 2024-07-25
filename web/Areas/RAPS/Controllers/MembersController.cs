using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.RAPS.Models;
using Viper.Areas.RAPS.Services;
using Viper.Classes.SQLContext;
using Web.Authorization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{Instance=VIPER}/[controller]")]
    [ApiController]
    public class MembersController : ControllerBase
    {
        private readonly RAPSContext _context;
        private readonly RAPSSecurityService _securityService;
        private readonly RAPSAuditService _auditService;

        public MembersController(RAPSContext context)
        {
            _context = context;
            _securityService = new RAPSSecurityService(_context);
            _auditService = new RAPSAuditService(_context);
        }
        // GET: <Members>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberSearchResult>>> Get(string search, string active = "active")
        {
            var members = await _context.VwAaudUser
                    .Include(u => u.TblRoleMembers)
                    .Include(u => u.TblMemberPermissions)
                    .Where(u => (u.DisplayFirstName + " " + u.DisplayLastName).Contains(search)
                        || (u.MailId != null && u.MailId.Contains(search))
                        || (u.LoginId != null && u.LoginId.Contains(search))
                        || (u.MothraId == search) 
                    )
                    .Where(u => active == "all" || (active == "recent" && u.MostRecentTerm != null && u.MostRecentTerm >= DateTime.Now.Year * 100) || u.Current)
                    .OrderBy(u => u.DisplayLastName)
                    .ThenBy(u => u.DisplayFirstName)
                    .ToListAsync();
            List<MemberSearchResult> results = new();
            members.ForEach(m =>
            {
                results.Add(new MemberSearchResult()
                {
                    MemberId = m.MothraId,
                    DisplayFirstName = m.DisplayFirstName,
                    DisplayLastName = m.DisplayLastName,
                    LoginId = m.LoginId,
                    MailId = m.MailId,
                    CountPermissions = m.TblMemberPermissions.Count,
                    CountRoles = m.TblRoleMembers.Count,
                    Current = m.Current
                });
            });
            return results;
        }

        // GET <Members>/12345678
        [HttpGet("{memberId}")]
        public async Task<ActionResult<MemberSearchResult>> Get(string memberId)
        {
            var member = await _context.VwAaudUser.FirstOrDefaultAsync(u => u.MothraId == memberId);
            if(member == null)
            {
                return NotFound();
            }
            return new MemberSearchResult()
            {
                MemberId = member.MothraId,
                DisplayFirstName = member.DisplayFirstName,
                DisplayLastName = member.DisplayLastName,
                LoginId = member.LoginId,
                MailId = member.MailId,
                CountPermissions = member.TblMemberPermissions.Count,
                CountRoles = member.TblRoleMembers.Count,
                Current = member.Current
            };
        }

        /// <summary>
        /// Get all permissions assigned to a user (either allowed or denied) based on either role membership or direct
        /// permission assignment.
        /// </summary>
        /// <param name="memberId"></param>
        /// <returns>A list of permission results, including a source param to determine how the permission was assigned</returns>
        [Permission(Allow = "RAPS.Admin,RAPS.RSOP")]
        [HttpGet("{memberId}/RSOP")]
        public async Task<ActionResult<List<PermissionResult>>> RSOP(string instance, string memberId)
        {
            if(!_securityService.IsAllowedTo("RSOP", instance))
            {
                return Forbid();
            }

            var permsViaRoles = await (
                from permission in _context.TblPermissions
                join rolePermissions in _context.TblRolePermissions
                    on permission.PermissionId equals rolePermissions.PermissionId
                join memberRole in _context.TblRoleMembers
                    on rolePermissions.RoleId equals memberRole.RoleId
                join role in _context.TblRoles
                    on memberRole.RoleId equals role.RoleId
                where memberRole.MemberId == memberId
                && (memberRole.StartDate == null || memberRole.StartDate <= DateTime.Today)
                && (memberRole.EndDate == null || memberRole.EndDate >= DateTime.Today)
                select new
                {
                    permission.PermissionId,
                    permission.Permission,
                    rolePermissions.Access,
                    role.Role
                }).ToListAsync();

            var permsAssigned = await (from permission in _context.TblPermissions
                join memberPermissions in _context.TblMemberPermissions
                    on permission.PermissionId equals memberPermissions.PermissionId
                where memberPermissions.MemberId == memberId
                && (memberPermissions.StartDate == null || memberPermissions.StartDate <= DateTime.Today)
                && (memberPermissions.EndDate == null || memberPermissions.EndDate >= DateTime.Today)
                select new {
                    permission.PermissionId,
                    permission.Permission,
                    memberPermissions.Access
                }).ToListAsync();

            Dictionary<int, PermissionResult> permissions = new();
            //add permissions that assigned via roles (could be deny or allow)
            foreach(var p in permsViaRoles)
            {
                if (permissions.TryGetValue(p.PermissionId, out PermissionResult? value)) {
                    var existingPerm = value;
                    //record deny if this role is denying access
                    if (existingPerm.Access && p.Access == 0)
                    {
                        existingPerm.Access = false;
                        existingPerm.Source = p.Role;
                    }
                    //if access matches, add role to source
                    else
                    {
                        existingPerm.Source += "," + p.Role;
                    }
                }
                else
                {
                    permissions[p.PermissionId] = new PermissionResult() { PermissionId = p.PermissionId, PermissionName = p.Permission, Source = p.Role, Access = p.Access == 1 };
                }
            };

            //add permissions assigned manually (could be deny or allow)
            foreach (var p in permsAssigned)
            {
                if (permissions.TryGetValue(p.PermissionId, out PermissionResult? value))
                {
                    var existingPerm = value;
                    //record deny if permission assignment is denying access
                    if (existingPerm.Access && p.Access == 0)
                    {
                        existingPerm.Access = false;
                        existingPerm.Source = "Member Permission";
                    }
                    //if access matches, add member permission to source
                    else
                    {
                        existingPerm.Source += ",Member Permission";
                    }
                }
                else
                {
                    permissions[p.PermissionId] = new PermissionResult() { PermissionId = p.PermissionId, PermissionName = p.Permission, Source = "Member Permission", Access = p.Access == 1};
                }
            };

            return permissions.Values.OrderBy(p => p.PermissionName)
                .Where(p => _securityService.PermissionBelongsToInstance(instance, p.PermissionName))
                .ToList();
        }

        [Permission(Allow = "RAPS.Admin,RAPS.Clone")]
        [HttpGet("{sourceMemberId}/CloneTo/{targetMemberId}")]
        public async Task<ActionResult<MemberCloneObjects>> GetDifference(string instance, string sourceMemberId, string targetMemberId)
        {
            if (!_securityService.IsAllowedTo("Clone", instance))
            {
                return Forbid();
            }
            return await new CloneService(_context).GetUserComparison(instance, sourceMemberId, targetMemberId);
        }

        [Permission(Allow = "RAPS.Admin,RAPS.Clone")]
        [HttpPost("{sourceMemberId}/CloneTo/{targetMemberId}")]
        public async Task<ActionResult> Clone(string instance, string sourceMemberId, string targetMemberId, CloneConfirm objectsToClone)
        {
            if (!_securityService.IsAllowedTo("Clone", instance))
            {
                return Forbid();
            }
            if(objectsToClone.RoleIds.Count == 0 && objectsToClone.PermissionIds.Count == 0)
            {
                return ValidationProblem("At least one role or permission must be selected.");
            }
            await new CloneService(_context).Clone(instance, sourceMemberId, targetMemberId, objectsToClone);
            return NoContent();
        }

        [Permission(Allow = "RAPS.Admin,RAPS.EditRoleMembership")]
        [HttpGet("{memberId}/History")]
        public async Task<ActionResult<List<AuditLog>>> GetHistory(string instance, string memberId, DateOnly startDate)
        {
            if(!_securityService.IsAllowedTo("ViewHistory", instance))
            {
                return Forbid();
            }
            List<AuditLog> audit = await _auditService.GetMemberRolesAndPermissionHistory(instance: instance, memberId: memberId, startDate: startDate);
            return audit;
        }

        [Permission(Allow = "RAPS.Admin,RAPS.EditRoleMembership")]
        [HttpGet("{memberId}/History/Dates")]
        public async Task<ActionResult<List<DateOnly>>> GetHistoryDates(string instance, string memberId)
        {
            if (!_securityService.IsAllowedTo("ViewHistory", instance))
            {
                return Forbid();
            }
            List<AuditLog> audit = await _auditService.GetAuditEntries(instance: instance, modifiedUser: memberId);
            return audit
                .Select(a => DateOnly.FromDateTime(a.ModTime))
                .Distinct()
                .ToList();
        }

        [Permission(Allow = "RAPS.Admin,RAPS.EditRoleMembership")]
        [HttpPost("{memberId}/History")]
        public async Task<ActionResult<List<AuditLog>>> RevertFrom(string instance, string memberId, DateOnly? startDate = null, DateOnly? endDate = null)
        {
            if (!_securityService.IsAllowedTo("ViewHistory", instance)
                && !_securityService.IsAllowedTo("EditRoleMembership", instance))
            {
                return Forbid();
            }
            List<AuditLog> audit = await _auditService.GetAuditEntries(instance: instance, modifiedUser: memberId, startDate: startDate, endDate: endDate);
            return audit;
        }
    }
}
