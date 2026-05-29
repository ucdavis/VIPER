using Viper.Models.VIPER;

namespace Viper.Models.CTS
{
    public class EncounterInstructor
    {
        public int EncounterInstructorId { get; set; }
        public int InstructorId { get; set; }
        public int EncounterId { get; set; }

        public virtual Person Instructor { get; set; } = null!;
        public virtual Encounter Encounter { get; set; } = null!;
    }
}
