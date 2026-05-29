using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    public class GroupRole
    {
        public int RoleId { get; set; }
        public bool IsGroupRole { get; set; }
        public string Role { get; set; }

        public GroupRole(OuGroupRole role)
        {
            RoleId = role.RoleId;
            IsGroupRole = role.IsGroupRole;
            Role = role.Role.FriendlyName;
        }
    }
}
