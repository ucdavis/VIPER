namespace Viper.Areas.CTS.Models
{
    public class LevelDto
    {
        public int LevelId { get; set; }
        public string Level { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public bool Active { get; set; }
        public int Order { get; set; }
        public bool Course { get; set; }
        public bool Clinical { get; set; }
        public bool Assessment { get; set; }
        public bool Milestone { get; set; }
        public bool Epa { get; set; }
    }
}
