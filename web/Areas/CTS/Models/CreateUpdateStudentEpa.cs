using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.CTS.Models
{
    public class CreateUpdateStudentEpa
    {
        public required int StudentId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "EPA is required.")]
        public required int EpaId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Level is required.")]
        public required int LevelId { get; set; }
        public string? Comment { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Service is required.")]
        public required int ServiceId { get; set; }
        public DateTime? EncounterDate { get; set; }
        public int? EncounterId { get; set; }
    }
}
