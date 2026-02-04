using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CTS.Models;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/api/cts/roles")]
    [Permission(Allow = "SVMSecure.CTS")]
    public class RoleController : ApiController
    {
        private readonly VIPERContext context;
        private readonly IMapper mapper;

        public RoleController(VIPERContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        public async Task<ActionResult<List<RoleDto>>> GetRoles(int? bundleId = null)
        {
            var rolesQuery = context.Roles.AsQueryable();
            if (bundleId != null)
            {
                rolesQuery = rolesQuery.Where(r => r.BundleRoles.Any(br => br.BundleId == bundleId));
            }
            var roles = await rolesQuery.OrderBy(r => r.RoleId).ToListAsync();
            return mapper.Map<List<RoleDto>>(roles);
        }

        [HttpPost]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<RoleDto>> AddRole(RoleDto roleDto)
        {
            var checkName = await context.Roles.AnyAsync(r => r.Name == roleDto.Name);
            if (checkName)
            {
                return BadRequest("Role name must be unique.");
            }

            var role = mapper.Map<Role>(roleDto);
            context.Add(role);
            await context.SaveChangesAsync();

            return mapper.Map<RoleDto>(role);
        }

        [HttpPut("{roleId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<RoleDto>> UpdateRole(int roleId, RoleDto roleDto)
        {
            var role = await context.Roles.FindAsync(roleId);
            if (role == null)
            {
                return NotFound();
            }

            var checkName = await context.Roles.AnyAsync(r => r.Name == roleDto.Name && r.RoleId != roleId);
            if (checkName)
            {
                return BadRequest("Role name must be unique.");
            }

            role.Name = roleDto.Name;
            context.Update(role);
            await context.SaveChangesAsync();
            return mapper.Map<RoleDto>(role);
        }

        [HttpDelete("{roleId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<RoleDto>> DeleteRole(int roleId)
        {
            var role = await context.Roles.FindAsync(roleId);
            if (role == null)
            {
                return NotFound();
            }

            try
            {
                context.Entry(role).State = EntityState.Deleted;
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return BadRequest("Could not delete role. If this role has been added to a bundle, it cannot be deleted.");
            }

            return mapper.Map<RoleDto>(role);
        }
    }
}
