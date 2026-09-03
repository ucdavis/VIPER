using System.Text.Json.Serialization;
using Viper.Classes.Utilities;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    public class RoleMemberCreateUpdate
    {
        public required int RoleId { get; set; }
        public required string MemberId { get; set; }
        [JsonConverter(typeof(EmptyStringAsNullConverter<DateOnly>))]
        public DateOnly? StartDate { get; set; }
        [JsonConverter(typeof(EmptyStringAsNullConverter<DateOnly>))]
        public DateOnly? EndDate { get; set; }
        public string Comment { get; set; } = "";

        public static RoleMemberCreateUpdate CreateRoleMember(TblRoleMember roleMember)
        {
            return new RoleMemberCreateUpdate
            {
                MemberId = roleMember.MemberId,
                RoleId = roleMember.RoleId,
                StartDate = roleMember.StartDate != null ? DateOnly.FromDateTime((DateTime)roleMember.StartDate) : null,
                EndDate = roleMember.EndDate != null ? DateOnly.FromDateTime((DateTime)roleMember.EndDate) : null
            };
        }
    }

}
