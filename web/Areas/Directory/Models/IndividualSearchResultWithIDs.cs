using System.Runtime.Versioning;
using Viper.Models.AAUD;

namespace Viper.Areas.Directory.Models
{
    public class IndividualSearchResultWithIDs : IndividualSearchResult
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
                MothraId = aaudUser.MothraId;
                IamId = aaudUser.IamId;
                MivId = aaudUser.MivId;
            }
            else if (ldapUserContact != null)
            {
                Title = ldapUserContact.Title;
                Department = ldapUserContact.Ou;
                Phone = ldapUserContact.TelephoneNumber;
                Mobile = ldapUserContact.Mobile;
                UserName = ldapUserContact.Uid;
                PostalAddress = (ldapUserContact.PostalAddress ?? "").Replace("$", '\n'.ToString());
                UCDAffiliation = ldapUserContact.UcdPersonAffiliation;
                MothraId = ldapUserContact.MothraId;
                IamId = ldapUserContact.IamId;
                if (string.IsNullOrEmpty(DisplayFullName))
                {
                    DisplayFullName = ldapUserContact.DisplayName;
                }
                if (string.IsNullOrEmpty(Name))
                {
                    Name = ldapUserContact.DisplayName;
                }
            }
        }
    }
}
