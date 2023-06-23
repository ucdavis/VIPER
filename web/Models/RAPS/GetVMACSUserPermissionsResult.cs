namespace Viper.Models.RAPS
{
    public class GetVMACSUserPermissionsResult
    {
        public required string MemberId { get; set; }
        public string? PermissionIds { get; set; } = null!;
    }
}
