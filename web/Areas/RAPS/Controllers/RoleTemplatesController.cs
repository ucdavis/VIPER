using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections.Features;
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
            var roleTemplate = await _context.RoleTemplates.FindAsync(roleTemplateId);

            if (roleTemplate == null || !_securityService.RoleTemplateBelongsToInstance(instance, roleTemplate))
            {
                return NotFound();
            }

            return roleTemplate;
        }

        // GET: RoleTemplates/5/Roles
        [HttpGet("{roleTemplateId}")]
        public async Task<ActionResult<List<RoleTemplateRole>>> GetRoleTemplateRoles(string instance, int roleTemplateId)
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

            List<RoleTemplateRole> roles = roleTemplate.RoleTemplateRoles.ToList();
            roles.Sort((r1, r2) => r1.Role.FriendlyName.CompareTo(r2.Role.FriendlyName));
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

        // POST: RoleTemplates
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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
