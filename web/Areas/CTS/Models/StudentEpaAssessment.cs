using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    public class StudentEpaAssessment : StudentAssessment
    {
        public override string AssessmentType { get; } = "EPA";

        //Epa Info
        public int EpaId { get; set; }
        public string EpaName { get; set; } = string.Empty;

        //service info 
        public int? ServiceId { get; set; }
        public string? ServiceName { get; set; }

        public StudentEpaAssessment()
        {

        }

        public StudentEpaAssessment(Encounter encounter) : base(encounter, encounter.Level)
        {
            if(encounter.Epa != null && encounter.EpaId != null)
            {
                EpaId = (int)encounter.EpaId;
                EpaName = encounter.Epa.Name;
            }
            if(encounter.Service != null)
            {
                ServiceId = encounter.ServiceId;
                ServiceName = encounter.Service?.ServiceName;
            }
        }
    }
}
