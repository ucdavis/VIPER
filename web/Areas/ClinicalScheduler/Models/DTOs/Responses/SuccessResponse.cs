namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// Generic success response model
    /// </summary>
    public class SuccessResponse
    {
        public string Message { get; set; }

        public SuccessResponse(string message)
        {
            Message = message;
        }
    }
}
