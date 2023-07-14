using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    public class MemberPermissionCreateUpdate
    {
        public string MemberId { get; set; } = null!;
        public int PermissionId { get; set; }
        public byte Access { get; set; }
        public DateTime? StartDate { get; set; } = null!;
        public DateTime? EndDate { get; set; } = null!;

        public static MemberPermissionCreateUpdate CreateMemberPermission(TblMemberPermission permissionMember)
        {
            return new MemberPermissionCreateUpdate()
            {
                PermissionId = permissionMember.PermissionId,
                MemberId = permissionMember.MemberId,
                Access = permissionMember.Access,
                StartDate = permissionMember.StartDate,
                EndDate = permissionMember.EndDate
            };
        }
    }
}
