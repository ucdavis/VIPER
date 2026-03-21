using System.DirectoryServices.Protocols;
using System.Runtime.Versioning;

namespace Viper.Areas.RAPS.Models
{
    [SupportedOSPlatform("windows")]
    public class LdapUser
    {
        public string SamAccountName { get; set; } = null!;
        public byte[] ObjectGuid { get; set; } = null!;
        public string Cn { get; set; } = null!;
        public string CanonicalName { get; set; } = null!;
        public string Dn { get; set; } = null!;
        public string DistinguishedName { get; set; } = null!;
        public string GivenName { get; set; } = null!;
        public string Sn { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Description { get; set; } = "";
        public string UserPrincipalName { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Department { get; set; } = null!;
        public string UidNumber { get; set; } = null!;
        public string MemberOf { get; set; } = null!;

        public LdapUser() { }
        public LdapUser(SearchResultEntry? ldapSearchResult)
        {
            if (ldapSearchResult != null)
            {
                foreach (DirectoryAttribute attr in ldapSearchResult.Attributes.Values)
                {
                    var v = attr[0];
                    switch (attr.Name)
                    {
                        case "sAMAccountName": SamAccountName = v.ToString(); break;
                        case "objectGUID": ObjectGuid = (byte[])v; break;
                        case "cn": Cn = v.ToString(); break;
                        case "canonicalName": CanonicalName = v.ToString(); break;
                        case "dn": Dn = v.ToString(); break;
                        case "distinguishedName": DistinguishedName = v.ToString(); break;
                        case "givenName": GivenName = v.ToString(); break;
                        case "sn": Sn = v.ToString(); break;
                        case "displayName": DisplayName = v.ToString(); break;
                        case "description": Description = v.ToString(); break;
                        case "userPrincipalName": UserPrincipalName = v.ToString(); break;
                        case "title": Title = v.ToString(); break;
                        case "department": Department = v.ToString(); break;
                        case "uidNumber": UidNumber = v.ToString(); break;

                        case "memberOf":
                            List<string> groups = new();
                            foreach (string group in attr.GetValues(typeof(string)))
                            {
                                groups.Add(group);
                            }
                            MemberOf = string.Join(",", groups);
                            break;
                        default: break;
                    }
                }
            }
        }
    }
}
