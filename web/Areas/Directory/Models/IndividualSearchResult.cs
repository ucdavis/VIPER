using System.Runtime.Versioning;
using Viper.Areas.RAPS.Models;
using Viper.Models.AAUD;

namespace Viper.Areas.Directory.Models
{
    public class IndividualSearchResult
    {
        public string ClientId { get; set; } = string.Empty;

        public string MothraId { get; set; } = string.Empty;

        public string? LoginId { get; set; } = string.Empty;

        public string? MailId { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; } = string.Empty;

        public string DisplayLastName { get; set; } = string.Empty;

        public string DisplayFirstName { get; set; } = string.Empty;

        public string? DisplayMiddleName { get; set; } = string.Empty;

        public string DisplayFullName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool CurrentStudent { get; set; } = false;

        public bool FutureStudent { get; set; } = false;

        public bool CurrentEmployee { get; set; } = false;

        public bool FutureEmployee { get; set; } = false;

        public int? StudentTerm { get; set; } = null!;

        public int? EmployeeTerm { get; set; } = null!;

        public string? PpsId { get; set; } = string.Empty;

        public string? StudentPKey { get; set; } = string.Empty;

        public string? EmployeePKey { get; set; } = string.Empty;
        public string? Title { get; set; } = string.Empty;
        public string? Department { get; set; } = string.Empty;

        public int Current { get; set; } = -1;

        public int Future { get; set; } = -1;

        public string? IamId { get; set; } = string.Empty;

        public bool? Ross { get; set; } = null!;

        public DateTime? Added { get; set; } = null!;

        public string? Phone { get; set; } = null!;
        public string? Mobile { get; set; } = null!;
        public string? PostalAddress { get; set; } = null!;
        public string? UCDAffiliation { get; set; } = null!;
        public string? UserName { get; set; } = null!;
        public bool? SVM { get; set; } = null!;
        public string? originalObject { get; set; } = null!;

        public IndividualSearchResult()
        {

        }

        [SupportedOSPlatform("windows")]
        public IndividualSearchResult(AaudUser? aaudUser, LdapUserContact? ldapUserContact)
        {
            SVM = false;
            if (aaudUser != null) { 
                MothraId = aaudUser.MothraId;
                LoginId = aaudUser.LoginId;
                MailId = aaudUser.MailId;
                LastName = aaudUser.LastName;
                FirstName = aaudUser.FirstName;
                MiddleName = aaudUser.MiddleName;
                DisplayLastName = aaudUser.DisplayLastName;
                DisplayFirstName = aaudUser.DisplayFirstName;
                DisplayMiddleName = aaudUser.DisplayMiddleName;
                DisplayFullName = aaudUser.DisplayFullName;
                Name = aaudUser.DisplayFullName;
                CurrentStudent = aaudUser.CurrentStudent;
                FutureStudent = aaudUser.FutureStudent;
                CurrentEmployee = aaudUser.CurrentEmployee;
                FutureEmployee = aaudUser.FutureEmployee;
                StudentTerm = aaudUser.StudentTerm;
                EmployeeTerm = aaudUser.EmployeeTerm;
                PpsId = aaudUser.PpsId;
                StudentPKey = aaudUser.StudentPKey;
                EmployeePKey = aaudUser.EmployeePKey;
                Current = aaudUser.Current;
                Future = aaudUser.Future;
                IamId = aaudUser.IamId;
                Ross = aaudUser.Ross;
                SVM = true;
                Added = aaudUser.Added;
            }

            if (ldapUserContact != null)
            {
                originalObject = ldapUserContact.originalObject;
                Title = ldapUserContact.title;
                Department = ldapUserContact.department;
                Phone = ldapUserContact.telephonenumber;
                Mobile = ldapUserContact.mobile;
                UserName = ldapUserContact.uid;
                PostalAddress = (ldapUserContact.postaladdress ?? "").Replace("$", '\n'.ToString());
                UCDAffiliation = ldapUserContact.ucdpersonaffiliation;
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
