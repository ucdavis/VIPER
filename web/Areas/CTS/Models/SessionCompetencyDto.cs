namespace Viper.Areas.CTS.Models
{
    public class SessionCompetencyDto
    {
        public int SessionCompetencyId { get; set; }
        public int Order { get; set; }

        public int CompetencyId { get; set; }
        public string CompetencyNumber { get; set; } = string.Empty;
        public string CompetencyName { get; set; } = string.Empty;
        public bool CanLinkToStudent { get; set; }

        //level and role
        public List<LevelIdAndNameDto> Levels { get; set; } = new List<LevelIdAndNameDto>();
        public int? RoleId { get; set; }
        public string? RoleName { get; set; } = string.Empty;
    }
}
