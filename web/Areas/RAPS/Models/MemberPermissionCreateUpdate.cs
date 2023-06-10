namespace Viper.Areas.RAPS.Models
{
    public class MemberPermissionCreateUpdate
    {
        public string MemberId { get; set; } = null!;
        public int PermissionId { get; set; }
        public byte Access { get; set; }
        public DateTime? StartDate { get; set; } = null!;
        public DateTime? EndDate { get; set; } = null!;
    }
}
