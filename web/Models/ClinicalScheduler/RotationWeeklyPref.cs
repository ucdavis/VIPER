using System.ComponentModel.DataAnnotations;

namespace Viper.Models.ClinicalScheduler
{
    public class RotationWeeklyPref
    {
        [Key]
        public int RotationWeeklyPrefsId { get; set; }
        public int RotId { get; set; }
        public int WeekId { get; set; }
        public int MinStudents { get; set; }
        public int MaxStudents { get; set; }
        public string ModifiedBy { get; set; } = null!;
        public DateTime ModifiedDate { get; set; }
        public bool Closed { get; set; }
        public bool Virtual { get; set; }
        public string? GradeMode { get; set; }
        public string? GradeModeLevel { get; set; }

        // Navigation properties
        public virtual Rotation Rotation { get; set; } = null!;
        public virtual Week Week { get; set; } = null!;
    }
}
