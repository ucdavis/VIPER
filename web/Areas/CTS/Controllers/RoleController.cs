using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CTS.Models;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;
using Microsoft.Data.SqlClient;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/api/cts/roles")]
    [Permission(Allow = "SVMSecure.CTS")]
    public class RoleController : ApiController
    {
        private readonly VIPERContext context;

        public RoleController(VIPERContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<RoleDto>>> GetRoles(int? bundleId = null)
        {
            var rolesQuery = context.Roles.AsQueryable();
            if (bundleId != null)
            {
                rolesQuery = rolesQuery.Where(r => r.BundleRoles.Any(br => br.BundleId == bundleId));
            }
            var roles = await rolesQuery.OrderBy(r => r.RoleId).ToListAsync();
            return CtsMapper.ToRoleDtos(roles);
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

            var role = CtsMapper.ToRole(roleDto);
            context.Add(role);
            await context.SaveChangesAsync();

            return CtsMapper.ToRoleDto(role);
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
            return CtsMapper.ToRoleDto(role);
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
            catch (Exception ex) when (ex is DbUpdateException or SqlException or InvalidOperationException or OperationCanceledException)
            {
                return BadRequest("Could not delete role. If this role has been added to a bundle, it cannot be deleted.");
            }

            return CtsMapper.ToRoleDto(role);
        }
    }
}
