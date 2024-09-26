namespace Viper.Areas.CTS.Models
{
    public class MilestoneDto
    {
        public int MilestoneId { get; set; }
        public string Name { get; set; } = null!;
        public int? CompetencyId { get; set; }
        public string? CompetencyName { get; set; }
    }
}
