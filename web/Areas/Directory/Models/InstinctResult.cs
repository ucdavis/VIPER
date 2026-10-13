namespace Viper.Areas.Directory.Models
{
    public class InstinctResult
    {
        public bool Valid { get; set; }
        public string? InstinctId { get; set; }
        public bool IsActive { get; set; }
        public string? PasswordExpiresAt { get; set; }
        public string? Status { get; set; }
        public string? Username { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public string? ErrorMessage { get; set; }
    }
}
