namespace Viper.Areas.RAPS.Models
{
    public class VmacsResponse
    {
        public bool Success { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
        public int NumErrors { get; set; }
        public int NumSkippedUsers { get; set; }
        public int NumTotalUsers { get; set; }
        public int NumUsersWithAuthChanged { get; set; }
        public int NumUsersWithOptionChanged { get; set; }
        public int NumUsersWithPermChanged { get; set; }
    }
}
