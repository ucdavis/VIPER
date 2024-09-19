namespace Viper.Areas.CTS.Models
{
    public class BundleCompetencyGroupDto
    {
        public int? BundleCompetencyGroupId { get; set; }
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        //public IEnumerable<BundleCompetencyDto> Competencies { get; set; } = new List<BundleCompetencyDto>();
    }
}
