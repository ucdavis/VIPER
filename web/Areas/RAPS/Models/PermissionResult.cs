namespace Viper.Areas.RAPS.Models
{
    public class PermissionResult
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = null!;
        public string Source { get; set; } = null!;
        public bool Access { get; set; }
    }
}
