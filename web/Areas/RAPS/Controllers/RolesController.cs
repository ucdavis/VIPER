﻿using System.Data;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.RAPS.Models;
using Viper.Areas.RAPS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{instance}/[controller]")]
    [Authorize(Roles = "VMDO SVM-IT,RAPS Users", Policy = "2faAuthentication")]
    public class RolesController : ApiController
    {
        private readonly RAPSContext _context;
		public IRAPSSecurityServiceWrapper SecurityService;
        public IUserWrapper UserWrapper;
		public IRAPSAuditServiceWrapper AuditService;

        public RolesController(RAPSContext context)
        {
            _context = context;
            RAPSSecurityService rss = new(_context);
			SecurityService = new RAPSSecurityServiceWrapper(rss);
			UserWrapper = new UserWrapper();
            RAPSAuditService ras = new(_context);
			AuditService = new RAPSAuditServiceWrapper(ras);
        }

        // GET: Roles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TblRole>>> GetTblRoles(string instance, int? Application, bool? allInstances = false)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }

            allInstances ??= false;
            if(SecurityService.IsAllowedTo("ViewAllRoles", instance))
            {
                var q = _context.TblRoles
                    .Include(r => r.TblRoleMembers.Where(rm => rm.ViewName == null))
                    .Where((r => Application == null || r.Application == Application));
                if(!(bool)allInstances)
                {
                    q = q.Where(RAPSSecurityServiceWrapper.FilterRolesToInstance(instance));
                }
                List<TblRole> roles = await q
                    .OrderBy(r => r.Role.ToUpper().StartsWith("VIPERFORMS") ? 1 : 
                        r.Role.ToUpper().StartsWith("VMACS.VMTH") ? 2 :
                        r.Role.ToUpper().StartsWith("VMACS.VMLF") ? 3 :
                        r.Role.ToUpper().StartsWith("VMACS.UCVMCSD") ? 4 :
                        r.Role.ToUpper().StartsWith("VMACS.MGVP") ? 5 :
                        0)
                    .ThenByDescending(r => r.Application)
                    .ThenBy(r => r.DisplayName ?? r.Role)
                    .ToListAsync();
                return roles;
            }
            List<int> controlledRoleIds = SecurityService.GetControlledRoleIds(UserWrapper.GetCurrentUser()?.MothraId);
            return await _context.TblRoles
                .Include(r => r.TblRoleMembers.Where(rm => rm.ViewName == null))
                .Where(r => r.Application == 0)
                .Where(r => controlledRoleIds.Contains(r.RoleId))
                .Where(RAPSSecurityServiceWrapper.FilterRolesToInstance(instance))
                .OrderBy(r => r.DisplayName ?? r.Role)
                .ToListAsync();
        }

        // GET: Roles/5
        [HttpGet("{roleId}")]
        public async Task<ActionResult<TblRole>> GetTblRole(string instance, int roleId)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            var tblRole = await _context.TblRoles.FindAsync(roleId);

            if (tblRole == null || !SecurityService.RoleBelongsToInstance(instance, tblRole))
            {
                return NotFound();
            }

            return tblRole;
        }

        // PUT: Roles/5
        [HttpPut("{roleId}")]
        public async Task<IActionResult> PutTblRole(string instance, int roleId, RoleCreateUpdate role)
        {
            TblRole tblRole = CreateTblRoleFromDTO(role);
            if (roleId != tblRole.RoleId || !SecurityService.RoleBelongsToInstance(instance, tblRole))
            {
                return BadRequest();
            }

            _context.ChangeTracker.Clear();
			_context.SetModified(tblRole);
			AuditService.AuditRoleChange(tblRole, RAPSAuditService.AuditActionType.Update);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TblRoleExists(roleId))
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
        [HttpPost]
        public async Task<ActionResult<TblRole>> PostTblRole(string instance, RoleCreateUpdate role)
        {
			if (_context.TblRoles == null)
			{
				return Problem("Entity set 'RAPSContext.TblRoles'  is null.");
			}

            try
            {
			    using var transaction = _context.Database.BeginTransaction();
			    TblRole tblRole = CreateTblRoleFromDTO(role);
                if (!SecurityService.RoleBelongsToInstance(instance, tblRole))
                {
                    return ValidationProblem("Role name is invalid for this instance");
                }
			    _context.TblRoles.Add(tblRole);
			    await _context.SaveChangesAsync();
				AuditService.AuditRoleChange(tblRole, RAPSAuditService.AuditActionType.Create);
			    await _context.SaveChangesAsync();
			    transaction.Commit();

			    return CreatedAtAction("GetTblRole", new { id = tblRole.RoleId }, tblRole);
			}
			catch (DbUpdateConcurrencyException ex)
			{
				return Problem("The record was not updated because it was locked. " + ex.InnerException?.Message);
			}
			catch (Exception ex)
			{
				return Problem("There was a problem updating the database. " + ex.InnerException?.Message);
			}

		}

        // DELETE: Roles/5
        [HttpDelete("{roleId}")]
        public async Task<IActionResult> DeleteTblRole(string instance, int roleId)
        {
            if (_context.TblRoles == null)
            {
                return NotFound();
            }
            var tblRole = await _context.TblRoles.FindAsync(roleId);
            if (tblRole == null || !SecurityService.RoleBelongsToInstance(instance, tblRole))
            {
                return NotFound();
            }

            _context.TblRoles.Remove(tblRole);
			AuditService.AuditRoleChange(tblRole, RAPSAuditService.AuditActionType.Delete);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static TblRole CreateTblRoleFromDTO(RoleCreateUpdate role)
        {
            var tblRole = new TblRole() { Role = role.Role, Description = role.Description, ViewName = role.ViewName, Application = (byte)role.Application };
            if (role.RoleId != null && role.RoleId > 0)
            {
                tblRole.RoleId = (int)role.RoleId;
            }
            return tblRole;
        }

        private bool TblRoleExists(int roleId)
        {
            return (_context.TblRoles?.Any(e => e.RoleId == roleId)).GetValueOrDefault();
        }

		
	}
}
