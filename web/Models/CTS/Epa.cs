namespace Viper.Models.CTS
{
    public class Epa
    {
        public required int EpaId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Order { get; set; }
        public required bool Active { get; set; }

        public virtual List<Service> Services { get; set; } = new List<Service>();
    }
}
