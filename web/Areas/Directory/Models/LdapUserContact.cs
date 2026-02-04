using System.DirectoryServices.Protocols;
using System.Runtime.Versioning;

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

        public LdapUserContact() { }

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
