using Viper.Areas.ClinicalScheduler.Models.DTOs.Requests;

namespace Viper.Areas.ClinicalScheduler.Validators
{
    public class AddInstructorValidator
    {
        private readonly ILogger<AddInstructorValidator> _logger;

        public AddInstructorValidator(ILogger<AddInstructorValidator> logger)
        {
            _logger = logger;
        }

        public ValidationResult ValidateRequest(AddInstructorRequest request, string correlationId)
        {
            var mothraId = request.MothraId?.Trim();
            var errors = new List<string>();

            // Validate MothraId
            if (string.IsNullOrWhiteSpace(mothraId))
            {
                errors.Add("Instructor ID is required.");
            }
            else if (mothraId.Length > 50)
            {
                errors.Add("Instructor ID must be 50 characters or less.");
            }

            // Validate RotationId
            if (!request.RotationId.HasValue || request.RotationId.Value <= 0)
            {
                errors.Add("Valid rotation selection is required.");
            }

            // Validate GradYear
            if (!request.GradYear.HasValue || request.GradYear.Value < 1900 || request.GradYear.Value > 2100)
            {
                errors.Add("Valid academic year is required.");
            }

            // Validate WeekIds
            if (request.WeekIds == null || request.WeekIds.Length == 0)
            {
                errors.Add("At least one week must be selected.");
            }
            else if (request.WeekIds.Any(id => id <= 0))
            {
                errors.Add("All week IDs must be valid positive numbers.");
            }

            if (errors.Any())
            {
                _logger.LogWarning("Model validation failed (CorrelationId: {CorrelationId}): {Errors}",
                    correlationId, string.Join("; ", errors));

                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = errors.FirstOrDefault() ?? "Please check your input and try again.",
                    Errors = errors
                };
            }

            return new ValidationResult { IsValid = true };
        }

        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string? ErrorMessage { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }
    }
}
