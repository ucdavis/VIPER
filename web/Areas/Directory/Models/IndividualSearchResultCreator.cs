using System.Runtime.Versioning;
using Viper.Models.AAUD;

namespace Viper.Areas.Directory.Models
{
    public class IndividualSearchResultCreator
    {
        [SupportedOSPlatform("windows")]
        public static IndividualSearchResult CreateIndividualSearchResult(AaudUser? aaudUser, LdapUserContact? ldapUserContact, bool includeDetail=false)
        {
            if (includeDetail)
            {
                IndividualSearchResultWithIDs indiv = new();
                if (aaudUser != null)
                {
                    AddAaudUser(indiv, aaudUser);
                    AddIds(indiv, aaudUser);
                }
                if (ldapUserContact != null)
                {
                    AddLdapContact(indiv, ldapUserContact);
                }

                return indiv;
            }
            else
            {
                IndividualSearchResult indiv = new();
                if (aaudUser != null)
                {
                    AddAaudUser(indiv, aaudUser);
                }
                if (ldapUserContact != null)
                {
                    AddLdapContact(indiv, ldapUserContact);
                }
                return indiv;
            }
        }

        private static void AddAaudUser(IndividualSearchResult indiv, AaudUser aaudUser)
        {
            indiv.MothraId = aaudUser.MothraId;
            indiv.LoginId = aaudUser.LoginId;
            indiv.MailId = aaudUser.MailId;
            indiv.LastName = aaudUser.LastName;
            indiv.FirstName = aaudUser.FirstName;
            indiv.MiddleName = aaudUser.MiddleName;
            indiv.DisplayLastName = aaudUser.DisplayLastName;
            indiv.DisplayFirstName = aaudUser.DisplayFirstName;
            indiv.DisplayMiddleName = aaudUser.DisplayMiddleName;
            indiv.DisplayFullName = aaudUser.DisplayFullName;
            indiv.Name = aaudUser.DisplayFullName;
            indiv.CurrentStudent = aaudUser.CurrentStudent;
            indiv.FutureStudent = aaudUser.FutureStudent;
            indiv.CurrentEmployee = aaudUser.CurrentEmployee;
            indiv.FutureEmployee = aaudUser.FutureEmployee;
            indiv.StudentTerm = aaudUser.StudentTerm;
            indiv.EmployeeTerm = aaudUser.EmployeeTerm;
            indiv.PpsId = aaudUser.PpsId;
            indiv.StudentPKey = aaudUser.StudentPKey;
            indiv.EmployeePKey = aaudUser.EmployeePKey;
            indiv.Current = aaudUser.Current;
            indiv.Future = aaudUser.Future;
            indiv.IamId = aaudUser.IamId;
            indiv.Ross = aaudUser.Ross;
            indiv.Added = aaudUser.Added;
        }

        private static void AddIds(IndividualSearchResultWithIDs indiv, AaudUser aaudUser)
        {
            indiv.SpridenId = aaudUser.SpridenId;
            indiv.Pidm = aaudUser.Pidm;
            indiv.EmployeeId = aaudUser.EmployeeId;
            indiv.VmacsId = aaudUser.VmacsId;
            indiv.UnexId = aaudUser.UnexId;
            indiv.MivId = aaudUser.MivId;
        }

        [SupportedOSPlatform("windows")]
        private static void AddLdapContact(IndividualSearchResult indiv, LdapUserContact ldapUserContact)
        {
            indiv.Title = ldapUserContact.Title;
            indiv.Department = ldapUserContact.Ou;
            indiv.Phone = ldapUserContact.TelephoneNumber;
            indiv.Mobile = ldapUserContact.Mobile;
            indiv.UserName = ldapUserContact.Uid;
            if(string.IsNullOrEmpty(indiv.DisplayFullName))
            {
                indiv.DisplayFullName = ldapUserContact.DisplayName;
            }
            if(string.IsNullOrEmpty(indiv.Name))
            {
                indiv.Name = ldapUserContact.DisplayName;
            }
        }
    }
}
