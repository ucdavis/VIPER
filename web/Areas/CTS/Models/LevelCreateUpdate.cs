namespace Viper.Areas.CTS.Models
{
    public class LevelCreateUpdate
    {
        public int? LevelId { get; set; }
        public required string LevelName { get; set; }
        public string? Description { get; set; }
        public required bool Active { get; set; }
        public required int Order { get; set; }
        public required bool Course { get; set; }
        public required bool Clinical { get; set; }
        public required bool Epa { get; set; }
        public required bool Milestone { get; set; }
        public required bool Dops { get; set; }
    }
}
