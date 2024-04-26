using System.Runtime.Versioning;
using Viper.Areas.RAPS.Models;
using Viper.Models.AAUD;

namespace Viper.Areas.Directory.Models
{
    public class IndividualSearchResultWithIDs: IndividualSearchResult
    {
        public string? SpridenId { get; set; } = string.Empty;

        public string? Pidm { get; set; } = string.Empty;

        public string? EmployeeId { get; set; } = string.Empty;

        public int? VmacsId { get; set; } = null!;

        public string? VmcasId { get; set; } = string.Empty;

        public string? UnexId { get; set; } = string.Empty;

        public int? MivId { get; set; } = null!;

        public IndividualSearchResultWithIDs()
        {

        }

        [SupportedOSPlatform("windows")]
        public IndividualSearchResultWithIDs(AaudUser? aaudUser, LdapUserContact? ldapUserContact)
            : base(aaudUser, ldapUserContact)
        {
            if (aaudUser != null)
            {
                SpridenId = aaudUser.SpridenId;
                Pidm = aaudUser.Pidm;
                EmployeeId = aaudUser.EmployeeId;
                VmacsId = aaudUser.VmacsId;
                UnexId = aaudUser.UnexId;
                MivId = aaudUser.MivId;
            }
            else if(ldapUserContact != null)
            {
                Title = ldapUserContact.title;
                Department = ldapUserContact.department;
                Phone = ldapUserContact.telephonenumber;
                Mobile = ldapUserContact.mobile;
                UserName = ldapUserContact.uid;
                PostalAddress = (ldapUserContact.postaladdress ?? "").Replace("$", '\n'.ToString());
                UCDAffiliation = ldapUserContact.ucdpersonaffiliation;
                UCDPersonUUID = ldapUserContact.ucdpersonuuid;
                if (string.IsNullOrEmpty(DisplayFullName))
                {
                    DisplayFullName = ldapUserContact.displayname;
                }
                if (string.IsNullOrEmpty(Name))
                {
                    Name = ldapUserContact.displayname;
                }
            }
        }
    }
}
