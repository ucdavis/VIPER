namespace Viper.Models.AAUD;

public partial class LdapFacilityLink
{
    public string LdapUcdPersonUuid { get; set; } = null!;

    public byte LdapSeqNumber { get; set; }

    public string? LdapName { get; set; }

    public string? LdapLastName { get; set; }

    public string? LdapFirstName { get; set; }

    public string? LdapEmail { get; set; }

    public string? LdapTitle { get; set; }

    public string? LdapDepartment { get; set; }

    public string? LdapStreet { get; set; }

    public string? LdapCity { get; set; }

    public string? LdapState { get; set; }

    public string? LdapPostalCode { get; set; }

    public string? LdapTelephoneNumber { get; set; }

    public string? LdapMobile { get; set; }

    public string? LdapPager { get; set; }

    public string? LdapDepartmentCode { get; set; }

    public string? LdapBldgKey { get; set; }
}
