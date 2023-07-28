using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Template;
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
    [Authorize(Roles = "VMDO SVM-IT", Policy = "2faAuthentication")]
    [Permission(Allow = "RAPS.Admin")]
    public class RoleTemplatesController : ApiController
    {
        private readonly RAPSContext _context;
        private readonly RAPSSecurityService _securityService;

        public RoleTemplatesController(RAPSContext context)
        {
            _context = context;
            _securityService = new(context);
        }

        // GET: RoleTemplates
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleTemplate>>> GetRoleTemplates(string instance)
        {
          if (_context.RoleTemplates == null)
          {
              return NotFound();
          }
            return await _context.RoleTemplates
                .Where(RAPSSecurityService.FilterRoleTemplatesToInstance(instance))
                .OrderBy(rt => rt.TemplateName)
                .ToListAsync();
        }

        // GET: RoleTemplates/5
        [HttpGet("{roleTemplateId}")]
        public async Task<ActionResult<RoleTemplate>> GetRoleTemplate(string instance, int roleTemplateId)
        {
            if (_context.RoleTemplates == null)
            {
                return NotFound();
            }
            var roleTemplate = await GetRoleTemplateForInstance(instance, roleTemplateId);

            if (roleTemplate == null)
            {
                return NotFound();
            }

            return roleTemplate;
        }

        // GET: RoleTemplates/5
        [HttpGet("{roleTemplateId}/Apply/{memberId}")]
        public async Task<ActionResult<RoleTemplateApplyPreview>> PreviewRoleTemplateApply(string instance, int roleTemplateId, string memberId)
        {
            if (_context.RoleTemplates == null)
            {
                return NotFound();
            }
            RoleTemplate? roleTemplate = await GetRoleTemplateForInstance(instance, roleTemplateId);
            if (roleTemplate == null)
            {
                return NotFound();
            }

            var preview = await GetRoleTemplateApplyPreview(instance, roleTemplate, memberId);
            if(preview == null)
            {
                return NotFound();
            }
            return preview;
        }
        
        // Post: RoleTemplates/5/Apply/12345678
        [HttpPost("{roleTemplateId}/Apply/{memberId}")]
        public async Task<ActionResult<RoleTemplateApplyPreview>> RoleTemplateApply(string instance, int roleTemplateId, string memberId)
        {
            if (_context.RoleTemplates == null)
            {
                return NotFound();
            }

            RoleTemplate? roleTemplate = await GetRoleTemplateForInstance(instance, roleTemplateId);
            if (roleTemplate == null)
            {
                return NotFound();
            }

            var preview = await GetRoleTemplateApplyPreview(instance, roleTemplate, memberId);
            if(preview == null)
            {
                return NotFound();
            }

            if(memberId.StartsWith("loginid:"))
            {
                var userIdLookup = _context.VwAaudUser
                    .Where(u => u.LoginId == memberId.Substring(memberId.IndexOf(":") + 1))
                    .FirstOrDefault();
                if(userIdLookup == null)
                {
                    return NotFound();
                }
                memberId = userIdLookup.MothraId;
            }
            
            RoleMemberService roleMemberService = new(_context);
            foreach(RoleApplyPreview role in preview.Roles)
            {
                if(!role.UserHasRole)
                {
                    await roleMemberService.AddMemberToRole(role.RoleId, memberId, null, null, string.Format("Added via role template {0}", roleTemplate.TemplateName));
                }
            }

            return NoContent();
        }

        // Get a role template in the given instance, or null if it does not exist
        private async Task<RoleTemplate?> GetRoleTemplateForInstance(string instance, int roleTemplateId)
        {
            var roleTemplate = await _context.RoleTemplates
                .Include(rt => rt.RoleTemplateRoles)
                .ThenInclude(rtr => rtr.Role)
                .Where(rt => rt.RoleTemplateId == roleTemplateId)
                .Where(RAPSSecurityService.FilterRoleTemplatesToInstance(instance))
                .FirstOrDefaultAsync();
            return roleTemplate;
        }

        // Get a list of roles in the template and whether or not the given user already has them
        private async Task<RoleTemplateApplyPreview?> GetRoleTemplateApplyPreview(string instance, RoleTemplate roleTemplate, string memberId)
        {
            VwAaudUser? user = await _context.VwAaudUser
                .Where(u => memberId.StartsWith("loginid:")
                    ? u.LoginId == memberId.Substring(memberId.IndexOf(":") + 1)
                    : u.MothraId == memberId)
                .FirstOrDefaultAsync();

            List<TblRole> userRoles = new();
            List<RoleApplyPreview> rolesToApply = new();

            //if user is not found, return the object with placeholder text
            if (user == null)
            {
                return null;
            }
            
            userRoles = await _context.TblRoleMembers
                .Include(rm => rm.Role)
                .Where(rm => rm.MemberId == user.MothraId)
                .Select(rm => rm.Role)
                .ToListAsync();
            foreach (var role in roleTemplate.RoleTemplateRoles)
            {
                rolesToApply.Add(new RoleApplyPreview()
                {
                    RoleId = role.Role.RoleId,
                    RoleName = role.Role.FriendlyName,
                    Description = role.Role.Description,
                    UserHasRole = userRoles.Find(r => r.RoleId == role.RoleTemplateRoleRoleId) != null
                });
            }

            return new RoleTemplateApplyPreview()
            {
                DisplayName = user?.DisplayFullName ?? "User not found",
                MemberId = user?.MothraId ?? "",
                Roles = rolesToApply
            };
        }


        // GET: RoleTemplates/5/Roles
        [HttpGet("{roleTemplateId}/Roles")]
        public async Task<ActionResult<List<TblRole>>> GetRoleTemplateRoles(string instance, int roleTemplateId)
        {
            if (_context.RoleTemplates == null)
            {
                return NotFound();
            }
            var roleTemplate = await _context.RoleTemplates
                .Include(rt => rt.RoleTemplateRoles)
                .ThenInclude(rtr => rtr.Role)
                .Where(rt => rt.RoleTemplateId == roleTemplateId)
                .FirstOrDefaultAsync();

            if (roleTemplate == null || !_securityService.RoleTemplateBelongsToInstance(instance, roleTemplate))
            {
                return NotFound();
            }

            List<TblRole> roles = roleTemplate.RoleTemplateRoles.Select(rtr => rtr.Role).ToList();
            roles.Sort((r1, r2) => r1.FriendlyName.CompareTo(r2.FriendlyName));
            return roles;
        }

        // PUT: RoleTemplates/5
        [HttpPut("{roleTemplateId}")]
        public async Task<IActionResult> PutRoleTemplate(string instance, int roleTemplateId, RoleTemplateCreateUpdate roleTemplate)
        {
            RoleTemplate? rt = await _context.RoleTemplates.FindAsync(roleTemplateId);
            if(rt == null)
            {
                return NotFound();
            }
            if (roleTemplateId != roleTemplate.RoleTemplateId || !_securityService.RoleTemplateBelongsToInstance(instance, rt))
            {
                return BadRequest();
            }
            rt.TemplateName = roleTemplate.TemplateName;
            rt.Description = roleTemplate.Description;

            _context.Entry(rt).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoleTemplateExists(roleTemplateId))
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

        // PUT: RoleTemplates/5/Roles
        [HttpPut("{roleTemplateId}/Roles")]
        public async Task<IActionResult> PutRoleTemplateRoles(string instance, int roleTemplateId, List<int> roleIds)
        {
            List<RoleTemplateRole> templateRoles = await _context.RoleTemplateRoles
                .Where(rtr => rtr.RoleTemplateTemplateId == roleTemplateId)
                .ToListAsync();
            //remove deleted roles
            foreach (RoleTemplateRole templateRole in templateRoles)
            {
                if (!roleIds.Contains(templateRole.RoleTemplateRoleRoleId))
                {
                    _context.Remove(templateRole);
                }
                else
                {
                    //don't add this one
                    roleIds.Remove(templateRole.RoleTemplateRoleRoleId);
                }
            }

            //add new roles
            foreach (int id in roleIds)
            {
                _context.Add(new RoleTemplateRole()
                {
                    RoleTemplateRoleRoleId = id,
                    RoleTemplateTemplateId = roleTemplateId,
                    ModBy = UserHelper.GetCurrentUser()?.LoginId ?? "",
                    ModTime = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: RoleTemplates
        [HttpPost]
        public async Task<ActionResult<RoleTemplate>> PostRoleTemplate(string instance, RoleTemplateCreateUpdate roleTemplate)
        {
            if (_context.RoleTemplates == null)
            {
                return Problem("Entity set 'RAPSContext.RoleTemplates'  is null.");
            }
            RoleTemplate rt = new RoleTemplate()
            {
                TemplateName = roleTemplate.TemplateName,
                Description = roleTemplate.Description
            };

            if(!_securityService.RoleTemplateBelongsToInstance(instance, rt))
            {
                return BadRequest();
            }

            _context.RoleTemplates.Add(rt);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRoleTemplate", new { id = rt.RoleTemplateId }, rt);
        }

        // DELETE: RoleTemplates/5
        [HttpDelete("{roleTemplateId}")]
        public async Task<IActionResult> DeleteRoleTemplate(int roleTemplateId)
        {
            if (_context.RoleTemplates == null)
            {
                return NotFound();
            }
            var roleTemplate = await _context.RoleTemplates.FindAsync(roleTemplateId);
            if (roleTemplate == null)
            {
                return NotFound();
            }

            _context.RoleTemplates.Remove(roleTemplate);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RoleTemplateExists(int roleTemplateId)
        {
            return (_context.RoleTemplates?.Any(e => e.RoleTemplateId == roleTemplateId)).GetValueOrDefault();
        }
    }
}
