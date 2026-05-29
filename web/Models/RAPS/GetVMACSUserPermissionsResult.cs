namespace Viper.Models.RAPS
{
    public class GetVmacsUserPermissionsResult
    {
        public required string MemberId { get; set; }
        public string? PermissionIds { get; set; } = null!;
    }
}
