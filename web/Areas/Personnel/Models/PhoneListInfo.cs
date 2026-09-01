namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// What a client needs to render a phone list before it fetches any rows: the display name
    /// plus the caller's own capabilities. Allows the client to render correctly
    /// based on the permissions that will be enforced on the back end.
    /// </summary>
    public class PhoneListInfo
    {
        public int PhoneListId { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public bool CanMaintain { get; set; }
        public bool CanViewDirectPhone { get; set; }
    }
}
