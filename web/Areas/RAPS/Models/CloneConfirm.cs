namespace Viper.Areas.RAPS.Models
{
    public class CloneConfirm
    {
        public List<int> RoleIds { get; set; } = new List<int>();
        public List<int> PermissionIds { get; set; } = new List<int>();
    }
}
