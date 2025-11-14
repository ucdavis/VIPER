using Viper.Areas.Students.Models;

namespace Viper.Areas.Students.Services
{
    /// <summary>
    /// Service for retrieving course information for photo gallery
    /// </summary>
    public interface ICourseService
    {
        /// <summary>
        /// Get available courses for the current term
        /// </summary>
        /// <param name="subjectCode">Subject code to filter courses (default: "VET")</param>
        /// <returns>List of courses for the current term</returns>
        Task<List<CourseInfo>> GetAvailableCoursesForPhotosAsync(string subjectCode = "VET");

        /// <summary>
        /// Get detailed information for a specific course
        /// </summary>
        /// <param name="termCode">Term code (6 digits)</param>
        /// <param name="crn">Course Reference Number</param>
        /// <returns>Course information or null if not found</returns>
        Task<CourseInfo?> GetCourseInfoAsync(string termCode, string crn);
    }
}
