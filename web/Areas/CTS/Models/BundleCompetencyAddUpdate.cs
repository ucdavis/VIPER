namespace Viper.Areas.CTS.Models
{
    public class BundleCompetencyAddUpdate
    {
        public int? BundleCompetencyId { get; set; }
        public int BundleId { get; set; }
        public int CompetencyId { get; set; }
        public int Order { get; set; }
        public List<int> LevelIds { get; set; } = new List<int>();
        public int? RoleId { get; set; }
        public int? BundleCompetencyGroupId { get; set; }
    }
}
