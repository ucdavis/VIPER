namespace Viper.Areas.CTS.Models
{
    public class MilestoneLevelAddUpdate
    {
        public required int LevelId { get; set; }
        public string Description { get; set; } = null!;
    }
}
