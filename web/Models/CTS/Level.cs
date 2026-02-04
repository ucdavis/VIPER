namespace Viper.Models.CTS
{
    public class Level
    {
        public int LevelId { get; set; }
        public string LevelName { get; set; } = null!;
        public string? Description { get; set; }
        public bool Active { get; set; }
        public int Order { get; set; }
        public bool Course { get; set; }
        public bool Clinical { get; set; }
        public bool Epa { get; set; }
        public bool Milestone { get; set; }
        public bool Dops { get; set; }
    }
}
