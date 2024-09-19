namespace Viper.Areas.CTS.Models
{
    public class BundleCompetencyDto
    {
        public int BundleCompetencyId { get; set; }
        public int BundleId { get; set; }
        
        public int? RoleId { get; set; }
        public string? RoleName { get; set; } = null!;
        
        public IEnumerable<LevelDto> Levels { get; set; } = new List<LevelDto>();

        //comp info
        public int CompetencyId { get; set; }
        public string CompetencyNumber { get; set; } = null!;
        public string CompetencyName { get; set; } = null!;
        public string? Description { get; set; }
        public bool CanLinkToStudent { get; set; }

        //group and order
        public int? BundleCompetencyGroupId { get; set; }
        public int Order { get; set; }
    }
}
