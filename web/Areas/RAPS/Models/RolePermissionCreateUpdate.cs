namespace Viper.Areas.RAPS.Models
{
    public class RolePermissionCreateUpdate
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public byte Access { get; set; }
    }
}