using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Students.Models;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;

namespace Viper.Areas.Students.Services
{
    /// <summary>
    /// Service for retrieving course information for photo gallery
    /// </summary>
    public class CourseService : ICourseService
    {
        private readonly CoursesContext _coursesContext;
        private readonly ILogger<CourseService> _logger;
        private readonly TermCodeService _termCodeService;

        public CourseService(CoursesContext coursesContext, ILogger<CourseService> logger, TermCodeService termCodeService)
        {
            _coursesContext = coursesContext;
            _logger = logger;
            _termCodeService = termCodeService;
        }

        /// <summary>
        /// Get available courses for photo gallery, grouped by term
        /// </summary>
        public async Task<List<CoursesByTerm>> GetAvailableCoursesForPhotosAsync(string subjectCode = "VET")
        {
            try
            {
                // Calculate term range: current academic year + previous year
                var (startTerm, endTerm) = await _termCodeService.GetAcademicYearTermRangeAsync();

                // Query courses with enrolled students
                var courses = await _coursesContext.Baseinfos
                    .Where(b => b.BaseinfoSubjCode == subjectCode)
                    .Where(b => b.BaseinfoTermCode != null &&
                               string.Compare(b.BaseinfoTermCode, startTerm, StringComparison.Ordinal) >= 0 &&
                               string.Compare(b.BaseinfoTermCode, endTerm, StringComparison.Ordinal) <= 0)
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

                // Group by term and get descriptions, ordered by term code (most recent first)
                // Note: GetTermDescriptionAsync uses a static cache loaded on first access, so repeated calls are efficient after initial load
                var coursesByTerm = new List<CoursesByTerm>();
                foreach (var termGroup in courses.GroupBy(c => c.TermCode).OrderByDescending(g => g.Key))
                {
                    var termDescription = await _termCodeService.GetTermDescriptionAsync(termGroup.Key);
                    coursesByTerm.Add(new CoursesByTerm
                    {
                        TermCode = termGroup.Key,
                        TermDescription = termDescription,
                        Courses = termGroup.Select(c => new CourseInfo
                        {
                            TermCode = c.TermCode,
                            Crn = c.Crn,
                            SubjectCode = c.SubjectCode,
                            CourseNumber = c.CourseNumber,
                            Title = c.Title,
                            TermDescription = termDescription
                        }).ToList()
                    });
                }

                return coursesByTerm;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error fetching available courses for subject code {SubjectCode}", LogSanitizer.SanitizeString(subjectCode));
                return new List<CoursesByTerm>();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation fetching available courses for subject code {SubjectCode}", LogSanitizer.SanitizeString(subjectCode));
                return new List<CoursesByTerm>();
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid term code format while fetching available courses for subject code {SubjectCode}", LogSanitizer.SanitizeString(subjectCode));
                return new List<CoursesByTerm>();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Term code not found in database while fetching available courses for subject code {SubjectCode}", LogSanitizer.SanitizeString(subjectCode));
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
                        TermDescription = string.Empty // Set below after query
                    })
                    .FirstOrDefaultAsync();

                if (course != null)
                {
                    course.TermDescription = await _termCodeService.GetTermDescriptionAsync(course.TermCode);
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
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid term code format while fetching course info for term {TermCode} and CRN {Crn}", LogSanitizer.SanitizeId(termCode), LogSanitizer.SanitizeId(crn));
                return null;
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Term code not found in database while fetching course info for term {TermCode} and CRN {Crn}", LogSanitizer.SanitizeId(termCode), LogSanitizer.SanitizeId(crn));
                return null;
            }
        }
    }
}
