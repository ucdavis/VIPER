namespace Viper.Areas.CTS.Models
{
    public class ClinicalScheduledStudent
    {
        public int PersonId { get; set; }
        public string LastName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; } = null;
        public string FullName { get; set; } = null!;
        public string? MothraId { get; set; } = null;
        public string? MailId { get; set; } = null;

        public DateTime? DateStart { get; set; }
        public DateTime? DateEnd { get; set; }
        public int? WeekId { get; set; }
        public string? SubjCode { get; set; } = null;
        public string? CrseNumb { get; set; } = null;
        public int? RotationId { get; set; }
        public string RotationName { get; set; } = null!;
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;
    }
}
