using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Viper.Areas.Directory.Services;
using Viper.Areas.Students.Models;
using Viper.Models.AAUD;
using Viper.Models.PPS;
using Viper.Models.VIPER;

namespace Viper.Areas.Directory.Models
{
    public class UserInfoResult
    {
        public string ClientId { get; set; } = null!;
        public string MothraId { get; set; } = null!;
        public string SpridenId { get; set; } = null!;
        public string LoginId { get; set; } = null!;
        public string EmployeeId { get; set; } = null!;
        public string StudentId { get; set; } = null!;
        public string IamId { get; set; } = null!;
        public int? VmacsId { get; set; } = null!;
        public string MailId { get; set; } = null!;
        public int? MivId { get; set; } = null!;
        public string Pidm { get; set; } = null!;
        public bool IamIdMismatch { get; set; } = false;


        public string? AltPhoto { get; set; } = null!;
        public string? Email { get; set; } = null!;
        public string? EmailHost { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; } = null!;
        public string DisplayLastName { get; set; } = null!;
        public string DisplayFirstName { get; set; } = null!;
        public string? DisplayMiddleName { get; set; } = null!;
        public string? DisplayFullName { get; set; } = null!;
        public string? Name { get; set; } = null!;
        public bool CurrentStudent { get; set; } = false;
        public bool FutureStudent { get; set; } = false;
        public bool CurrentEmployee { get; set; } = false;
        public bool FutureEmployee { get; set; } = false;

        public string? EmployeePrimaryTitle { get; set; } = null!;
        public string? EmployeeSchoolDivision { get; set; } = null!;
        public string? EmployeeStatus { get; set; } = null!;
        public int? EmployeeTerm { get; set; } = null!;
        public string? EmployeeHomeDepartment { get; set; } = null!;
        public string? EmployeeEffortHomeDepartment { get; set; } = null!;
        public string? EmployeeTeachingHomeDepartment { get; set; } = null!;
        public string? EmployeeTeachingPercentFulltime { get; set; } = null!;
        public string? StudentPriorName { get; set; } = null!;
        public string? StudentBannerId { get; set; } = null!;
        public bool StudentConfidential { get; set; } = false;
        public string? StudentStatus { get; set; } = null!;
        public string? StudentPrimaryMajor { get; set; } = null!;
        public string? StudentAllMajors { get; set; } = null!;
        public string? StudentRegistrationStatus { get; set; } = null!;
        public string? StudentClassLevel { get; set; } = null!;
        public string? StudentClassOf { get; set; } = null!;
        public int? StudentTerm { get; set; } = null!;

        public string? PPSId { get; set; } = null!;
        public string? OFullName { get; set; } = null!;
        public bool IsEmployee { get; set; } = false;
        public bool IsHSEmployee { get; set; } = false;
        public bool IsFaculty { get; set; } = false;
        public bool IsStudent { get; set; } = false;
        public bool IsStaff { get; set; } = false;
        public bool IsExternal { get; set; } = false;
        public string? AssociationsTitle { get; set; } = null!;
        public string? AssociationsTitleCode { get; set; } = null!;
        public string? AssociationsDepartment { get; set; } = null!;
        public string? AssociationsDepartmentCode { get; set; } = null!;
        public string? AssociationsAdminDepartment { get; set; } = null!;
        public string? AssociationsAdminDepartmentAbbrev { get; set; } = null!;
        public string? AssociationsAdminDepartmentCode { get; set; } = null!;
        public string? AssociationsAppointmentDepartment { get; set; } = null!;
        public string? AssociationsAppointmentDepartmentAbbrev { get; set; } = null!;
        public string? AssociationsAppointmentDepartmentCode { get; set; } = null!;
        public string? AssociationsPositionType { get; set; } = null!;
        public string? AssociationsEmployeeClass { get; set; } = null!;
        public float? AssociationsPercentFulltime { get; set; } = null!;
        public DateTime? AssociationsStart { get; set; } = null!;
        public DateTime? AssociationsEnd { get; set; } = null!;
        public List<string> UCPathFlags { get; set; } = null!;
        public string? UCPathJobCode { get; set; } = null!;
        public string? UCPathJobDescription { get; set; } = null!;
        public string? UCPathDepartmentId { get; set; } = null!;
        public string? UCPathDepartmentDescription { get; set; } = null!;
        public string? UCPathJobStatus { get; set; } = null!;
        public string? UCPathJobStatusDescription { get; set; } = null!;
        public string? UCPathEmployeeStatus { get; set; } = null!;
        public string? UCPathPositionEffective { get; set; } = null!;
        public DateOnly? UCPathEndDate { get; set; } = null!;
        public string? UCPathFTE { get; set; } = null!;
        public string? UCPathUnion { get; set; } = null!;
        public string? UCPathReportsToName { get; set; } = null!;
        public string? UCPathReportsToPosition { get; set; } = null!;
        public List<UCPathResult> UCPathHistory { get; set; } = null!;
        public List<IDCardResult> IDCards { get; set; } = null!;
        public List<KeyResult> Keys { get; set; } = null!;
        public List<LoanResult> Loans { get; set; } = null!;

