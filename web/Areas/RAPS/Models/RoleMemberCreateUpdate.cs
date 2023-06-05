using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    public class RoleMemberCreateUpdate
    {
        public int RoleId { get; set; }
        public string? MemberId { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Comment { get; set; } = "";
    }
}
