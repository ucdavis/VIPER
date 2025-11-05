using Microsoft.EntityFrameworkCore;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Students.Models;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Students.Services
{
    /// <summary>
    /// Service for retrieving course information for photo gallery
    /// </summary>
    public class CourseService : ICourseService
    {
        private readonly CoursesContext _coursesContext;
        private readonly ILogger<CourseService> _logger;

        public CourseService(CoursesContext coursesContext, ILogger<CourseService> logger)
        {
            _coursesContext = coursesContext;
            _logger = logger;
        }

        /// <summary>
        /// Get available courses for photo gallery, grouped by term
        /// </summary>
        public async Task<List<CoursesByTerm>> GetAvailableCoursesForPhotosAsync(string subjectCode = "VET")
        {
            try
            {
                // Calculate term range: current academic year + previous year
                var termRange = TermCodeService.GetAcademicYearTermRange();

                // Query courses with enrolled students
                var courses = await _coursesContext.Baseinfos
                    .Where(b => b.BaseinfoSubjCode == subjectCode)
                    .Where(b => b.BaseinfoTermCode != null &&
                               string.Compare(b.BaseinfoTermCode, termRange.StartTerm) >= 0 &&
                               string.Compare(b.BaseinfoTermCode, termRange.EndTerm) <= 0)
                    .Where(b => b.BaseinfoEnrollment > 0)
                    .Where(b => _coursesContext.Rosters.Any(r =>
                        r.RosterTermCode == b.BaseinfoTermCode &&
                        r.RosterCrn == b.BaseinfoCrn))
                    .OrderByDescending(b => b.BaseinfoTermCode)
                    .ThenBy(b => b.BaseinfoSubjCode)
                    .ThenBy(b => b.BaseinfoCrseNumb)
                    .ThenBy(b => b.BaseinfoSeqNumb)
                    .Select(b => new
                    {
                        TermCode = b.BaseinfoTermCode ?? string.Empty,
                        Crn = b.BaseinfoCrn ?? string.Empty,
                        SubjectCode = b.BaseinfoSubjCode ?? string.Empty,
                        CourseNumber = b.BaseinfoCrseNumb ?? string.Empty,
                        Title = b.BaseinfoTitle ?? string.Empty
                    })
                    .ToListAsync();

                // Group by term
                var coursesByTerm = courses
                    .GroupBy(c => c.TermCode)
                    .Select(g => new CoursesByTerm
                    {
                        TermCode = g.Key,
                        TermDescription = TermCodeService.GetTermDescriptionFromYYYYMM(g.Key),
                        Courses = g.Select(c => new CourseInfo
                        {
                            TermCode = c.TermCode,
                            Crn = c.Crn,
                            SubjectCode = c.SubjectCode,
                            CourseNumber = c.CourseNumber,
                            Title = c.Title,
                            TermDescription = TermCodeService.GetTermDescriptionFromYYYYMM(c.TermCode)
                        }).ToList()
                    })
                    .ToList();

                return coursesByTerm;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available courses for subject code {SubjectCode}", subjectCode);
                return new List<CoursesByTerm>();
            }
        }

        /// <summary>
        /// Get detailed information for a specific course
        /// </summary>
        public async Task<CourseInfo?> GetCourseInfoAsync(string termCode, string crn)
        {
            try
            {
                var course = await _coursesContext.Baseinfos
                    .Where(b => b.BaseinfoTermCode == termCode && b.BaseinfoCrn == crn)
                    .Select(b => new CourseInfo
                    {
                        TermCode = b.BaseinfoTermCode ?? string.Empty,
                        Crn = b.BaseinfoCrn ?? string.Empty,
                        SubjectCode = b.BaseinfoSubjCode ?? string.Empty,
                        CourseNumber = b.BaseinfoCrseNumb ?? string.Empty,
                        Title = b.BaseinfoTitle ?? string.Empty,
                        TermDescription = TermCodeService.GetTermDescriptionFromYYYYMM(b.BaseinfoTermCode ?? string.Empty)
                    })
                    .FirstOrDefaultAsync();

                return course;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching course info for term {TermCode} and CRN {Crn}", termCode, crn);
                return null;
            }
        }
    }
}
