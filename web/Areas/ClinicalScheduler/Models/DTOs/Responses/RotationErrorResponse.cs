namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// Standard error response for rotation-related operations
    /// </summary>
    public class RotationErrorResponse
    {
        /// <summary>
        /// Error message describing the issue
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// The rotation ID related to the error, if applicable
        /// </summary>
        public int? RotationId { get; set; }

        public RotationErrorResponse(string error, int? rotationId = null)
        {
            Error = error;
            RotationId = rotationId;
        }
    }
}
