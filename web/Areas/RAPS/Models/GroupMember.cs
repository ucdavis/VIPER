namespace Viper.Areas.RAPS.Models
{
    public class GroupMember
    {
        public string MemberId { get; set; } = string.Empty;
        public string MailId { get; set; } = string.Empty;
        public string LoginId { get; set; } = string.Empty;
        public string DisplayFirstName { get; set; } = string.Empty;
        public string DisplayLastName { get; set; } = string.Empty;
        public bool Current { get; set; }
        public bool? IsInGroup { get; set; } = null;
        public List<GroupMemberRole> Roles { get; set; } = new List<GroupMemberRole>();
        public string DisplayName { get
            {
                return DisplayLastName + ", " + DisplayFirstName;
            } 
        }
    }
}
