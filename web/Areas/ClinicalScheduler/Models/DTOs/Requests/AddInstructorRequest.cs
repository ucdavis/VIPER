using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Requests
{
    /// <summary>
    /// Request model for adding an instructor to specific rotation weeks
    /// </summary>
    public class AddInstructorRequest : IValidatableObject
    {
        [Required(ErrorMessage = "MothraId is required")]
        public string MothraId { get; set; } = string.Empty;

        [Required(ErrorMessage = "RotationId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "RotationId must be greater than 0")]
        public int? RotationId { get; set; }

        [Required(ErrorMessage = "WeekIds are required")]
        [MinLength(1, ErrorMessage = "At least one week must be specified")]
        [MaxLength(52, ErrorMessage = "Cannot schedule more than 52 weeks at once")]
        public int[] WeekIds { get; set; } = Array.Empty<int>();

        [Required(ErrorMessage = "GradYear is required")]
        [Range(1, int.MaxValue, ErrorMessage = "GradYear must be greater than 0")]
        public int? GradYear { get; set; }

        public bool IsPrimaryEvaluator { get; set; } = false;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Ensure WeekIds are all positive and unique
            if (WeekIds != null && WeekIds.Length > 0)
            {
                if (WeekIds.Any(w => w <= 0))
                {
                    yield return new ValidationResult(
                        "All week IDs must be greater than 0",
                        new[] { nameof(WeekIds) });
                }

                if (WeekIds.Distinct().Count() != WeekIds.Length)
                {
                    yield return new ValidationResult(
                        "Week IDs must be unique",
                        new[] { nameof(WeekIds) });
                }
            }
        }
    }
}
