using Microsoft.EntityFrameworkCore;
using Viper.Areas.CTS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Course = Viper.Areas.CTS.Models.Course;
using Session = Viper.Areas.CTS.Models.Session;

namespace Viper.Areas.CTS.Services
{
    public class CrestCourseService
    {
        private readonly VIPERContext _context;
        public CrestCourseService(VIPERContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get Courses for year, or by course number or subject code
        /// </summary>
        /// <param name="academicYear"></param>
        /// <param name="courseNum"></param>
        /// <param name="subjectCode"></param>
        /// <returns></returns>
        public async Task<List<Course>> GetCourses(string? termCode, string? courseNum, string? subjectCode)
        {
            var cso = await GetCourseSessionOffering(academicYear: termCode, courseNum: courseNum, subjectCode: subjectCode);
            return CourseSessionOfferingsToCourses(cso);
        }

        /// <summary>
        /// Get a single course + sessions and offerings
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        public async Task<Course?> GetCourse(int courseId)
        {
            var cso = await GetCourseSessionOffering(courseId: courseId);
            return CourseSessionOfferingsToCourses(cso).FirstOrDefault();
        }

        /// <summary>
        /// Get a single session and offerings
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public async Task<Session?> GetSession(int sessionId)
        {
            var cso = await GetCourseSessionOffering(sessionId: sessionId);
            return CourseSessionOfferingsToSessions(cso).FirstOrDefault();
        }

        /// <summary>
        /// Gets sessions for a course and their offerings
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        public async Task<List<Session>> GetSessions(int courseId)
        {
            var course = await GetCourse(courseId);
            if (course == null)
            {
                return new List<Session>();
            }
            return course.Sessions;
        }

        /// <summary>
        /// Turn a list of CourseSessionOfferings into a list of Course objects
        /// </summary>
        /// <param name="csos"></param>
        /// <returns></returns>
        static private List<Course> CourseSessionOfferingsToCourses(List<CourseSessionOffering> csos)
        {
            List<Course> courses = new();
            foreach (var cso in csos.GroupBy(c => c.CourseId))
            {
                var rows = cso.ToList();
                courses.Add(new Course(rows[0])
                {
                    Sessions = CourseSessionOfferingsToSessions(rows)
                });
            }
            return courses;
        }

        /// <summary>
        /// Turn a list of CourseSessionOfferings, all from the same course, into a list of sessions for that course
        /// </summary>
        /// <param name="csos"></param>
        /// <returns></returns>
        static private List<Session> CourseSessionOfferingsToSessions(List<CourseSessionOffering> csos)
        {
            List<Session> sessions = new();
            foreach (var cso in csos.GroupBy(c => c.SessionId))
            {
                var rows = cso.ToList();
                sessions.Add(new Session(rows[0])
                {
                    Offerings = CourseSessionOfferingsToOfferings(rows)
                });
            }
            return sessions;
        }

        /// <summary>
        /// Turn a list of CourseSessionOfferings, all from the same session, into a list of offerings for that session
        /// </summary>
        /// <param name="csos"></param>
        /// <returns></returns>
        static private List<Offering> CourseSessionOfferingsToOfferings(List<CourseSessionOffering> csos)
        {
            List<Offering> offerings = new();
            foreach (var cso in csos)
            {
                offerings.Add(new Offering(cso));
            }
            return offerings;
        }

        /// <summary>
        /// Get a list CourseSessionOffering objects from the database
        /// </summary>
        /// <param name="academicYear"></param>
        /// <param name="courseNum"></param>
        /// <param name="subjectCode"></param>
        /// <param name="crn"></param>
        /// <param name="sessionType"></param>
        /// <param name="room"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="studentGroup"></param>
        /// <returns></returns>
        private async Task<List<CourseSessionOffering>> GetCourseSessionOffering(
            int? courseId = null, string? academicYear = null, string? courseNum = null, string? subjectCode = null,
            string? crn = null, int? sessionId = null, string? sessionType = null,
            int? offerId = null, string? room = null, DateTime? from = null, DateTime? to = null, string? studentGroup = null)
        {
            var cso = _context.CourseSessionOffering.AsQueryable();
            if (courseId != null)
            {
                cso = cso.Where(c => c.CourseId == courseId);
            }
            if (academicYear != null)
            {
                cso = cso.Where(c => c.AcademicYear == academicYear);
            }
            if (courseNum != null)
            {
                cso = cso.Where(c => c.SsaCourseNum != null && c.SsaCourseNum.EndsWith(courseNum));
            }
            if (subjectCode != null)
            {
                cso = cso.Where(c => c.SsaCourseNum != null && c.SsaCourseNum.StartsWith(subjectCode));
            }
            if (crn != null)
            {
                cso = cso.Where(c => c.Crn == crn);
            }
            if (sessionId != null)
            {
                cso.Where(cso => cso.SessionId == sessionId);
            }
            if (sessionType != null)
            {
                cso = cso.Where(c => c.SessionType == sessionType);
            }
            if (offerId != null)
            {
                cso = cso.Where(c => c.EduTaskOfferid == offerId);
            }
            if (room != null)
            {
                cso = cso.Where(c => c.Room == room);
            }
            if (from != null)
            {
                cso = cso.Where(c => c.ThruDate >= from);
            }
            if (to != null)
            {
                cso = cso.Where(c => c.FromDate <= to);
            }
            if (studentGroup != null)
            {
                cso = cso.Where(c => c.StudentGroup == studentGroup);
            }
            return await cso.OrderBy(c => c.AcademicYear)
                .ThenBy(c => c.SsaCourseNum)
                .ThenBy(c => c.CourseId)
                .ThenBy(c => c.PaceOrder)
                .ThenBy(c => c.SessionId)
                .ThenBy(c => c.FromDate)
                .ThenBy(c => c.FromTime)
                .ThenBy(c => c.EduTaskOfferid)
                .ToListAsync();
        }
    }
}
