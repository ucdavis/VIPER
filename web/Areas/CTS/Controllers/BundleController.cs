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
    [Route("/api/cts/bundles/")]
    [Permission(Allow = "SVMSecure")]
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

        [HttpPost]
        public async Task<ActionResult<BundleDto>> AddBundle(BundleDto bundleDto)
        {
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
    }
}
