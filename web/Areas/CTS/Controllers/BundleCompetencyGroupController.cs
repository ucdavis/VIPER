using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Viper.Areas.CTS.Models;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/api/cts/bundles/{bundleId}/groups")]
    [Permission(Allow = "SVMSecure")]
    public class BundleCompetencyGroupController : ApiController
    {
        private readonly IMapper mapper;
        private readonly VIPERContext context;
        public BundleCompetencyGroupController(IMapper mapper, VIPERContext context)
        {
            this.mapper = mapper;
            this.context = context;
        }

        private bool BundleExists(int bundleId)
        {
            return context.Bundles.Where(b => b.BundleId == bundleId).Any();
        }

        private bool SameNameExists(int bundleId, string name, int? bundleCompetencyGroupId = null)
        {
            return context.BundleCompetencyGroups
                .Where(g => g.BundleId == bundleId
                    && g.Name == name
                    && (bundleCompetencyGroupId == null || bundleCompetencyGroupId != g.BundleCompetencyGroupId))
                .Any();
        }

        [HttpGet]
        public async Task<ActionResult<List<BundleCompetencyGroupDto>>> GetGroups(int bundleId)
        {
            if (!BundleExists(bundleId))
            {
                return NotFound();
            }
            var groups = await context.BundleCompetencyGroups
                .Where(g => g.BundleId == bundleId)
                .ToListAsync();
            return mapper.Map<List<BundleCompetencyGroupDto>>(groups);
        }

        [HttpPost]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<BundleCompetencyGroupDto>> AddGroup(int bundleId, BundleCompetencyGroupDto groupDto)
        {
            if (!BundleExists(bundleId))
            {
                return NotFound();
            }
            if (SameNameExists(bundleId, groupDto.Name))
            {
                return BadRequest("Name must be unique");
            }

            var group = new BundleCompetencyGroup()
            {
                BundleId = bundleId,
                Name = groupDto.Name,
                Order = groupDto.Order
            };
            context.BundleCompetencyGroups.Add(group);
            await context.SaveChangesAsync();
            AdjustGroupOrders(group);
            return mapper.Map<BundleCompetencyGroupDto>(group);
        }

        [HttpPut("{bundleCompetencyGroupId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<BundleCompetencyGroupDto>> UpdateGroup(int bundleId, int bundleCompetencyGroupId, BundleCompetencyGroupDto groupDto)
        {
            if (!BundleExists(bundleId))
            {
                return NotFound();
            }
            var group = await context.BundleCompetencyGroups.FindAsync(bundleCompetencyGroupId);
            if (group == null)
            {
                return NotFound();
            }
            if (SameNameExists(bundleId, groupDto.Name, bundleCompetencyGroupId))
            {
                return BadRequest("Name must be unique");
            }

            group.Name = groupDto.Name;
            group.Order = groupDto.Order;
            context.Update(group);
            await context.SaveChangesAsync();
            AdjustGroupOrders(group);
            return mapper.Map<BundleCompetencyGroupDto>(group);
        }

        [HttpDelete("{bundleCompetencyGroupId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<BundleCompetencyGroupDto>> DeleteGroup(int bundleId, int bundleCompetencyGroupId)
        {
            if (!BundleExists(bundleId))
            {
                return NotFound();
            }
            var group = await context.BundleCompetencyGroups.FindAsync(bundleCompetencyGroupId);
            if (group == null)
            {
                return NotFound();
            }
            try
            {
                context.Remove(group);
                await context.SaveChangesAsync();
                AdjustGroupOrders(group);
            }
            catch (Exception)
            {
                return BadRequest("Cannot delete group. Competencies must be removed from the group, and the group cannot have been used to document a student competency.");
            }
            return mapper.Map<BundleCompetencyGroupDto>(group);
        }

        private void AdjustGroupOrders(BundleCompetencyGroup group)
        {
            var groups = context.BundleCompetencyGroups
                .Where(g => g.BundleId == group.BundleId)
                .OrderBy(g => g.Order)
                .ToList();

            int i = 1;
            bool changeMade = false;
            foreach (var g in groups)
            {
                if (g.Order != i)
                {
                    //not correct
                    g.Order = i;
                    context.Entry(g).State = EntityState.Modified;
                    changeMade = true;
                }
                i++;
            }

            if (changeMade)
            {
                context.SaveChanges();
            }
        }
    }
}
