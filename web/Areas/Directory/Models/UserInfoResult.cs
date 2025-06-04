using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Viper.Areas.Directory.Services;
using Viper.Models.AAUD;

namespace Viper.Areas.Directory.Models
{
    public class UserInfoResult
    {
        public string ClientId { get; set; } = string.Empty;
        public string MothraId { get; set; } = string.Empty;
        public string? LoginId { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? MailId { get; set; } = string.Empty;
        public string? EmailHost { get; set; } = string.Empty;
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
        public string? Nextel { get; set; } = null!;
        public string? LDPager { get; set; } = null!;
        public string? Mobile { get; set; } = null!;
        public string? PostalAddress { get; set; } = null!;
        public string? UCDAffiliation { get; set; } = null!;
        public string? UserName { get; set; } = null!;
        public bool? SVM { get; set; } = null!;
        public string? originalObject { get; set; } = null!;

        /*
         * 
         *    <td class="center">#cards.idcard_number#</td>
                    <td class="just">#cards.idcard_displayname# #cards.idcard_lastname# / #cards.idcard_line2#</td>
                    <td>#cards.statusdescription#</td>
                    <td>#showDate(cards.idcard_applieddate)#</td>
                    <td>#showDate(cards.idcard_issuedate)#</td>
                    <td>#cards.deactivatedreason#</td>
                    <td>#showDate(cards.idcard_deactivateddate)#</td>
         * 
         * 
         */



        public UserInfoResult() { }

        [SupportedOSPlatform("windows")]
        public UserInfoResult(AaudUser? aaudUser, LdapUserContact? ldapUserContact)
        {
            SVM = false;
            originalObject = "";
            if (aaudUser != null) {
                MothraId = aaudUser.MothraId;
                LoginId = aaudUser.LoginId;
                Email = aaudUser.MailId;
                MailId = aaudUser.MailId?.Split("@").First();
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
                UserName = aaudUser.LoginId;
            }
            if (ldapUserContact != null)
            {
                // displayname=Francisco Javier Acosta,ucdPersonAffiliation=student:undergraduate,ucdPersonUUID=02198608,mail=facosta@ucdavis.edu,ucdPersonPIDM=3817514,ucdStudentSID=921084299,sn=Acosta,givenName=Francisco,employeeNumber=10665044,ucdStudentLevel=Sophomore,title=STDT 2,ucdPersonIAMID=1000459572,uid=fcobay04,ou=STUDENT HOUSING DINING SVCS
                DisplayFullName = ldapUserContact.DisplayName;
                FirstName = ldapUserContact.GivenName;
                MiddleName = ldapUserContact.MiddleName;
                LastName = ldapUserContact.Sn;
                Title = ldapUserContact.Title;
                Department = ldapUserContact.Ou;
                Phone = ldapUserContact.TelephoneNumber;
                Mobile = ldapUserContact.Mobile;
                Email = ldapUserContact.Mail;
                MailId = ldapUserContact.Mail?.Split("@").First();
                UserName = ldapUserContact.Uid;
                PostalAddress = (ldapUserContact.PostalAddress ?? "").Replace("$", '\n'.ToString());
                UCDAffiliation = ldapUserContact.UcdPersonAffiliation;
                if (string.IsNullOrEmpty(DisplayFullName))
                {
                    DisplayFullName = ldapUserContact.DisplayName;
                }
                if (string.IsNullOrEmpty(Name))
                {
                    Name = ldapUserContact.DisplayName;
                }
                originalObject = ldapUserContact.originalObject;
            }


            using (var context = new Classes.SQLContext.AAUDContext())
            {
                if (MailId != null)
                {
                    var query = $"SELECT * FROM OPENQUERY(UCDMothra,'SELECT (USERPART || ''@'' || HOSTPART) AS USERATHOST FROM MOTHRA.MAILIDS WHERE MAILID = ''{MailId}'' AND MAILSTATUS = ''A'' AND MAILTYPE = ''P''')".ToString();
                    var results = context.Database.SqlQuery<string>(FormattableStringFactory.Create(query)).ToList();
                    foreach (var r in results)
                    {
                        EmailHost = r.Split("@").Last();
                    }
                }
            }
        }
    }
}
