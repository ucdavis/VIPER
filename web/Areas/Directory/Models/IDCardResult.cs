namespace Viper.Areas.Directory.Models
{
    public class IDCardResult
    {
        public string? Number { get; set; }
        public string? DisplayName { get; set; }
        public string? LastName { get; set; }
        public string? Line2 { get; set; }
        public string? StatusDescription { get; set; }
        public DateTime? Applied { get; set; }
        public DateTime? Issued { get; set; }
        public string? DeactivatedReason { get; set; }
        public DateTime? Deactivated { get; set; }
    }
}

