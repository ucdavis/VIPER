using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    public class EvaluateeWithEpaCompletion
    {
        public int EvaluateeId { get; set; }
        public int EvalId { get; set; }
        public int RotId { get; set; }
        public string Rotation { get; set; } = null!;
        public int ServiceId { get; set; }
        public string Service { get; set; } = null!;
        public int StartWeekId { get; set; }
        public DateTime StartDate { get; set; }
        public int EndWeekId { get; set; }
        public DateTime EndDate { get; set; }
        public string InstructorMothraId { get; set; } = null!;
        public int InstanceId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string MothraId { get; set; } = null!;
        public int PersonId { get; set; }
        public bool EpaDone { get; set; }

        public EvaluateeWithEpaCompletion() { }
        public EvaluateeWithEpaCompletion(EvaluateesByInstance e, bool epaDone = false)
        {
            EvaluateeId = e.EvaluateeId;
            EvalId = e.EvalId;
            RotId = e.RotId;
            Rotation = e.Rotation;
            ServiceId = e.ServiceId;
            Service = e.Service;
            StartWeekId = e.StartWeekId;
            StartDate = e.StartDate;
            EndWeekId = e.EndWeekId;
            EndDate = e.EndDate;
            InstructorMothraId = e.InstructorMothraId;
            InstanceId = e.InstanceId;
            FirstName = e.FirstName;
            LastName = e.LastName;
            MothraId = e.MothraId;
            PersonId = e.PersonId;
            EpaDone = epaDone;
        }
    }
}
