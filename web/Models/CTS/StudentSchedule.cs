using System.ComponentModel.DataAnnotations;

namespace Viper.Models.CTS
{
    public class StudentSchedule
    {
        public string LastName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; } = null;
        public string FullName { get; set; } = null!;
        public string MothraId { get; set; } = null!;
        public string? MailId { get; set; } = null;
        public int? Pidm { get; set; }
        public bool? NotGraded { get; set; }
        public bool? Incomplete { get; set; }
        public bool? MakeUp { get; set; }
        public bool? NotEnrolled { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public int WeekId { get; set; }
        public int RotationId { get; set; }
        public string RotationName { get; set; } = null!;
        public string Abbreviation { get; set; } = null!;
        public string? SubjCode { get; set; } = null;
        public string? CrseNumb { get; set; } = null;
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;
    }
}
