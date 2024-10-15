namespace Viper.Models.CTS
{
    public class CompetencyMapping
    {
        public int CompetencyMappingId { get; set; }
        public int CompetencyId { get; set; }
        public int DvmCompetencyId { get; set; }

        public virtual Competency Competency { get; set; } = null!;
        public virtual LegacyCompetency LegacyCompetency { get; set; } = null!;
    }
}
