namespace Viper.Areas.RAPS.Models.Uinform
{
    public class ManagedGroup
    {
        public string? ObjectGuid { get; set; }
        public string? DisplayName { get; set; }
        public string? DistinguishedName { get; set; }
        public string? OwnedByGuid { get; set; }
        public string? ManagedByGuid { get; set; }
        public string? HasScopeOver { get; set; }
        public int? MaxMembers { get; set; }
        public string? ExtensionAttribute6 { get; set; }
        public string? SamAccountName { get; set; }
        public string? Description { get; set; }
    }
}
