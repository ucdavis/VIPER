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
    [Route("/api/cts/legacyCompetencies/")]
    [Permission(Allow = "SVMSecure.CTS.Manage")]
    public class LegacyCompetenciesController : ApiController
    {
        private readonly VIPERContext context;
        private readonly IMapper mapper;

        public LegacyCompetenciesController(VIPERContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<LegacyCompetencyDto>>> Get()
        {
            var legacyComps = await context.LegacyCompetencies
                .Include(e => e.DvmCompetencyMapping)
                .ThenInclude(e => e.Competency)
                .ToListAsync();

            return mapper.Map<List<LegacyCompetencyDto>>(legacyComps);
        }

        [HttpGet("session/{sessionId}")]
        public async Task<ActionResult<List<LegacySessionCompetencyDto>>> GetSessionCompentencies(int sessionId)
        {
            var comps = await context.LegacySessionCompetencies
                .Where(l => l.SessionId == sessionId)
                .OrderBy(l => l.SessionCompetencyOrder)
                .ThenBy(l => l.DvmCompetencyId)
                .ThenBy(l => l.DvmRoleName)
                .ThenBy(l => l.DvmLevelOrder)
                .ToListAsync();
            return GroupSessionCompetencies(comps);
        }

        [HttpGet("course/{courseId}")]
        public async Task<ActionResult<List<LegacySessionCompetencyDto>>> GetCourseCompetencies(int courseId)
        {
            var comps = await context.LegacySessionCompetencies
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.PaceOrder)
                .ThenBy(l => l.SessionCompetencyOrder)
                .ThenBy(l => l.DvmCompetencyId)
                .ThenBy(l => l.DvmRoleName)
                .ThenBy(l => l.DvmLevelOrder)
                .ToListAsync();
            return GroupSessionCompetencies(comps);
        }

        private List<LegacySessionCompetencyDto> GroupSessionCompetencies(List<LegacySessionCompetency> comps)
        {
            List<LegacySessionCompetencyDto> lscs = new();
            int lastComp = 0;
            int? lastRole = null;
            LegacySessionCompetencyDto? current = null;
            foreach (var sessionCompetency in comps)
            {
                if (sessionCompetency.DvmCompetencyId != null && lastComp != sessionCompetency.DvmCompetencyId || lastRole != sessionCompetency.DvmRoleId)
                {
                    current = mapper.Map<LegacySessionCompetencyDto>(sessionCompetency);
                    lscs.Add(current);
                    lastComp = (int)sessionCompetency.DvmCompetencyId!;
                    lastRole = sessionCompetency.DvmRoleId;
                }
                if (current != null && sessionCompetency.DvmLevelId != null)
                {
                    current.Levels.Add(new LevelIdAndNameDto()
                    {
                        LevelId = (int)sessionCompetency.DvmLevelId,
                        LevelName = sessionCompetency.DvmLevelName,
                    });
                }
            }
            return lscs;
        }

        [HttpGet("term/{termCode}")]
        public async Task<ActionResult<List<LegacySessionCompetencyDto>>> GetCoursesCompetenciesForTerm(int termCode)
        {
            var comps = await context.LegacySessionCompetencies
                .Where(l => l.AcademicYear == termCode.ToString())
                .OrderBy(l => l.CourseTitle)
                .ThenBy(l => l.PaceOrder)
                .ThenBy(l => l.SessionCompetencyOrder)
                .ToListAsync();
            return mapper.Map<List<LegacySessionCompetencyDto>>(comps);
        }
    }
}
