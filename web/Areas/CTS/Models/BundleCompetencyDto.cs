namespace Viper.Areas.CTS.Models
{
    public class BundleCompetencyDto
    {
        public int BundleCompetencyId { get; set; }
        public int BundleId { get; set; }
        
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        
        public int CompetencyId { get; set; }
        public string CompetencyName { get; set; } = null!;
        public string? Description { get; set; }
        public bool CanLinkToStudent { get; set; }
        
        public int Order { get; set; }
    }
}
