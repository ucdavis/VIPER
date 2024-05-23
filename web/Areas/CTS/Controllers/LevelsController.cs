using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics;
using System.Linq.Dynamic.Core;
using Viper.Areas.CTS.Models;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/cts/levels")]
    [Permission(Allow = "SVMSecure")]
    public class LevelsController : ApiController
    {
        private readonly VIPERContext context;
        public LevelsController(VIPERContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Level>>> GetLevels(bool? epa = null, bool active = true)
        {
            var q = context.Levels.AsQueryable();
            if (epa != null)
            {
                q = q.Where(l => l.Epa == epa);
            }
            q = q.Where(l => (active && l.Active) || (!active && !l.Active));
            return await q.OrderBy(l => l.Epa ? 1 : 0)
                .ThenBy(l => l.Dops ? 1 : 0)
                .ThenBy(l => l.Clinical ? 1 : 0)
                .ThenBy(l => l.Course ? 1 : 0)
                .ThenBy(l => l.Milestone ? 1 : 0)
                .ThenBy(l => l.Order)
                .ToListAsync();
        }

        [HttpGet("{levelId}")]
        public async Task<ActionResult<Level>> GetLevel(int levelId)
        {
            var level = await context.Levels.FindAsync(levelId);
            if (level == null)
            {
                return NotFound();
            }
            return level;
        }

        [HttpPost]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<Level>> Createlevel(LevelCreateUpdate level)
        {
            using var trans = context.Database.BeginTransaction();

            //Make sure there isn't a level with the same description and the same level type
            if (!await CheckNameAndType(level))
            {
                return BadRequest("Another level for the same type of assessment has the same text.");
            }

            var l = new Level()
            {
                LevelName = level.LevelName,
                Description = level.Description,
                Active = level.Active,
                Order = level.Order,
                Epa = level.Epa,
                Dops = level.Dops,
                Clinical = level.Clinical,
                Course = level.Course,
                Milestone = level.Milestone
            };
            context.Add(l);
            await context.SaveChangesAsync();
            AdjustLevelOrders(l);
            await trans.CommitAsync();
            return Ok(l);
        }

        [HttpPut("{levelId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<Level>> UpdateLevel(LevelCreateUpdate level)
        {
            using var trans = context.Database.BeginTransaction();

            if (!await CheckNameAndType(level))
            {
                return BadRequest("Another level for the same type of assessment has the same text.");
            }

            var existing = await context.Levels.FindAsync(level.LevelId);
            if (existing == null)
            {
                return NotFound();
            }

            existing.LevelName = level.LevelName;
            existing.Description = level.Description;
            existing.Active = level.Active;
            existing.Order = level.Order;
            existing.Epa = level.Epa;
            existing.Dops = level.Dops;
            existing.Clinical = level.Clinical;
            existing.Course = level.Course;
            existing.Milestone = level.Milestone;

            context.Entry(existing).State = EntityState.Modified;
            await context.SaveChangesAsync();
            AdjustLevelOrders(existing);
            await trans.CommitAsync();
            return Ok(existing);
        }

        [HttpDelete("{levelId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult> DeleteLevel(int levelId)
        {
            using var trans = context.Database.BeginTransaction();
            var existing = await context.Levels.FindAsync(levelId);
            if (existing == null)
            {
                return NotFound();
            }

            try
            {
                context.Entry(existing).State = EntityState.Deleted;
                await context.SaveChangesAsync();
                AdjustLevelOrders(existing);
                await trans.CommitAsync();
            }
            catch (Exception)
            {
                return BadRequest("Could not delete level. If this level has been used in an assessment, it cannot be deleted.");
            }

            return Ok(existing);
        }

        /// <summary>
        /// Go through levels for this levels type and adjust order if necessary
        /// </summary>
        /// <param name="level"></param>
        private void AdjustLevelOrders(Level level)
        {
            //get levels for this type of assessment
            var levels = context.Levels.AsQueryable();
            if (level.Epa)
            {
                levels = levels.Where(l => l.Epa);
            }
            levels = levels.OrderBy(l => l.Order);

            //check orders are correct
            int i = 1;
            bool changeMade = false;
            foreach (var l in levels)
            {
                if (l.Order != i)
                {
                    //not correct
                    l.Order = i;
                    context.Entry(l).State = EntityState.Modified;
                    changeMade = true;
                }
                i++;
            }

            if (changeMade)
            {
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Checks for conflicts with level name against other levels with the same name and type
        /// </summary>
        /// <param name="updatedLevel"></param>
        /// <returns>True if OK, False if there's a conflict</returns>
        private async Task<bool> CheckNameAndType(LevelCreateUpdate updatedLevel)
        {
            var conflict = await context.Levels
                .Where(l => l.LevelName == updatedLevel.LevelName &&
                    (updatedLevel.LevelId == null || updatedLevel.LevelId != l.LevelId) &&
                    (
                        updatedLevel.Epa && l.Epa ||
                        updatedLevel.Dops && l.Dops ||
                        updatedLevel.Course && l.Course ||
                        updatedLevel.Clinical && l.Clinical ||
                        updatedLevel.Milestone && l.Milestone
                    ))
                .ToListAsync();

            return conflict.Count == 0;
        }
    }
}
