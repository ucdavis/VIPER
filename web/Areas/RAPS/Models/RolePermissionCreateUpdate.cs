namespace Viper.Areas.RAPS.Models
{
    public class RolePermissionCreateUpdate
    {
        public required int RoleId { get; set; }
        public required int PermissionId { get; set; }
        public required byte Access { get; set; }
    }
}
