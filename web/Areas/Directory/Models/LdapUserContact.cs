using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;

namespace Viper.Areas.Directory.Models
{
    [SupportedOSPlatform("windows")]
    public class LdapUserContact
    {
        public string Uid { get; set; } = null!;
        public string SamAccountName { get; set; } = null!;
        public string Ou { get; set; } = null!;
        public string Sn { get; set; } = null!;
        public string GivenName { get; set; } = null!;
        public string MiddleName { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string EduPersonNickname { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string PostalAddress { get; set; } = null!;
        public string TelephoneNumber { get; set; } = null!;
        public string Mobile { get; set; } = null!;
        public string Mail { get; set; } = null!;
        public string UcdStudentLevel { get; set; } = null!;
        public string UcdStudentSid { get; set; } = null!;
        public string UcdPersonPidm { get; set; } = null!;
        public string EmployeeNumber { get; set; } = null!;
        public string MothraId { get; set; } = null!;
        public string IamId { get; set; } = null!;
        public string UcdPersonAffiliation { get; set; } = null!;
        public string originalObject { get; set; } = null!;

        /*
            
        public string? middlename { get; set; } = null!;
        public string? eduPersonNickname { get; set; } = null!;
        public string? ucdStudentLevel { get; set; } = null!;
        public string? ucdPersonIAMID { get; set; } = null!;
        public string? ucdPersonUUID { get; set; } = null!;
        public string? ucdStudentSID { get; set; } = null!;
        public string? ucdPersonPIDM { get; set; } = null!;
        public string? employeeNumber { get; set; } = null!;
        public string? ucdPersonAffiliation { get; set; } = null!;

        public string? accountexpires { get; set; } = null!;
        public string? adspath { get; set; } = null!;
        public string? badpasswordtime { get; set; } = null!;
        public string? badpwdcount { get; set; } = null!;
        public string? cn { get; set; } = null!;
        public string? codepage { get; set; } = null!;
        public string? company { get; set; } = null!;
        public string? countrycode { get; set; } = null!;
        public string? deliverandredirect { get; set; } = null!;
        public string? department { get; set; } = null!;
        public string? departmentnumber { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? displayname { get; set; } = null!;
        public string? distinguishedname { get; set; } = null!;
        public string? dscorepropagationdata { get; set; } = null!;
        public string? edupersonaffiliation { get; set; } = null!;
        public string? edupersonprincipalname { get; set; } = null!;
        public string? employeenumber { get; set; } = null!;
        public string? extensionattribute10 { get; set; } = null!;
        public string? extensionattribute11 { get; set; } = null!;
        public string? extensionattribute12 { get; set; } = null!;
        public string? extensionattribute13 { get; set; } = null!;
        public string? extensionattribute14 { get; set; } = null!;
        public string? extensionattribute15 { get; set; } = null!;
        public string? extensionattribute5 { get; set; } = null!;
        public string? extensionattribute6 { get; set; } = null!;
        public string? extensionattribute7 { get; set; } = null!;
        public string? extensionattribute8 { get; set; } = null!;
        public string? extensionattribute9 { get; set; } = null!;
        public string? gidnumber { get; set; } = null!;
        public string? givenname { get; set; } = null!;
        public string? instancetype { get; set; } = null!;
        public string? internetencoding { get; set; } = null!;
        public string? l { get; set; } = null!;
        public string? lastlogoff { get; set; } = null!;
        public string? lastlogon { get; set; } = null!;
        public string? lastlogontimestamp { get; set; } = null!;
        public string? legacyexchangedn { get; set; } = null!;
        public string? lockouttime { get; set; } = null!;
        public string? logoncount { get; set; } = null!;
        public string? mail { get; set; } = null!;
        public string? mailnickname { get; set; } = null!;
        public string? mapirecipient { get; set; } = null!;
        public string? memberof { get; set; } = null!;
        public string? mobile { get; set; } = null!;
        public string? msexcharchiveguid { get; set; } = null!;
        public string? msexcharchivename { get; set; } = null!;
        public string? msexcharchivestatus { get; set; } = null!;
        public string? msexchblockedsendershash { get; set; } = null!;
        public string? msexchcomanagedobjectsbl { get; set; } = null!;
        public string? msexchextensionattribute16 { get; set; } = null!;
        public string? msexchextensionattribute17 { get; set; } = null!;
        public string? msexchmailboxguid { get; set; } = null!;
        public string? msexchpoliciesexcluded { get; set; } = null!;
        public string? msexchrecipientdisplaytype { get; set; } = null!;
        public string? msexchrecipienttypedetails { get; set; } = null!;
        public string? msexchremoterecipienttype { get; set; } = null!;
        public string? msexchsafesendershash { get; set; } = null!;
        public string? msexchtextmessagingstate { get; set; } = null!;
        public string? msexchumdtmfmap { get; set; } = null!;
        public string? msexchuseraccountcontrol { get; set; } = null!;
        public string? msexchversion { get; set; } = null!;
        public string? msexchwhenmailboxcreated { get; set; } = null!;
        public string? name { get; set; } = null!;
        public string? objectcategory { get; set; } = null!;
        public string? objectclass { get; set; } = null!;
        public string? objectguid { get; set; } = null!;
        public string? objectsid { get; set; } = null!;
        public string? ou { get; set; } = null!;
        public string? pager { get; set; } = null!;
        public string? phone { get; set; } = null!;
        public string? physicaldeliveryofficename { get; set; } = null!;
        public string? postaladdress { get; set; } = null!;
        public string? postalcode { get; set; } = null!;
        public string? primarygroupid { get; set; } = null!;
        public string? proxyaddresses { get; set; } = null!;
        public string? pwdlastset { get; set; } = null!;
        public string SamAccountName { get; set; } = null!;
        public string? samaccounttype { get; set; } = null!;
        public string? showinaddressbook { get; set; } = null!;
        public string surName { get; set; } = null!;
        public string? st { get; set; } = null!;
        public string? street { get; set; } = null!;
        public string? streetaddress { get; set; } = null!;
        public string? targetaddress { get; set; } = null!;
        public string? telephonenumber { get; set; } = null!;
        public string? textencodedoraddress { get; set; } = null!;
        public string? title { get; set; } = null!;
        public string? ucdappointmentdepartmentcode { get; set; } = null!;
        public string? ucdappointmenttitlecode { get; set; } = null!;
        public string? ucdpersonaffiliation { get; set; } = null!;
        public string? ucdpersoniamid { get; set; } = null!;
        public string? ucdpersonnetid { get; set; } = null!;
        public string? ucdpersonpidm { get; set; } = null!;
        public string? ucdpersonppsid { get; set; } = null!;
        public string? ucdpersonuuid { get; set; } = null!;
        public string? ucdpublishitemflag { get; set; } = null!;
        public string? ucdstudentsid { get; set; } = null!;
        public string? uid { get; set; } = null!;
        public string? uidnumber { get; set; } = null!;
        public string? useraccountcontrol { get; set; } = null!;
        public string? username { get; set; } = null!;
        public string? userprincipalname { get; set; } = null!;
        public string? usnchanged { get; set; } = null!;
        public string? usncreated { get; set; } = null!;
        public string? whenchanged { get; set; } = null!;
        public string? whencreated { get; set; } = null!;
        public string originalObject { get; set; } = null!;

        */

        public LdapUserContact() { }

        /*
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
                            case "Description": Description = v[0].ToString(); break;
                            case "description": Description = v[0].ToString(); break;
                            case "displayname": displayname = v[0].ToString(); break;
                            case "distinguishedname": distinguishedname = v[0].ToString(); break;
                            case "dscorepropagationdata": dscorepropagationdata = v[0].ToString(); break;
                            case "edupersonaffiliation": edupersonaffiliation = v[0].ToString(); break;
                            case "edupersonprincipalname": edupersonprincipalname = v[0].ToString(); break;
                            case "employeenumber": employeenumber = v[0].ToString(); break;
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
                            case "internetencoding": internetencoding = v[0].ToString(); break;
                            case "l": l = v[0].ToString(); break;
                            case "lastlogoff": lastlogoff = v[0].ToString(); break;
                            case "lastlogon": lastlogon = v[0].ToString(); break;
                            case "lastlogontimestamp": lastlogontimestamp = v[0].ToString(); break;
                            case "legacyexchangedn": legacyexchangedn = v[0].ToString(); break;
                            case "lockouttime": lockouttime = v[0].ToString(); break;
                            case "logoncount": logoncount = v[0].ToString(); break;
                            case "mail": mail = v[0].ToString(); break;
                            case "mailnickname": mailnickname = v[0].ToString(); break;
                            case "mapirecipient": mapirecipient = v[0].ToString(); break;
                            case "mobile": mobile = v[0].ToString(); break;
                            case "msexcharchiveguid": msexcharchiveguid = v[0].ToString(); break;
                            case "msexcharchivename": msexcharchivename = v[0].ToString(); break;
                            case "msexcharchivestatus": msexcharchivestatus = v[0].ToString(); break;
                            case "msexchblockedsendershash": msexchblockedsendershash = v[0].ToString(); break;
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
                            case "ou": ou = v[0].ToString(); break;
                            case "pager": pager = v[0].ToString(); break;
                            case "phone": phone = v[0].ToString(); break;
                            case "physicaldeliveryofficename": physicaldeliveryofficename = v[0].ToString(); break;
                            case "postaladdress": postaladdress = v[0].ToString(); break;
                            case "postalcode": postalcode = v[0].ToString(); break;
                            case "primarygroupid": primarygroupid = v[0].ToString(); break;
                            case "proxyaddresses": proxyaddresses = v[0].ToString(); break;
                            case "pwdlastset": pwdlastset = v[0].ToString(); break;
                            case "SamAccountName": SamAccountName = v[0].ToString(); break;
                            case "samaccountname": SamAccountName = v[0].ToString(); break;
                            case "samaccounttype": samaccounttype = v[0].ToString(); break;
                            case "showinaddressbook": showinaddressbook = v[0].ToString(); break;
                            case "sn": surName = v[0].ToString(); break;
                            case "st": st = v[0].ToString(); break;
                            case "street": street = v[0].ToString(); break;
                            case "streetaddress": streetaddress = v[0].ToString(); break;
                            case "targetaddress": targetaddress = v[0].ToString(); break;
                            case "telephonenumber": telephonenumber = v[0].ToString(); break;
                            case "textencodedoraddress": textencodedoraddress = v[0].ToString(); break;
                            case "title": title = v[0].ToString(); break;
                            case "ucdappointmentdepartmentcode": ucdappointmentdepartmentcode = v[0].ToString(); break;
                            case "ucdappointmenttitlecode": ucdappointmenttitlecode = v[0].ToString(); break;
                            case "ucdpersonaffiliation": ucdpersonaffiliation = v[0].ToString(); break;
                            case "ucdpersoniamid": ucdpersoniamid = v[0].ToString(); break;
                            case "ucdpersonnetid": ucdpersonnetid = v[0].ToString(); break;
                            case "ucdpersonpidm": ucdpersonpidm = v[0].ToString(); break;
                            case "ucdpersonppsid": ucdpersonppsid = v[0].ToString(); break;
                            case "ucdpersonuuid": ucdpersonuuid = v[0].ToString(); break;
                            case "ucdpublishitemflag": ucdpublishitemflag = v[0].ToString(); break;
                            case "ucdstudentsid": ucdstudentsid = v[0].ToString(); break;
                            case "uid": uid = v[0].ToString(); break;
                            case "uidnumber": uidnumber = v[0].ToString(); break;
                            case "useraccountcontrol": useraccountcontrol = v[0].ToString(); break;
                            case "username": username = v[0].ToString(); break;
                            case "userprincipalname": userprincipalname = v[0].ToString(); break;
                            case "usnchanged": usnchanged = v[0].ToString(); break;
                            case "usncreated": usncreated = v[0].ToString(); break;
                            case "whenchanged": whenchanged = v[0].ToString(); break;
                            case "whencreated": whencreated = v[0].ToString(); break;
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
        */

        public LdapUserContact(SearchResultEntry entry)
        {
            var or = new List<string>();
            foreach (DirectoryAttribute attr in entry.Attributes.Values)
            {
                var v = attr[0];
                or.Add(attr.Name + "=" + v.ToString());
                switch (attr.Name)
                {
                    case "uid": Uid = v.ToString(); break;
                    case "sAMAccountName": SamAccountName = v.ToString(); break;
                    case "sn": Sn = v.ToString(); break;
                    case "ou": Ou = v.ToString(); break;

                    case "givenName": GivenName = v.ToString(); break;
                    case "middleName": MiddleName = v.ToString(); break;
                    case "displayname": DisplayName = v.ToString(); break;
                    case "eduPersonNickname": EduPersonNickname = v.ToString(); break;

                    case "title": Title = v.ToString(); break;
                    case "postalAddress": PostalAddress = v.ToString(); break;
                    case "telephoneNumber": TelephoneNumber = v.ToString(); break;
                    case "mobile": Mobile = v.ToString(); break;
                    case "mail": Mail = v.ToString(); break;

                    case "employeeNumber": EmployeeNumber = v.ToString(); break;
                    case "ucdStudentLevel": UcdStudentLevel = v.ToString(); break;
                    case "ucdStudentSID": UcdStudentSid = v.ToString(); break;
                    case "ucdPersonPIDM": UcdPersonPidm = v.ToString(); break;
                    case "ucdPersonUUID": MothraId = v.ToString(); break;
                    case "ucdPersonIAMID": IamId = v.ToString(); break;
                    case "ucdPersonAffiliation": UcdPersonAffiliation = v.ToString(); break;

                    default: break;
                }
            }
            originalObject = string.Join(",", or);
        }
    }
}
