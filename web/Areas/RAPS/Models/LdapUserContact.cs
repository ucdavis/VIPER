using Newtonsoft.Json;
using System.DirectoryServices;
using System.Linq.Dynamic.Core;
using System.Runtime.Versioning;

namespace Viper.Areas.RAPS.Models
{
    [SupportedOSPlatform("windows")]
    public class LdapUserContact
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
        public string Phone { get; set; } = null!;
        public string Mobile { get; set; } = null!;
        public string Mail { get; set; } = null!;
        public string UserName { get; set; } = null!;

        public LdapUserContact() { }
        public LdapUserContact(SearchResult? ldapSearchResult)
        {
            if (ldapSearchResult != null)
            {
                foreach (System.Collections.DictionaryEntry prop in ldapSearchResult.Properties)
                {
                    if (prop.Value != null)
                    {
                        var v = ((ResultPropertyValueCollection)prop.Value);
                        switch (prop.Key.ToString())
                        {
                            case "samaccountname": SamAccountName = v[0].ToString(); break;
                            case "objectguid": ObjectGuid = (byte[])v[0]; break;
                            case "cn": Cn = v[0].ToString(); break;
                            case "canonicalname": CanonicalName = v[0].ToString(); break;
                            case "dn": Dn = v[0].ToString(); break;
                            case "distinguishedname": DistinguishedName = v[0].ToString(); break;
                            case "givenname": GivenName = v[0].ToString(); break;
                            case "sn": Sn = v[0].ToString(); break;
                            case "displayname": DisplayName = v[0].ToString(); break;
                            case "description": Description = v[0].ToString(); break;
                            case "userprincipalname": UserPrincipalName = v[0].ToString(); break;
                            case "title": Title = v[0].ToString(); break;
                            case "department": Department = v[0].ToString(); break;
                            case "uidNumber": UidNumber = v[0].ToString(); break;
                            case "telephonenumber": Phone = v[0].ToString(); break;
                            case "mobile": Mobile = v[0].ToString(); break;
                            case "uid": UserName = v[0].ToString(); break;
                            case "mail": Mail = v[0].ToString(); break;
                            case "memberof":
                                List<string> groups = new();
                                foreach (string group in v)
                                {
                                    groups.Add(group);
                                }
                                MemberOf = string.Join(",", groups); break;
                            default: break;
                        }

                    }
                }
            }
        }
    }
}
