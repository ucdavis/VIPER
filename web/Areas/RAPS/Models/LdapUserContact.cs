using Newtonsoft.Json;
using System.DirectoryServices;
using System.Linq.Dynamic.Core;
using System.Runtime.Versioning;
using System.Text.Json;

namespace Viper.Areas.RAPS.Models
{
    [SupportedOSPlatform("windows")]
    public class LdapUserContact
    {
        public string accountexpires { get; set; } = null!;
        public string adspath { get; set; } = null!;
        public string badpasswordtime { get; set; } = null!;
        public string badpwdcount { get; set; } = null!;
        public string cn { get; set; } = null!;
        public string codepage { get; set; } = null!;
        public string company { get; set; } = null!;
        public string countrycode { get; set; } = null!;
        public string deliverandredirect { get; set; } = null!;
        public string department { get; set; } = null!;
        public string departmentnumber { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string displayname { get; set; } = null!;
        public string distinguishedname { get; set; } = null!;
        public string dscorepropagationdata { get; set; } = null!;
        public string extensionattribute10 { get; set; } = null!;
        public string extensionattribute11 { get; set; } = null!;
        public string extensionattribute12 { get; set; } = null!;
        public string extensionattribute13 { get; set; } = null!;
        public string extensionattribute14 { get; set; } = null!;
        public string extensionattribute15 { get; set; } = null!;
        public string extensionattribute5 { get; set; } = null!;
        public string extensionattribute6 { get; set; } = null!;
        public string extensionattribute7 { get; set; } = null!;
        public string extensionattribute8 { get; set; } = null!;
        public string extensionattribute9 { get; set; } = null!;
        public string gidnumber { get; set; } = null!;
        public string givenname { get; set; } = null!;
        public string instancetype { get; set; } = null!;
        public string lastlogoff { get; set; } = null!;
        public string lastlogon { get; set; } = null!;
        public string lastlogontimestamp { get; set; } = null!;
        public string legacyexchangedn { get; set; } = null!;
        public string lockouttime { get; set; } = null!;
        public string logoncount { get; set; } = null!;
        public string mail { get; set; } = null!;
        public string mailnickname { get; set; } = null!;
        public string memberof { get; set; } = null!;
        public string msexcharchiveguid { get; set; } = null!;
        public string msexcharchivename { get; set; } = null!;
        public string msexcharchivestatus { get; set; } = null!;
        public string msexchcomanagedobjectsbl { get; set; } = null!;
        public string msexchextensionattribute16 { get; set; } = null!;
        public string msexchextensionattribute17 { get; set; } = null!;
        public string msexchmailboxguid { get; set; } = null!;
        public string msexchpoliciesexcluded { get; set; } = null!;
        public string msexchrecipientdisplaytype { get; set; } = null!;
        public string msexchrecipienttypedetails { get; set; } = null!;
        public string msexchremoterecipienttype { get; set; } = null!;
        public string msexchsafesendershash { get; set; } = null!;
        public string msexchtextmessagingstate { get; set; } = null!;
        public string msexchumdtmfmap { get; set; } = null!;
        public string msexchuseraccountcontrol { get; set; } = null!;
        public string msexchversion { get; set; } = null!;
        public string msexchwhenmailboxcreated { get; set; } = null!;
        public string name { get; set; } = null!;
        public string objectcategory { get; set; } = null!;
        public string objectclass { get; set; } = null!;
        public string objectguid { get; set; } = null!;
        public string objectsid { get; set; } = null!;
        public string physicaldeliveryofficename { get; set; } = null!;
        public string primarygroupid { get; set; } = null!;
        public string proxyaddresses { get; set; } = null!;
        public string pwdlastset { get; set; } = null!;
        public string SamAccountName { get; set; } = null!;
        public string samaccounttype { get; set; } = null!;
        public string showinaddressbook { get; set; } = null!;
        public string sn { get; set; } = null!;
        public string targetaddress { get; set; } = null!;
        public string title { get; set; } = null!;
        public string uid { get; set; } = null!;
        public string uidnumber { get; set; } = null!;
        public string useraccountcontrol { get; set; } = null!;
        public string userprincipalname { get; set; } = null!;
        public string usnchanged { get; set; } = null!;
        public string usncreated { get; set; } = null!;
        public string whenchanged { get; set; } = null!;
        public string whencreated { get; set; } = null!;

        public string phone { get; set; } = null!;
        public string mobile { get; set; } = null!;
        public string username { get; set; } = null!;
        public string? description { get; set; } = null!;
        public string? internetencoding { get; set; } = null!;
        public string? mapirecipient { get; set; } = null!;
        public string? msexchblockedsendershash { get; set; } = null!;
        public string? samaccountname { get; set; } = null!;
        public string? streetaddress { get; set; } = null!;
        public string? telephonenumber { get; set; } = null!;
        public string? textencodedoraddress { get; set; } = null!;
        public string originalObject { get; set; } = null!;
        public LdapUserContact() { }
        public LdapUserContact(SearchResult? ldapSearchResult)
        {
            if (ldapSearchResult != null)
            {
                originalObject = System.Text.Json.JsonSerializer.Serialize(ldapSearchResult.Properties);
                foreach (System.Collections.DictionaryEntry prop in ldapSearchResult.Properties)
                {
                    if (prop.Value != null)
                    {
                        var v = ((ResultPropertyValueCollection)prop.Value);
                        switch (prop.Key.ToString())
                        {
                            case "accountexpires": accountexpires = v[0].ToString(); break;
                            case "adspath": adspath = v[0].ToString(); break;
                            case "badpasswordtime": badpasswordtime = v[0].ToString(); break;
                            case "badpwdcount": badpwdcount = v[0].ToString(); break;
                            case "cn": cn = v[0].ToString(); break;
                            case "codepage": codepage = v[0].ToString(); break;
                            case "company": company = v[0].ToString(); break;
                            case "countrycode": countrycode = v[0].ToString(); break;
                            case "deliverandredirect": deliverandredirect = v[0].ToString(); break;
                            case "department": department = v[0].ToString(); break;
                            case "departmentnumber": departmentnumber = v[0].ToString(); break;
                            case "description": Description = v[0].ToString(); break;
                            case "displayname": displayname = v[0].ToString(); break;
                            case "distinguishedname": distinguishedname = v[0].ToString(); break;
                            case "dscorepropagationdata": dscorepropagationdata = v[0].ToString(); break;
                            case "extensionattribute10": extensionattribute10 = v[0].ToString(); break;
                            case "extensionattribute11": extensionattribute11 = v[0].ToString(); break;
                            case "extensionattribute12": extensionattribute12 = v[0].ToString(); break;
                            case "extensionattribute13": extensionattribute13 = v[0].ToString(); break;
                            case "extensionattribute14": extensionattribute14 = v[0].ToString(); break;
                            case "extensionattribute15": extensionattribute15 = v[0].ToString(); break;
                            case "extensionattribute5": extensionattribute5 = v[0].ToString(); break;
                            case "extensionattribute6": extensionattribute6 = v[0].ToString(); break;
                            case "extensionattribute7": extensionattribute7 = v[0].ToString(); break;
                            case "extensionattribute8": extensionattribute8 = v[0].ToString(); break;
                            case "extensionattribute9": extensionattribute9 = v[0].ToString(); break;
                            case "gidnumber": gidnumber = v[0].ToString(); break;
                            case "givenname": givenname = v[0].ToString(); break;
                            case "instancetype": instancetype = v[0].ToString(); break;
                            case "lastlogoff": lastlogoff = v[0].ToString(); break;
                            case "lastlogon": lastlogon = v[0].ToString(); break;
                            case "lastlogontimestamp": lastlogontimestamp = v[0].ToString(); break;
                            case "legacyexchangedn": legacyexchangedn = v[0].ToString(); break;
                            case "lockouttime": lockouttime = v[0].ToString(); break;
                            case "logoncount": logoncount = v[0].ToString(); break;
                            case "mail": mail = v[0].ToString(); break;
                            case "mailnickname": mailnickname = v[0].ToString(); break;
                            case "msexcharchiveguid": msexcharchiveguid = v[0].ToString(); break;
                            case "msexcharchivename": msexcharchivename = v[0].ToString(); break;
                            case "msexcharchivestatus": msexcharchivestatus = v[0].ToString(); break;
                            case "msexchcomanagedobjectsbl": msexchcomanagedobjectsbl = v[0].ToString(); break;
                            case "msexchextensionattribute16": msexchextensionattribute16 = v[0].ToString(); break;
                            case "msexchextensionattribute17": msexchextensionattribute17 = v[0].ToString(); break;
                            case "msexchmailboxguid": msexchmailboxguid = v[0].ToString(); break;
                            case "msexchpoliciesexcluded": msexchpoliciesexcluded = v[0].ToString(); break;
                            case "msexchrecipientdisplaytype": msexchrecipientdisplaytype = v[0].ToString(); break;
                            case "msexchrecipienttypedetails": msexchrecipienttypedetails = v[0].ToString(); break;
                            case "msexchremoterecipienttype": msexchremoterecipienttype = v[0].ToString(); break;
                            case "msexchsafesendershash": msexchsafesendershash = v[0].ToString(); break;
                            case "msexchtextmessagingstate": msexchtextmessagingstate = v[0].ToString(); break;
                            case "msexchumdtmfmap": msexchumdtmfmap = v[0].ToString(); break;
                            case "msexchuseraccountcontrol": msexchuseraccountcontrol = v[0].ToString(); break;
                            case "msexchversion": msexchversion = v[0].ToString(); break;
                            case "msexchwhenmailboxcreated": msexchwhenmailboxcreated = v[0].ToString(); break;
                            case "name": name = v[0].ToString(); break;
                            case "objectcategory": objectcategory = v[0].ToString(); break;
                            case "objectclass": objectclass = v[0].ToString(); break;
                            case "objectguid": objectguid = v[0].ToString(); break;
                            case "objectsid": objectsid = v[0].ToString(); break;
                            case "physicaldeliveryofficename": physicaldeliveryofficename = v[0].ToString(); break;
                            case "primarygroupid": primarygroupid = v[0].ToString(); break;
                            case "proxyaddresses": proxyaddresses = v[0].ToString(); break;
                            case "pwdlastset": pwdlastset = v[0].ToString(); break;
                            case "samaccountname": SamAccountName = v[0].ToString(); break;
                            case "samaccounttype": samaccounttype = v[0].ToString(); break;
                            case "showinaddressbook": showinaddressbook = v[0].ToString(); break;
                            case "sn": sn = v[0].ToString(); break;
                            case "targetaddress": targetaddress = v[0].ToString(); break;
                            case "title": title = v[0].ToString(); break;
                            case "uid": uid = v[0].ToString(); break;
                            case "uidnumber": uidnumber = v[0].ToString(); break;
                            case "useraccountcontrol": useraccountcontrol = v[0].ToString(); break;
                            case "userprincipalname": userprincipalname = v[0].ToString(); break;
                            case "usnchanged": usnchanged = v[0].ToString(); break;
                            case "usncreated": usncreated = v[0].ToString(); break;
                            case "whenchanged": whenchanged = v[0].ToString(); break;
                            case "whencreated": whencreated = v[0].ToString(); break;
                            case "phone": phone = v[0].ToString(); break;
                            case "mobile": mobile = v[0].ToString(); break;
                            case "memberof":
                                List<string> groups = new();
                                foreach (string group in v)
                                {
                                    groups.Add(group);
                                }
                                memberof = string.Join(",", groups); break;
                            default: break;
                        }

                    }
                }
            }
        }
    }
}
