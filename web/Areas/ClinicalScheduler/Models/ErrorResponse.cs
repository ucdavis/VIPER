namespace Viper.Areas.ClinicalScheduler.Models
{
    public class ErrorResponse
    {
        public string Code { get; set; }
        public string UserMessage { get; set; }
        public string CorrelationId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ErrorResponse(string code, string userMessage, string correlationId = null)
        {
            Code = code;
            UserMessage = userMessage;
            CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        }
    }

    public static class ErrorCodes
    {
        public const string ValidationError = "VALIDATION_ERROR";
        public const string PermissionDenied = "PERMISSION_DENIED";
        public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
        public const string DuplicateSchedule = "DUPLICATE_SCHEDULE";
        public const string SchedulingConflict = "SCHEDULING_CONFLICT";
        public const string InvalidOperation = "INVALID_OPERATION";
        public const string SystemError = "SYSTEM_ERROR";
    }

    public static class UserMessages
    {
        public const string GenericError = "An error occurred while processing your request. Please try again later.";
        public const string PermissionDenied = "You do not have permission to perform this action.";
        public const string ResourceNotFound = "The requested resource was not found.";
        public const string DuplicateSchedule = "This schedule assignment already exists. Please refresh the page and try again.";
        public const string InvalidWeeks = "One or more specified weeks are invalid or not available.";
        public const string InvalidPerson = "The specified person was not found in the system.";
        public const string InvalidRotation = "The specified rotation was not found in the system.";
    }
}