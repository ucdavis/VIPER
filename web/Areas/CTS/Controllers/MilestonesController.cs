using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CTS.Models;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/api/cts/milestones/")]
    [Permission(Allow = "SVMSecure")]
    public class MilestonesController : ApiController
    {
        private readonly VIPERContext context;
        private readonly IMapper mapper;

        public MilestonesController(VIPERContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        //NB: Milestones are defined as bundles, so add/update/delete functions are in BundlesController
        //The GetMilestones function returns a simplified and more Milestone-centric object

        [HttpGet]
        public async Task<ActionResult<List<MilestoneDto>>> GetMilestones()
        {
            var bundleQuery = await context.Bundles
                .Include(b => b.BundleCompetencies)
                .ThenInclude(b => b.Competency)
                .Where(b => b.Milestone)
                .OrderBy(b => b.Name)
                .ToListAsync();
            return mapper.Map<List<MilestoneDto>>(bundleQuery);
        }


        [HttpGet("{milestoneId}/levels")]
        public async Task<ActionResult<List<MilestoneLevelDto>>> GetMilestoneLevels(int milestoneId)
        {
            var bundleExists = await context.Bundles
                .Where(b => b.BundleId == milestoneId)
                .Where(b => b.Milestone)
                .AnyAsync();
            if (!bundleExists)
            {
                return NotFound();
            }
            var levelQuery = await context.MilestoneLevels
                .Include(ml => ml.Level)
                .Where(ml => ml.BundleId == milestoneId)
                .OrderBy(ml => ml.Level.Order)
                .ToListAsync();
            return mapper.Map<List<MilestoneLevelDto>>(levelQuery);
        }

        [HttpPut("{milestoneId}/levels")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<List<MilestoneLevelDto>>> SetMilestoneLevels(int milestoneId, List<MilestoneLevelAddUpdate> milestoneLevels)
        {
            var bundleExists = await context.Bundles
                .Where(b => b.BundleId == milestoneId)
                .Where(b => b.Milestone)
                .AnyAsync();
            if (!bundleExists)
            {
                return NotFound();
            }
            var existingLevels = await context.MilestoneLevels
                .Where(ml => ml.BundleId == milestoneId)
                .ToListAsync();

            foreach (var ml in milestoneLevels)
            {
                //look for existing to determine whether to add milestoneLevel or update description
                var existingLevel = existingLevels.Where(el => el.LevelId == ml.LevelId).FirstOrDefault();
                if (existingLevel != null)
                {
                    existingLevel.Description = ml.Description;
                    context.MilestoneLevels.Update(existingLevel);
                }
                else
                {
                    context.MilestoneLevels.Add(new Viper.Models.CTS.MilestoneLevel()
                    {
                        BundleId = milestoneId,
                        LevelId = ml.LevelId,
                        Description = ml.Description,
                    });
                }
            }

            //shouldn't happen often, but if a level was not sent in the milestoneLevels argument, delete the milestone level
            //e.g. if the level is no longer in use
            var levelIds = milestoneLevels.Select(ml => ml.LevelId).ToList();
            var toDelete = existingLevels.Where(el => !levelIds.Contains(el.LevelId)).ToList();
            foreach(var deleteLevel in toDelete)
            {
                context.MilestoneLevels.Remove(deleteLevel);
            }

            await context.SaveChangesAsync();

            var savedLevels = await context.MilestoneLevels
                .Include(ml => ml.Level)
                .Where(ml => ml.BundleId == milestoneId)
                .ToListAsync();
            return mapper.Map<List<MilestoneLevelDto>>(savedLevels);                
        }
    }
}
