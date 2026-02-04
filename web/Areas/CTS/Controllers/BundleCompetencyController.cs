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
    [Route("/api/cts/bundles/{bundleId}/competencies")]
    [Permission(Allow = "SVMSecure.CTS")]
    public class BundleCompetencyController : ApiController
    {
        private readonly IMapper mapper;
        private readonly VIPERContext context;

        public BundleCompetencyController(IMapper mapper, VIPERContext context)
        {
            this.mapper = mapper;
            this.context = context;
        }

        private bool BundleExists(int bundleId)
        {
            return context.Bundles.Where(b => b.BundleId == bundleId).Any();
        }

        private bool CompetencyExists(int competencyId)
        {
            return context.Competencies.Where(b => b.CompetencyId == competencyId).Any();
        }

        [HttpGet]
        public async Task<ActionResult<List<BundleCompetencyDto>>> GetBundleCompetencies(int bundleId)
        {
            if (!BundleExists(bundleId))
            {
                return NotFound();
            }

            var bundleComps = await context.BundleCompetencies
                .Include(bc => bc.Competency)
                .Include(bc => bc.Role)
                .Include(bc => bc.BundleCompetencyGroup)
                .Include(bc => bc.BundleCompetencyLevels)
                .ThenInclude(bcl => bcl.Level)
                .Where(bc => bc.BundleId == bundleId)
                .OrderBy(bc => bc.BundleCompetencyGroup == null ? 0 : bc.BundleCompetencyGroup.Order)
                .ThenBy(bc => bc.Order)
                .ThenBy(bc => bc.Competency.Name)
                .ToListAsync();

            return mapper.Map<List<BundleCompetencyDto>>(bundleComps);
        }

        [HttpPost]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<BundleCompetencyDto>> AddBundleCompetency(int bundleId, BundleCompetencyAddUpdate bundleComp)
        {
            if (!BundleExists(bundleId) || !CompetencyExists(bundleComp.CompetencyId))
            {
                return NotFound();
            }
            var compExistsAlready = await context.BundleCompetencies
                .Where(bc => bc.BundleId == bundleId && bc.CompetencyId == bundleComp.CompetencyId && bc.RoleId == bundleComp.RoleId)
                .AnyAsync();
            if (compExistsAlready)
            {
                return BadRequest("Competency is already a part of this bundle.");
            }

            var bundleCompetency = new BundleCompetency()
            {
                BundleId = bundleId,
                CompetencyId = bundleComp.CompetencyId,
                BundleCompetencyGroupId = bundleComp.BundleCompetencyGroupId,
                Order = bundleComp.Order,
                RoleId = bundleComp.RoleId
            };

            //Create the bundle competency and add levels
            using var trans = await context.Database.BeginTransactionAsync();
            context.Add(bundleCompetency);
            await context.SaveChangesAsync();

            foreach (var levelId in bundleComp.LevelIds)
            {
                var level = await context.Levels.FindAsync(levelId);
                if (level != null)
                {
                    context.Add(new BundleCompetencyLevel()
                    {
                        BundleCompetencyId = bundleCompetency.BundleCompetencyId,
                        LevelId = levelId
                    });
                }
            }
            await context.SaveChangesAsync();
            AdjustBundleCompetencyOrders(bundleCompetency);
            await trans.CommitAsync();

            return mapper.Map<BundleCompetencyDto>(bundleCompetency);
        }

        [HttpPut("{bundleCompetencyId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<BundleCompetencyDto>> UpdateBundleCompetency(int bundleId, int bundleCompetencyId, BundleCompetencyAddUpdate bundleCompetency)
        {
            var bundleComp = await context.BundleCompetencies.FindAsync(bundleCompetencyId);
            if (bundleComp == null)
            {
                return NotFound();
            }
            if (!BundleExists(bundleId) || !CompetencyExists(bundleCompetency.CompetencyId))
            {
                return NotFound();
            }

            using var trans = await context.Database.BeginTransactionAsync();
            bundleComp.Order = bundleCompetency.Order;
            bundleComp.RoleId = bundleCompetency.RoleId;
            var existingLevels = await context.BundleCompetencyLevels.Where(bcl => bcl.BundleCompetencyId == bundleCompetencyId).ToListAsync();
            foreach (var existingLevel in existingLevels)
            {
                if (!bundleCompetency.LevelIds.Any(l => l == existingLevel.LevelId))
                {
                    context.BundleCompetencyLevels.Remove(existingLevel);
                }
            }
            foreach (var newLevel in bundleCompetency.LevelIds)
            {
                if (!bundleComp.BundleCompetencyLevels.Any(bcl => bcl.LevelId == newLevel))
                {
                    context.Add(new BundleCompetencyLevel()
                    {
                        BundleCompetencyId = bundleCompetencyId,
                        LevelId = newLevel
                    });
                }
            }
            await context.SaveChangesAsync();
            AdjustBundleCompetencyOrders(bundleComp);
            await trans.CommitAsync();

            return mapper.Map<BundleCompetencyDto>(bundleComp);
        }

        [HttpDelete("{bundleCompetencyId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<BundleCompetencyDto>> DeleteBundleCompetency(int bundleId, int bundleCompetencyId)
        {
            var bundleComp = await context.BundleCompetencies.FindAsync(bundleCompetencyId);
            if (bundleComp == null)
            {
                return NotFound();
            }
            if (!BundleExists(bundleId))
            {
                return NotFound();
            }

            try
            {
                context.BundleCompetencies.Remove(bundleComp);
                await context.SaveChangesAsync();
                AdjustBundleCompetencyOrders(bundleComp);
            }
            catch (Exception)
            {
                return BadRequest("Cannot delete this bundle competency.");
            }

            return mapper.Map<BundleCompetencyDto>(bundleComp);
        }

        private void AdjustBundleCompetencyOrders(BundleCompetency bundleComp)
        {
            var bundleComps = context.BundleCompetencies
                .Where(b => b.BundleId == bundleComp.BundleId)
                .Where(b => b.BundleCompetencyGroupId == bundleComp.BundleCompetencyGroupId)
                .OrderBy(b => b.Order)
                .ToList();

            //check orders are correct
            int i = 1;
            bool changeMade = false;
            foreach (var b in bundleComps)
            {
                if (b.Order != i)
                {
                    //not correct
                    b.Order = i;
                    context.Entry(b).State = EntityState.Modified;
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
