namespace Viper.Areas.CTS.Models
{
    public class LegacyCompetencyDto
    {
        public int DvmCompetencyId { get; set; }
        public string DvmCompetencyName { get; set; } = null!;
        public int? DvmCompetencyParentId { get; set; }
        public bool DvmCompetencyActive { get; set; }

        public List<CompetencyDto> Competencies { get; set; } = new List<CompetencyDto>();
    }
}