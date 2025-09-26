using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Requests
{
    /// <summary>
    /// Request model for setting or unsetting an instructor as primary evaluator
    /// </summary>
    public class SetPrimaryEvaluatorRequest
    {
        [Required(ErrorMessage = "IsPrimary flag is required")]
        public bool? IsPrimary { get; set; }

        /// <summary>
        /// Whether this week requires a primary evaluator (determined by frontend)
        /// Used for intelligent email notifications
        /// </summary>
        public bool RequiresPrimaryEvaluator { get; set; } = false;
    }
}
