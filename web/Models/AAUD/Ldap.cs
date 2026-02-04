namespace Viper.Models.AAUD;

public partial class Ldap
{
    public string LdapUcdPersonUuid { get; set; } = null!;

    public byte LdapSeqNumber { get; set; }

    public string? LdapName { get; set; }

    public string? LdapEmail { get; set; }

    public string? LdapTitle { get; set; }

    public string? LdapDepartment { get; set; }

    public string? LdapAddress { get; set; }

    public string? LdapTelephoneNumber { get; set; }

    public string? LdapMobile { get; set; }

    public string? LdapPager { get; set; }

    public string? LdapDepartmentCode { get; set; }
}
