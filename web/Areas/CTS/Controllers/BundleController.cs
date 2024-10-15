using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Viper.Areas.CTS.Models;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/api/cts/bundles/")]
    [Permission(Allow = "SVMSecure.CTS")]
    public class BundleController : ApiController
    {
        private readonly IMapper mapper;
        private readonly VIPERContext context;

        public BundleController(IMapper mapper, VIPERContext context)
        {
            this.mapper = mapper;
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<BundleDto>>> GetBundles(bool? clinical = null, bool? assessment = null, bool? milestone = null,
            int? serviceId = null, int? roleId = null)
        {
            var bundleQuery = context.Bundles
                .Include(b => b.BundleRoles)
                .ThenInclude(br => br.Role)
                .Include(b => b.BundleCompetencies)
                .AsQueryable();
            if (clinical != null)
            {
                bundleQuery = bundleQuery.Where(b => b.Clinical == clinical);
            }
            if (assessment != null)
            {
                bundleQuery = bundleQuery.Where(b => b.Assessment == assessment);
            }
            if (milestone != null)
            {
                bundleQuery = bundleQuery.Where(b => b.Milestone == milestone);
            }
            if (serviceId != null)
            {
                bundleQuery = bundleQuery.Where(b => b.BundleServices.Any(s => s.ServiceId == serviceId));
            }
            if (roleId != null)
            {
                bundleQuery = bundleQuery.Where(b => b.BundleRoles.Any(br => br.RoleId == roleId));
            }

            var bundles = await bundleQuery
                .OrderBy(b => b.Name)
                .ToListAsync();

            return mapper.Map<List<BundleDto>>(bundles);
        }

        [HttpGet("{bundleId}")]
        public async Task<ActionResult<BundleDto>> GetBundle(int bundleId)
        {
            var bundle = await context.Bundles
                .Include(b => b.BundleRoles)
                .ThenInclude(br => br.Role)
                .Where(b => b.BundleId == bundleId)
                .FirstOrDefaultAsync();
            if (bundle == null)
            {
                return NotFound();
            }
            return mapper.Map<BundleDto>(bundle);
        }

        [HttpPost]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<BundleDto>> AddBundle(BundleDto bundleDto)
        {
            if (string.IsNullOrEmpty(bundleDto.Name))
            {
                return BadRequest("Bundle Name is required.");
            }
            var nameCheck = await context.Bundles.Where(b => b.Name == bundleDto.Name).AnyAsync();
            if (nameCheck)
            {
                return BadRequest("Bundle Name must be unique.");
            }

            Bundle b = mapper.Map<Bundle>(bundleDto);
            context.Add(b);
            await context.SaveChangesAsync();

            return mapper.Map<BundleDto>(b);
        }

        [HttpPut("{bundleId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<BundleDto>> UpdateBundle(int bundleId, BundleDto bundleDto)
        {
            var exists = await context.Bundles.AnyAsync(b => b.BundleId == bundleId);
            if (!exists)
            {
                return NotFound();
            }
            if (string.IsNullOrEmpty(bundleDto.Name))
            {
                return BadRequest("Bundle Name is required.");
            }
            var nameCheck = await context.Bundles.Where(b => b.Name == bundleDto.Name && b.BundleId != bundleId).AnyAsync();
            if (nameCheck)
            {
                return BadRequest("Bundle Name must be unique.");
            }
            Bundle b = mapper.Map<Bundle>(bundleDto);
            context.Update(b);
            await context.SaveChangesAsync();
            return mapper.Map<BundleDto>(b);
        }

        [HttpDelete("{bundleId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<BundleDto>> DeleteBundle(int bundleId)
        {
            var bundle = await context.Bundles.FindAsync(bundleId);
            if (bundle == null)
            {
                return NotFound();
            }

            try
            {
                using var trans = context.Database.BeginTransaction();
                var bundleRoles = context.BundleRoles.Where(br => br.BundleId == bundleId);
                foreach (var role in bundleRoles)
                {
                    context.Remove(role);
                }
                context.Entry(bundle).State = EntityState.Deleted;
                await context.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch (Exception)
            {
                return BadRequest("Could not delete bundle. If this bundle has been used, it cannot be deleted.");
            }
            return mapper.Map<BundleDto>(bundle);
        }

        /* Bundle Roles */
        [HttpPut("{bundleId}/roles/")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<List<RoleDto>>> SetBundleRoles(int bundleId, List<int> bundleRoles)
        {
            using var trans = context.Database.BeginTransaction();
            var existing = await context.BundleRoles.Where(br => br.BundleId == bundleId).ToListAsync();
            foreach (var brId in bundleRoles)
            {
                if (!existing.Any(e => e.RoleId == brId))
                {
                    context.Add(new BundleRole()
                    {
                        BundleId = bundleId,
                        RoleId = brId
                    });
                }
            }

            foreach (var e in existing)
            {
                if (!bundleRoles.Any(brId => brId == e.RoleId))
                {
                    context.Entry(e).State = EntityState.Deleted;
                }
            }

            await context.SaveChangesAsync();
            await trans.CommitAsync();

            var roles = await context.BundleRoles.Where(br => br.BundleId == bundleId).Select(br => br.Role).ToListAsync();
            return mapper.Map<List<RoleDto>>(roles);
        }
    }
}
