using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.CTS.Models
{
    public class CreateUpdateStudentEpa
    {
        public int StudentId { get; set; }
        public int EpaId { get; set; }
        public int LevelId { get; set; }
        public string? Comment { get; set; }
        public int ServiceId { get; set; }
        public DateTime? EncounterDate { get; set; }
    }
}