        public string? Title { get; set; } = null!;
        public string? Department { get; set; } = null!;
        public int Current { get; set; } = -1;
        public int Future { get; set; } = -1;
        public bool? Ross { get; set; } = null!;
        public DateTime? Added { get; set; } = null!;
        public string? Phone { get; set; } = null!;
        public string? Nextel { get; set; } = null!;
        public string? Pager { get; set; } = null!;
        public string? Mobile { get; set; } = null!;
        public string? PostalAddress { get; set; } = null!;
        public string? UCDAffiliation { get; set; } = null!;
        public string? UserName { get; set; } = null!;
        public bool? SVM { get; set; } = null!;
        public string? URI { get; set; } = null!;
        public string? originalObject { get; set; } = null!;


        /*
         * 
    <!--- Roles and permissions ------------------------------------------------------------------------ --->
    <div class="roles">
        <h2><a href="/RAPS/" target="u">System Roles</a></h2>
        <table>
            <thead>
                <tr><th>#perm#</th></tr>
            </thead>
            <tbody>
                <tr>
                    <td>#permFormat(p["displayName"][r])#</td>
                </tr>
            </tbody>
        </table>
    </div>
    <div class="permissions">
        <h2>System Permissions</h2>
        <ul class="tree">
            <li>
                <details>
                    <summary>#p["summary"]# (#p["count"]#)</summary>
                    <ul>
                        <li>#q#</li>
                    </ul>
                </details>
            </li>
        </ul>
    </div>
    <!--- Instinct ----------------------------------------------------------------------------------- --->
    <div class="instinct">
        <h2>Instinct</h2>
        <ul>
            <li><b>Instinct ID:</b> #instinct.id#</li>
            <li><b>Instinct Username:</b> #instinct.username#</li>
            <li><b>Roles:</b> #arrayToList(instinct.roles,", ")#</li>
            <li><b>Status:</b> #instinct.status#</li>
            <li><b>Password expires:</b> #showDate(instinct.passwordExpiresAt)#</li>
            <li><b>Active account?:</b> #showYesNo(instinct.isActive)#</li>
        </ul>
    </div>
    <!--- Active Directory Group Membership -------------------------------------------------------------------------------------- --->
    <div class="adGroup">
        <h2>Active Directory Group Membership</h2>
        <p class="note">Information about ucsvm, ad.vmth, vetmed, and svm is not yet available.</p>
        <ul>
            <!---<li><b>GUID:</b> #ad.objectGUID#</li>--->
            <li><b>Display Name:</b> #ad.displayName#</li>
            <li><b>Mail:</b> #ad.mail#</li>
            <li><b>SAM Account Name:</b> #ad.samAccountName#</li>
            <li><b>User Principal Name:</b> #ad.userprincipalName#</li>
            <li><b>Distinguished Name:</b> #permFormat(ad.distinguishedName)#</li>
        </ul>
        <h3>Member of:</h3>
        <ul>
            <li>
                <details>
                    <summary>#domain#</summary><ul>
                        <li>#m#</li>
                    </ul>
                </details>
            </li>
        </ul>

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
                ClientId = aaudUser.ClientId;
                SpridenId = aaudUser.SpridenId;
                EmployeeId = aaudUser.EmployeeId;
                VmacsId = aaudUser.VmacsId;
                MivId = aaudUser.MivId;
                IamId = aaudUser.IamId;
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
                PPSId = aaudUser.PpsId;
                StudentId = aaudUser.StudentPKey;
                EmployeeId = aaudUser.EmployeePKey;
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
