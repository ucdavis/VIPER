namespace Viper.Areas.RAPS.Models
{
    public class GroupMemberRole
    {
        public int RoleId { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateOnly? AddDate { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string ModBy { get; set; } = string.Empty;
        public DateOnly? ModDate { get; set; }
        public string ViewName { get; set; } = string.Empty;
    }
}
