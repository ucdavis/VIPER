namespace Viper.Models.IAM
{
    public class ContactInfo
    {
        public required string IamId { get; set; }
        public required string Email { get; set; }
        public string? HsEmail { get; set; }
        public string? CampusEmail { get; set; }
        public string? AddrStreet { get; set; }
        public string? AddrCity { get; set; }
        public string? AddrState { get; set; }
        public string? AddrZip { get; set; }
        public string? PostalAddress { get; set; }
        public string? WorkPhone { get; set; }
        public string? WorkCell { get; set; }
        public string? WorkPager { get; set; }
        public string? WorkFax { get; set; }
    }
}
