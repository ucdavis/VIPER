namespace Viper.Areas.Directory.Models
{
    public class KeyResult
    {
        public string? AccessDescription { get; set; } = null!;
        public string? KeyNumber { get; set; } = null!;
        public string? CutNumber { get; set; } = null!;
        public DateOnly? Issued { get; set; } = null!;
        public string? IssuedBy { get; set; } = null!;
    }
}
