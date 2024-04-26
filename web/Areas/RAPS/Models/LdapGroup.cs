using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.Runtime.Versioning;

namespace Viper.Areas.RAPS.Models
{
    [SupportedOSPlatform("windows")]
    public class LdapGroup
    {
        public string SamAccountName { get; set; } = null!;
        public byte[] ObjectGuid { get; set; } = null!;
        public string Cn { get; set; } = null!;
        public string CanonicalName { get; set; } = null!;
        public string Dn { get; set; } = null!;
        public string DistinguishedName { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string GroupType { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string ExtensionAttribute1 { get; set; } = null!;
        public string ExtensionAttribute2 { get; set; } = null!;
        public string ExtensionAttribute3 { get; set; } = null!;

        public LdapGroup() { }
        public LdapGroup(SearchResult? ldapSearchResult)
        {
            if(ldapSearchResult != null) {
                foreach (System.Collections.DictionaryEntry prop in ldapSearchResult.Properties)
                {
                    if(prop.Value != null)
                    {
                        var v = ((ResultPropertyValueCollection)prop.Value)[0];
                        switch (prop.Key.ToString())
                        {
                            case "samaccountname": SamAccountName = v.ToString(); break;
                            case "objectguid": ObjectGuid = (byte[])v; break;
                            case "cn": Cn = v.ToString(); break;
                            case "canonicalname": CanonicalName = v.ToString(); break;
                            case "dn": Dn = v.ToString(); break;
                            case "distinguishedname": DistinguishedName = v.ToString(); break;
                            case "displayname": DisplayName = v.ToString(); break;
                            case "grouptype": GroupType = v.ToString(); break;
                            case "description": Description = v.ToString(); break;
                            case "extensionattribute1": ExtensionAttribute1 = v.ToString(); break;
                            case "extensionattribute2": ExtensionAttribute2 = v.ToString(); break;
                            case "extensionattribute3": ExtensionAttribute3 = v.ToString(); break;
                            default: break;
                        }

                    }
                }
            }
        }

        public LdapGroup(SearchResultEntry entry)
        {
            foreach (DirectoryAttribute attr in entry.Attributes.Values)
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
                    case "displayName": DisplayName = v.ToString(); break;
                    case "groupType": GroupType = v.ToString(); break;
                    case "description": Description = v.ToString(); break;
                    case "extensionattribute1": ExtensionAttribute1 = v.ToString(); break;
                    case "extensionattribute2": ExtensionAttribute2 = v.ToString(); break;
                    case "extensionattribute3": ExtensionAttribute3 = v.ToString(); break;
                    default: break;
                }

            }
        }
    }
}
