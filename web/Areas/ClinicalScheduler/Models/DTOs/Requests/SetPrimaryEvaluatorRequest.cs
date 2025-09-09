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
    }
}
