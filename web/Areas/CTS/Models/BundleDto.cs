namespace Viper.Areas.CTS.Models
{
    public class BundleDto
    {
        public int? BundleId { get; set; }
        public required string Name { get; set; }
        public required bool Clinical { get; set; }
        public required bool Assessment { get; set; }
        public required bool Milestone { get; set; }
        public required int CompetencyCount { get; set; }

        //Only including roles here - other related objects can be found separately
        public IEnumerable<RoleDto> Roles { get; set; } = new List<RoleDto>();
    }
}
