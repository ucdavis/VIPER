namespace Viper.Areas.CTS.Models
{
    public class BundleCompetencyAddUpdate
    {
        public int? BundleCompetencyId { get; set; }
        public required int BundleId { get; set; }
        public required int CompetencyId { get; set; }
        public required int Order { get; set; }
        public List<int> LevelIds { get; set; } = new List<int>();
        public int? RoleId { get; set; }
        public int? BundleCompetencyGroupId { get; set; }
    }
}
