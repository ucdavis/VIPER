namespace Viper.Areas.RAPS.Models
{
    public class RoleCreateUpdate
    {
        public int? RoleId { get; set; }
        public string Role { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public int Application { get; set; } = 0;
        public string? ViewName { get; set; } = null!;
    }
}
