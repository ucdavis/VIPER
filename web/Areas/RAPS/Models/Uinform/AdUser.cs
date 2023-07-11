namespace Viper.Areas.RAPS.Models.Uinform
{
    public class AdUser
    {
        public string? ObjectGuid { get; set; }
        public string? DisplayName { get; set; }
        public string? SamAccountName { get; set; }
        public string? UserPrincipalName { get; set; }
        public string? DistinguishedName { get; set; }
        public List<string>? MemberOf { get; set; }
        public List<string>? MemberOfAll { get; set; }
        public string? Mail { get; set; }
        public bool IsActive { get; set; }
    }
}
