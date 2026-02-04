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
    /*
     * Permissions for viewing course + session competency info:
     *  Faculty can view courses they are the leader of or an instructor for
     *  Dept proxies can view their courses
     *  CTS.Manage and CTS.LoginStudents can view all courses
     *  Students cannot view courses
     */
    [Route("/api/cts/courses/")]
    [Permission(Allow = "SVMSecure.CTS", Deny = "SVMSecure.CTS.Student")]
    public class CourseController : ApiController
    {
        private readonly RAPSContext rapsContext;
        private readonly VIPERContext context;
        private readonly IMapper mapper;
        private readonly List<string> CompetencySupportedSessionTypes = new List<string>()
        {
            "Lab", "L/D", "Dis","Exm","AUT","JLC","PRS","CBL","PBL","D/L","TBL","ACT"
        };

        public CourseController(IMapper mapper, VIPERContext context, RAPSContext rapsContext)
        {
            this.mapper = mapper;
            this.context = context;
            this.rapsContext = rapsContext;
        }

        [HttpGet]
        public async Task<ActionResult<List<CourseDto>>> GetCourseList(string? termCode = null, string? subjectCode = null, string? courseNum = null, int? courseId = null)
        {
            if (termCode == null && courseId == null)
            {
                return BadRequest("Term code or course ID is required.");
            }
            var courses = await GetCourses(termCode, subjectCode, courseNum, courseId);
            return mapper.Map<List<CourseDto>>(courses);
        }

        [HttpGet("{courseId}")]
        public async Task<ActionResult<CourseDto>> GetCourse(int courseId)
        {
            var c = (await GetCourses(courseId: courseId)).FirstOrDefault();
            if (c == null)
            {
                return NotFound();
            }
            return c;
        }

        /*
         * Course Roles
         */
        [HttpGet("{courseId}/roles")]
        public async Task<ActionResult<List<RoleDto>>> GetCourseRoles(int courseId)
        {
            var courseCheck = await GetCourseForUser(courseId);
            if (courseCheck == null)
            {
                return NotFound();
            }

            var roles = await context.CourseRoles
                .Where(cr => cr.CourseId == courseId)
                .Include(cr => cr.Role)
                .Select(cr => cr.Role)
                .OrderBy(r => r.Name)
                .ToListAsync();
            return mapper.Map<List<RoleDto>>(roles);
        }

        [HttpPut("{courseId}/roles")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<List<RoleDto>>> SetCourseRoles(int courseId, List<int> roleIds)
        {
            var existingRoles = await context.CourseRoles
                .Where(cr => cr.CourseId == courseId)
                .ToListAsync();
            var toRemove = existingRoles.Where(r => !roleIds.Contains(r.RoleId)).ToList();
            var toAdd = roleIds.Where(r => !existingRoles.Any(er => er.RoleId == r)).ToList();

            try
            {
                using var trans = await context.Database.BeginTransactionAsync();
                foreach (var r in toRemove)
                {
                    context.Remove(r);
                }
                foreach (var r in toAdd)
                {
                    context.Add(new CourseRole()
                    {
                        CourseId = courseId,
                        RoleId = r
                    });
                }
                await context.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch (Exception)
            {
                return BadRequest("Could not set roles.");
            }

            var rolesUpdated = await context.CourseRoles
                .Where(cr => cr.CourseId == courseId)
                .Include(cr => cr.Role)
                .Select(cr => cr.Role)
                .OrderBy(r => r.Name)
                .ToListAsync();
            return mapper.Map<List<RoleDto>>(rolesUpdated);
        }

        /* 
         * Sessions
         */
        [HttpGet("{courseId}/sessions")]
        public async Task<ActionResult<List<SessionDto>>> GetCourseSessions(int courseId, bool? supportedSessionTypes = null, bool includeLegacyCompetencies = false)
        {
            var courseCheck = await GetCourseForUser(courseId);
            if (courseCheck == null)
            {
                return NotFound();
            }

            var sessionsQ = context.Sessions
                .Include(s => s.Competencies)
                .Where(s => s.CourseId == courseId);
            if (supportedSessionTypes ?? false)
            {
                sessionsQ = sessionsQ.Where(s => s.Type != null && CompetencySupportedSessionTypes.Contains(s.Type));
            }
            var sessions = await sessionsQ
                .OrderBy(c => c.PaceOrder)
                .ThenBy(c => c.SessionId)
                .ToListAsync();
            return mapper.Map<List<SessionDto>>(sessions);
        }

        [HttpGet("{courseId}/sessions/{sessionId}")]
        public async Task<ActionResult<SessionDto>> GetSession(int courseId, int sessionId)
        {
            var courseCheck = await GetCourseForUser(courseId);
            if (courseCheck == null)
            {
                return NotFound();
            }

            var session = await context.Sessions.FindAsync(sessionId);
            if (session == null || session.CourseId != courseId)
            {
                return NotFound();
            }
            return mapper.Map<SessionDto>(session);
        }

        /*
         * Session Competencies
         */
        [HttpGet("{courseId}/sessions/{sessionId}/competencies")]
        public async Task<ActionResult<List<SessionCompetencyDto>>> GetSessionCompetencies(int courseId, int sessionId)
        {
            var courseCheck = await GetCourseForUser(courseId);
            if (courseCheck == null)
            {
                return NotFound();
            }

            var session = await GetCourseSession(courseId, sessionId);
            if (session == null)
            {
                return NotFound();
            }
            var sessionComps = await context.SessionCompetencies
                .Where(sc => sc.SessionId == sessionId)
                .Include(sc => sc.Competency)
                .Include(sc => sc.Level)
                .Include(sc => sc.Role)
                .OrderBy(sc => sc.Order)
                .ThenBy(sc => sc.Competency.Number)
                .ThenBy(sc => sc.CompetencyId)
                .ThenBy(sc => sc.RoleId)
                .ToListAsync();

            List<SessionCompetencyDto> scs = new();
            int lastComp = 0;
            int? lastRole = 0;
            SessionCompetencyDto? current = null;
            foreach (var sessionCompetency in sessionComps)
            {
                if (lastComp != sessionCompetency.CompetencyId || lastRole != sessionCompetency.RoleId)
                {
                    current = mapper.Map<SessionCompetencyDto>(sessionCompetency);
                    scs.Add(current);
                    lastComp = sessionCompetency.CompetencyId;
                    lastRole = sessionCompetency.RoleId;
                }
                if (current != null)
                {
                    current.Levels.Add(new LevelIdAndNameDto()
                    {
                        LevelId = sessionCompetency.LevelId,
                        LevelName = sessionCompetency.Level.LevelName,
                    });
                }
            }

            return scs;
        }

        [HttpPost("{courseId}/sessions/{sessionId}/competencies")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<List<SessionCompetencyDto>>> AddSessionCompetency(int courseId, int sessionId, CreateUpdateSessionCompetency sessionComp)
        {
            var checkResult = await CheckAddUpdateSessionComp(courseId, sessionId, sessionComp);
            if (checkResult != null)
            {
                return checkResult;
            }

            foreach (var levelId in sessionComp.LevelIds)
            {
                var newSessionComp = new SessionCompetency()
                {
                    CompetencyId = sessionComp.CompetencyId,
                    SessionId = sessionComp.SessionId,
                    LevelId = levelId,
                    RoleId = sessionComp.RoleId,
                    Order = sessionComp.Order ?? 0
                };
                context.Add(newSessionComp);
            }
            await context.SaveChangesAsync();

            var sessionComps = await context.SessionCompetencies
                .Where(sc => sc.SessionId == sessionComp.SessionId)
                .Where(sc => sc.CompetencyId == sessionComp.CompetencyId)
                .ToListAsync();

            return mapper.Map<List<SessionCompetencyDto>>(sessionComps);
        }

        [HttpPut("{courseId}/sessions/{sessionId}/competencies")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<List<SessionCompetencyDto>>> UpdateSessionCompetency(int courseId, int sessionId, CreateUpdateSessionCompetency sessionComp)
        {
            var checkResult = await CheckAddUpdateSessionComp(courseId, sessionId, sessionComp);
            if (checkResult != null)
            {
                return checkResult;
            }

            var existing = await context.SessionCompetencies
                .Where(sc => sc.SessionId == sessionComp.SessionId)
                .Where(sc => sc.CompetencyId == sessionComp.CompetencyId)
                .ToListAsync();
            var toRemove = existing.Where(e => !sessionComp.LevelIds.Contains(e.LevelId)).ToList();
            var toAdd = sessionComp.LevelIds.Where(l => !existing.Any(esc => esc.LevelId == l)).ToList();

            try
            {
                using var trans = await context.Database.BeginTransactionAsync();
                foreach (var r in toRemove)
                {
                    context.Remove(r);
                }
                foreach (var l in toAdd)
                {
                    var newSessionComp = new SessionCompetency()
                    {
                        CompetencyId = sessionComp.CompetencyId,
                        SessionId = sessionComp.SessionId,
                        LevelId = l,
                        RoleId = sessionComp.RoleId,
                        Order = sessionComp.Order ?? 0
                    };
                    context.Add(newSessionComp);
                }
                await context.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch (Exception)
            {
                return BadRequest("Could not update levels.");
            }

            existing = await context.SessionCompetencies
                .Where(sc => sc.SessionId == sessionComp.SessionId)
                .Where(sc => sc.CompetencyId == sessionComp.CompetencyId)
                .ToListAsync();
            return mapper.Map<List<SessionCompetencyDto>>(existing);
        }

        [HttpDelete("{courseId}/sessions/{sessionId}/competencies/{competencyId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult> DeleteSessionCompetency(int courseId, int sessionId, int competencyId, int? roleId = null)
        {
            var comps = await context.SessionCompetencies
                .Where(sc => sc.SessionId == sessionId)
                .Where(sc => sc.CompetencyId == competencyId)
                .Where(rc => rc.RoleId == roleId)
                .ToListAsync();

            if (comps.Count == 0)
            {
                return NotFound();
            }

            foreach (var sc in comps)
            {
                context.Remove(sc);
            }

            await context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Check arguments for adding or updating a session competency
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="sessionId"></param>
        /// <param name="sessionComp"></param>
        /// <returns></returns>
        private async Task<ActionResult?> CheckAddUpdateSessionComp(int courseId, int sessionId, CreateUpdateSessionCompetency sessionComp)
        {
            if (sessionComp.SessionId != sessionId)
            {
                return BadRequest();
            }

            var courseCheck = await GetCourseForUser(courseId);
            if (courseCheck == null)
            {
                return NotFound();
            }

            var session = await GetCourseSession(courseId, sessionId);
            if (session == null)
            {
                return NotFound();
            }

            if (sessionComp.LevelIds == null || sessionComp.LevelIds.Count == 0)
            {
                return BadRequest("At least one level is required.");
            }

            if (sessionComp.RoleId != null)
            {
                //check that session is multi role and the role belongs to this course
                if (!(session.MultiRole ?? false))
                {
                    return BadRequest("Session is not multi-role.");
                }

                var courseRoles = await context.CourseRoles.Where(cr => cr.CourseId == courseId).Select(cr => cr.RoleId).ToListAsync();
                if (!courseRoles.Contains((int)sessionComp.RoleId))
                {
                    return BadRequest("Invalid Role.");
                }
            }

            if (sessionComp.SessionCompetencyId == null)
            {
                var existing = await context.SessionCompetencies
                    .Where(sc => sc.SessionId == sessionId)
                    .Where(sc => sc.CompetencyId == sessionComp.CompetencyId)
                    .Where(rc => rc.RoleId == sessionComp.RoleId)
                    .FirstOrDefaultAsync();
                if (existing != null)
                {
                    return BadRequest("Competency is already attached to session. Please edit existing record instead.");
                }
            }
            return null;
        }

        /// <summary>
        /// Check that a course exists and the user has access to it.
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        private async Task<Viper.Models.CTS.Course?> GetCourseForUser(int courseId)
        {
            var c = await context.Courses.FindAsync(courseId);
            if (c == null)
            {
                return null;
            }

            var courseIds = await GetCourseIdsForUserForTerm(c.AcademicYear);
            if (!courseIds.Contains(c.CourseId))
            {
                return null;
            }

            return c;
        }

        private async Task<Viper.Models.CTS.Session?> GetCourseSession(int courseId, int sessionId)
        {
            var session = await context.Sessions.FindAsync(sessionId);
            return (session != null && session.CourseId == courseId) ? session : null;
        }

        private async Task<List<CourseDto>> GetCourses(string? termCode = null, string? subjectCode = null, string? courseNum = null, int? courseId = null)
        {
            //get list of all courses matching criteria
            var courses = context.Courses.AsQueryable();
            if (termCode != null)
            {
                courses = courses.Where(c => c.AcademicYear == termCode);
            }
            if (subjectCode != null)
            {
                courses = courses.Where(c => c.CourseNum.StartsWith(subjectCode));
            }
            if (courseNum != null)
            {
                courses = courses.Where(c => c.CourseNum.EndsWith(courseNum));
            }
            if (courseId != null)
            {
                courses = courses.Where(c => c.CourseId == courseId);
            }
            var courseList = await courses.OrderBy(c => c.AcademicYear)
                .ThenBy(c => c.CourseNum)
                .ToListAsync();

            //get allowed courses per term
            var allTerms = courseList.Select(c => c.AcademicYear).Distinct();
            List<int> validCourseIds = new List<int>();
            foreach (var t in allTerms)
            {
                var courseIdsThisTerm = await GetCourseIdsForUserForTerm(t);
                validCourseIds.AddRange(courseIdsThisTerm);
            }
            courseList = courseList.Where(c => validCourseIds.Contains(c.CourseId)).ToList();

            var courseDtos = mapper.Map<List<CourseDto>>(courseList);

            foreach (var c in courseDtos)
            {
                c.CompetencyCount = await context.SessionCompetencies
                    .Include(sc => sc.Session)
                    .Where(sc => sc.Session.CourseId == c.CourseId)
                    .Select(sc => sc.CompetencyId)
                    .Distinct()
                    .CountAsync();
            }

            return courseDtos;
        }

        /// <summary>
        /// Return a list of course ids that the logged in user can access
        ///     Faculty can view courses they are the leader of or an instructor for
        ///     Dept proxies can view their courses
        ///     CTS.Manage and CTS.LoginStudents can view all courses
        ///     Students cannot view courses
        /// </summary>
        /// <param name="termCode"></param>
        /// <returns></returns>
        private async Task<List<int>> GetCourseIdsForUserForTerm(string termCode)
        {
            var userHelper = new UserHelper();
            if (userHelper.HasPermission(rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.Student"))
            {
                return new List<int>();
            }

            var isAdmin = userHelper.HasPermission(rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.Manage")
                || userHelper.HasPermission(rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.LoginStudents");
            if (isAdmin)
            {
                return await context.Courses
                    .Where(c => c.AcademicYear == termCode)
                    .Select(c => c.CourseId)
                    .ToListAsync();
            }

            return context.GetMyCourses(termCode, Int32.Parse(userHelper?.GetCurrentUser()?.Pidm ?? "0"))
                .Select(c => c.CourseId)
                .ToList();
        }
    }
}
