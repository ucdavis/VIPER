namespace Viper.Areas.CTS.Models
{
    public class MilestoneLevelDto
    {
        public int MilestoneLevelId { get; set; }
        public int LevelId { get; set; }
        public string Level { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}