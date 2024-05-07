using Viper.Areas.CTS.Models;
using Viper.Models.VIPER;

namespace Viper.Models.CTS
{
    public class Encounter
    {
        public int EncounterId { get; set; }
        public int StudentUserId { get; set; }
        public int EncounterType { get; set; }
        public DateTime EncounterDate { get; set; }
        public DateTime EnteredOn { get; set; }
        public int EnteredBy { get; set; }
        public int? RoleId {  get; set; }
        public int? OfferingId { get; set; }
        public int? ServiceId { get; set; }
        public int? PatientId { get; set; }
        public int? ClinicianId { get; set; }
        public string? VisitNumber { get; set; }
        public string? PresentingComplaint { get; set; }
        public string? Diagnosis { get; set; }
        public int? TermCode { get; set; }
        public bool Complete { get; set; }
        public string? StudentLevel { get; set; }
        public string? Comment { get; set; }
        public string? EditComment { get; set; }

        public virtual Person Student { get; set; } = null!;
        public virtual Person EnteredByPerson { get; set; } = null!;
        public virtual Person Clinician { get; set; } = null!;
        public virtual CourseSessionOffering? Offering { get; set; }
        public virtual Service? Service { get; set; }
        public virtual ICollection<EncounterInstructor> EncounterInstructors { get; set; } = new List<EncounterInstructor>();
    }
}
