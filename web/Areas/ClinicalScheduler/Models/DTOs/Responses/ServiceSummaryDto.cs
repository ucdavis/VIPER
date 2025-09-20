namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// Summary of a service with its rotations
    /// </summary>
    public class ServiceSummaryDto
    {
        /// <summary>
        /// Service ID
        /// </summary>
        public int ServiceId { get; set; }

        /// <summary>
        /// Service name
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Short name for the service
        /// </summary>
        public string? ShortName { get; set; }

        /// <summary>
        /// Number of rotations in this service
        /// </summary>
        public int RotationCount { get; set; }

        /// <summary>
        /// List of rotations in this service
        /// </summary>
        public List<RotationSummaryDto> Rotations { get; set; } = new List<RotationSummaryDto>();
    }

    /// <summary>
    /// Brief summary of a rotation
    /// </summary>
    public class RotationSummaryDto
    {
        /// <summary>
        /// Rotation ID
        /// </summary>
        public int RotId { get; set; }

        /// <summary>
        /// Rotation name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Rotation abbreviation
        /// </summary>
        public string? Abbreviation { get; set; }
    }
}
