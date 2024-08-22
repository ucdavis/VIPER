using Viper.Models.CTS;

namespace Viper.Models.CTS
{
    public class EvaluateesByInstance
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
    }
}
