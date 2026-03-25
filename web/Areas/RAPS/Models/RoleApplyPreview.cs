namespace Viper.Areas.RAPS.Models
{
    public class RoleApplyPreview
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }
        public bool UserHasRole { get; set; } = false;
    }
}
