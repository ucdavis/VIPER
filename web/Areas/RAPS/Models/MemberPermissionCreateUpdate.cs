using System.Text.Json.Serialization;
using Viper.Classes.Utilities;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    public class MemberPermissionCreateUpdate
    {
        public required string MemberId { get; set; }
        public required int PermissionId { get; set; }
        public required byte Access { get; set; }
        [JsonConverter(typeof(EmptyStringAsNullConverter<DateTime>))]
        public DateTime? StartDate { get; set; }
        [JsonConverter(typeof(EmptyStringAsNullConverter<DateTime>))]
        public DateTime? EndDate { get; set; }

        public static MemberPermissionCreateUpdate CreateMemberPermission(TblMemberPermission permissionMember)
        {
            return new MemberPermissionCreateUpdate
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
