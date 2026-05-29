namespace Viper.Areas.Students.Models
{
    /// <summary>
    /// DTO for course information used in photo gallery
    /// </summary>
    public class CourseInfo
    {
        /// <summary>
        /// Term code in YYYYMM format (e.g., "202409")
        /// </summary>
        public string TermCode { get; set; } = string.Empty;

        /// <summary>
        /// Course Reference Number (CRN)
        /// </summary>
        public string Crn { get; set; } = string.Empty;

        /// <summary>
        /// Subject code (e.g., "VET")
        /// </summary>
        public string SubjectCode { get; set; } = string.Empty;

        /// <summary>
        /// Course number (e.g., "4201")
        /// </summary>
        public string CourseNumber { get; set; } = string.Empty;

        /// <summary>
        /// Course title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable term description (e.g., "Fall 2024")
        /// </summary>
        public string TermDescription { get; set; } = string.Empty;
    }
}
