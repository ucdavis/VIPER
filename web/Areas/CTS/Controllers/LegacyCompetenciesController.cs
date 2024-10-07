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
        public async Task<ActionResult<List<LegacySessionCompetency>>> GetSessionCompentencies(int sessionId)
        {
            return await context.LegacySessionCompetencies
                .Where(l => l.SessionId == sessionId)
                .OrderBy(l => l.SessionCompetencyOrder)
                .ToListAsync();
        }

        [HttpGet("course/{courseId}")]
        public async Task<ActionResult<List<LegacySessionCompetency>>> GetCourseCompetencies(int courseId)
        {
            return await context.LegacySessionCompetencies
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.PaceOrder)
                .ThenBy(l => l.SessionCompetencyOrder)
                .ToListAsync();
        }

        [HttpGet("term/{termCode}")]
        public async Task<ActionResult<List<LegacySessionCompetency>>> GetCoursesCompetenciesForTerm(int termCode)
        {
            return await context.LegacySessionCompetencies
                .Where(l => l.AcademicYear == termCode.ToString())
                .OrderBy(l => l.CourseTitle)
                .ThenBy(l => l.PaceOrder)
                .ThenBy(l => l.SessionCompetencyOrder)
                .ToListAsync();
        }
    }
}
