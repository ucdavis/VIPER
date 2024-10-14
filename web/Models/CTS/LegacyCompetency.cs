namespace Viper.Models.CTS
{
    public class LegacyCompetency
    {
        public int DvmCompetencyId { get; set; }
        public string DvmCompetencyName { get; set; } = null!;
        public int? DvmCompetencyParentId { get; set; }
        public bool DvmCompetencyActive { get; set; }

        public virtual List<CompetencyMapping> DvmCompetencyMapping { get; set; } = new List<CompetencyMapping>();
    }
}
