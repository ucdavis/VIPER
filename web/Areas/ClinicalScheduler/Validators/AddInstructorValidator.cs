using Viper.Areas.ClinicalScheduler.Models.DTOs.Requests;
using Viper.Areas.ClinicalScheduler.Services;

namespace Viper.Areas.ClinicalScheduler.Validators
{
    /// <summary>
    /// Validates instructor assignment requests for the Clinical Scheduler
    /// </summary>
    public class AddInstructorValidator
    {
        private readonly ILogger<AddInstructorValidator> _logger;
        private readonly IGradYearService _gradYearService;

        /// <summary>
        /// Initializes a new instance of the AddInstructorValidator class
        /// </summary>
        /// <param name="logger">Logger for validation operations</param>
        /// <param name="gradYearService">Service for grad year operations</param>
        public AddInstructorValidator(ILogger<AddInstructorValidator> logger, IGradYearService gradYearService)
        {
            _logger = logger;
            _gradYearService = gradYearService;
        }

        /// <summary>
        /// Validates an add instructor request for completeness and business rules
        /// </summary>
        /// <param name="request">The instructor assignment request to validate</param>
        /// <param name="correlationId">Correlation ID for request tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result containing success status and any error messages</returns>
        public async Task<ValidationResult> ValidateRequestAsync(AddInstructorRequest request, string correlationId, CancellationToken cancellationToken = default)
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
            if (!request.GradYear.HasValue)
            {
                errors.Add("Valid academic year is required.");
            }
            else
            {
                var requestedYear = request.GradYear.Value;
                var currentGradYear = await _gradYearService.GetCurrentGradYearAsync();
                var minYear = 2009;
                var maxYear = currentGradYear + 2;

                if (requestedYear < minYear || requestedYear > maxYear)
                {
                    errors.Add($"Academic year must be between {minYear} and {maxYear}.");
                }
                else if (requestedYear < currentGradYear)
                {
                    errors.Add($"Cannot modify schedules for past academic years. Current year is {currentGradYear}.");
                }
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

        /// <summary>
        /// Represents the result of a validation operation
        /// </summary>
        public class ValidationResult
        {
            /// <summary>
            /// Indicates whether the validation passed without errors
            /// </summary>
            public bool IsValid { get; set; }

            /// <summary>
            /// Primary error message for display
            /// </summary>
            public string? ErrorMessage { get; set; }

            /// <summary>
            /// Collection of all validation error messages
            /// </summary>
            public List<string> Errors { get; set; } = new List<string>();
        }
    }
}
