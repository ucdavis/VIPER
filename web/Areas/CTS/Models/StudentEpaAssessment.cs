using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    public class StudentEpaAssessment : StudentAssessment
    {
        public override string AssessmentType { get; } = "EPA";

        public int StudentEpaId { get; set; }
        public string? Comment { get; set; }

        //Epa Info
        public int EpaId { get; set; }
        public string EpaName { get; set; } = string.Empty;

        //service info 
        public int? ServiceId { get; set; }
        public string? ServiceName { get; set; }

        public StudentEpaAssessment()
        {

        }

        public StudentEpaAssessment(StudentEpa epa) : base(epa.Level, epa.Encounter)
        {
            StudentEpaId = epa.StudentEpaId;
            Comment = epa.Comment;
            EpaId = epa.EpaId;
            EpaName = epa.Epa.Name;
            ServiceId = epa.Encounter.ServiceId;
            ServiceName = epa.Encounter.Service?.ServiceName;
        }
    }
}
