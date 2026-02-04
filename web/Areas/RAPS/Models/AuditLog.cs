using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    public class AuditLog : TblLog
    {
        public string? Role { get; set; }
        public string? Permission { get; set; }
        public string? MemberName { get; set; }
        public string? ModByUserName { get; set; }
        //this is used by the history / revert tool to indicate that, after this change was made, the opposite change was made,
        //e.g. if this change was a role being added to a user, and afterwards the role was removed from the user, Undone = true
        public bool? Undone { get; set; } = null;
    }
}
