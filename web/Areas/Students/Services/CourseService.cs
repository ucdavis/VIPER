using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Students.Models;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Areas.Curriculum.Services;

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
        /// Get available courses for the current term
        /// </summary>
        public async Task<List<CourseInfo>> GetAvailableCoursesForPhotosAsync(string subjectCode = "VET")
        {
            try
            {
                var currentTermCode = await _coursesContext.Terminfos
                    .AsNoTracking()
                    .Where(t => t.TermCollCode == "VM" && t.TermCurrentTerm)
                    .Select(t => t.TermCode)
                    .FirstOrDefaultAsync()
                    ?? throw new InvalidOperationException("No current term is set in courses.dbo.terminfo. Please ensure term_current_term is set to true for the active term.");

                var termDescription = TermCodeService.GetTermCodeDescription(int.Parse(currentTermCode));

                return await _coursesContext.Baseinfos
                    .Where(b => b.BaseinfoSubjCode == subjectCode)
                    .Where(b => b.BaseinfoTermCode == currentTermCode)
                    .Where(b => b.BaseinfoEnrollment > 0)
                    .Where(b => _coursesContext.Rosters.Any(r =>
                        r.RosterTermCode == b.BaseinfoTermCode &&
                        r.RosterCrn == b.BaseinfoCrn))
                    .OrderBy(b => b.BaseinfoSubjCode)
                    .ThenBy(b => b.BaseinfoCrseNumb)
                    .ThenBy(b => b.BaseinfoSeqNumb)
                    .Select(b => new CourseInfo
                    {
                        TermCode = b.BaseinfoTermCode ?? string.Empty,
                        Crn = b.BaseinfoCrn ?? string.Empty,
                        SubjectCode = b.BaseinfoSubjCode ?? string.Empty,
                        CourseNumber = b.BaseinfoCrseNumb ?? string.Empty,
                        Title = b.BaseinfoTitle ?? string.Empty,
                        TermDescription = termDescription
                    })
                    .ToListAsync();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error fetching available courses for subject code {SubjectCode}", LogSanitizer.SanitizeString(subjectCode));
                return new List<CourseInfo>();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation fetching available courses for subject code {SubjectCode}", LogSanitizer.SanitizeString(subjectCode));
                return new List<CourseInfo>();
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
                        TermDescription = string.Empty // Set below after query
                    })
                    .FirstOrDefaultAsync();

                if (course != null)
                {
                    course.TermDescription = TermCodeService.GetTermCodeDescription(int.Parse(course.TermCode));
                }

                return course;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error fetching course info for term {TermCode} and CRN {Crn}", LogSanitizer.SanitizeId(termCode), LogSanitizer.SanitizeId(crn));
                return null;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation fetching course info for term {TermCode} and CRN {Crn}", LogSanitizer.SanitizeId(termCode), LogSanitizer.SanitizeId(crn));
                return null;
            }
        }
    }
}
