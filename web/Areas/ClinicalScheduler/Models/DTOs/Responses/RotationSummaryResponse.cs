namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// Response containing summary of all rotations grouped by service
    /// </summary>
    public class RotationSummaryResponse
    {
        /// <summary>
        /// Total number of rotations across all services
        /// </summary>
        public int TotalRotations { get; set; }

        /// <summary>
        /// Number of services
        /// </summary>
        public int ServiceCount { get; set; }

        /// <summary>
        /// List of services with their rotations
        /// </summary>
        public List<ServiceSummaryDto> Services { get; set; } = new List<ServiceSummaryDto>();
    }
}
