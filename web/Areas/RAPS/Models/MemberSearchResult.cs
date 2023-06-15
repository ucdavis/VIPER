namespace Viper.Areas.RAPS.Models
{
    public class MemberSearchResult
    {
        public string MemberId { get; set; } = string.Empty;
        public string MailId { get; set; } = string.Empty;
        public string LoginId { get; set; } = string.Empty;
        public string DisplayFirstName { get; set; } = string.Empty;
        public string DisplayLastName { get; set; } = string.Empty;
        public bool Current { get; set; }
        public string Department { get; set; } = string.Empty;
        public int CountRoles { get; set; }
        public int CountPermissions { get; set; }
    }
}
