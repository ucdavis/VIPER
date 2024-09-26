namespace Viper.Areas.CTS.Models
{
    public class MilestoneLevelDto
    {
        public int? MilestoneLevelId { get; set; }
        public int MilestoneId { get; set; }
        public int LevelId { get; set; }
        public string LevelName { get; set; } = null!;
        public int LevelOrder { get; set; }
        public string Description { get; set; } = null!;
    }
}