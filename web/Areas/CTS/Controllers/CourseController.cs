using Amazon.SimpleSystemsManagement.Model;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CTS.Models;
using Viper.Areas.CTS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;
using static System.Runtime.CompilerServices.RuntimeHelpers;

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
        private readonly CrestCourseService courseService;
        private readonly VIPERContext context;
        private readonly IMapper mapper;

        public CourseController(IMapper mapper, VIPERContext context)
        {
            this.mapper = mapper;
            this.context = context;
            courseService = new(context);
        }

        [HttpGet]
        public async Task<ActionResult<List<CourseDto>>> GetCourseList(string? termCode = null, string? subjectCode = null, string? courseNum = null, int? courseId = null)
        {
            if(termCode == null && courseId == null)
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

        [HttpGet("{courseId}/sessions")]
        public async Task<ActionResult<List<SessionDto>>> GetCourseSessions(int courseId, bool includeLegacyCompetencies = false)
        {
            var courseCheck = await context.Courses.FindAsync(courseId);
            if (courseCheck == null)
            {
                return NotFound();
            }
            var sessions = await context.Sessions
                .Include(s => s.Competencies)
                .Where(s => s.CourseId == courseId)
                .OrderBy(c => c.PaceOrder)
                .ThenBy(c => c.SessionId)
                .ToListAsync();
            return mapper.Map<List<SessionDto>>(sessions);
        }

        [HttpGet("{courseId}/sessions/{sessionId}/competencies")]
        public async Task<ActionResult<List<SessionCompetencyDto>>> GetSessionCompetencies(int courseId, int sessionId)
        {
            if (!await SessionExists(courseId, sessionId))
            {
                return NotFound();
            }
            var sessionComps = await context.SessionCompetencies
                .Where(sc => sc.SessionId == sessionId)
                .Include(sc => sc.Competency)
                .Include(sc => sc.Session)
                .Include(sc => sc.Level)
                .Include(sc => sc.Role)
                .ToListAsync();

            return mapper.Map<List<SessionCompetencyDto>>(sessionComps);
        }

        private async Task<bool> CourseExists(int courseId)
        {
            return (await context.Courses.FindAsync(courseId)) != null;
        }

        private async Task<bool> SessionExists(int courseId, int sessionId)
        {
            var session = await context.Sessions.FindAsync(sessionId);
            return session != null && session.CourseId == courseId;
        }

        private async Task<List<CourseDto>> GetCourses(string? termCode = null, string? subjectCode = null, string? courseNum = null, int? courseId = null)
        {
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
            //var courses = await courseService.GetCourses(termCode: termCode, subjectCode: subjectCode, courseNum: courseNum);
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
    }
}
