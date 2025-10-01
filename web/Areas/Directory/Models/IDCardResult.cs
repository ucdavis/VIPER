namespace Viper.Areas.Directory.Models
{
    public class IDCardResult
    {
        public string? Number { get; set; } = null;
        public string? DisplayName { get; set; } = null;
        public string? LastName { get; set; } = null;
        public string? Line2 { get; set; } = null;
        public string? StatusDescription { get; set; } = null;
        public DateTime? Applied { get; set; } = null;
        public DateTime? Issued { get; set; } = null;
        public string? DeactivatedReason { get; set; } = null;
        public DateTime? Deactivated { get; set; } = null;
    }
}
