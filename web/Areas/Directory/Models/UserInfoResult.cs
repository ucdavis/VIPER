using Viper.Models.IAM;

namespace Viper.Areas.Directory.Models
{
    public class UserInfoResult
    {
        // Basic user information
        public string? IamId { get; set; }
        public string? MothraId { get; set; }
        public string? DisplayFullName { get; set; }
        public string? MailId { get; set; }
        public bool IsValid { get; set; }
        public bool IsEmployee { get; set; }
        public bool IsStudent { get; set; }

        // Directory Information
        public string? Title { get; set; }
        public string? Department { get; set; }
        public string? Email { get; set; }
        public string? EmailHost { get; set; }
        public string? LoginId { get; set; }
        public string? LabeledUri { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Pager { get; set; }
        public string? PostalAddress { get; set; }
        public string? EmployeeId { get; set; }
        public string? StudentId { get; set; }
        public string? Pidm { get; set; }
        public string? MivId { get; set; }
        public bool CurrentAffiliate { get; set; } = true;

        // Employee Information
        public string? EmployeePrimaryTitle { get; set; }
        public string? EmployeeSchoolDivision { get; set; }
        public string? EmployeeStatus { get; set; }
        public string? EmployeeTerm { get; set; }
        public string? EmployeeHomeDepartment { get; set; }
        public string? EmployeeHomeDepartmentName { get; set; }
        public string? EmployeeEffortHomeDepartment { get; set; }
        public string? EmployeeEffortHomeDepartmentName { get; set; }
        public string? EmployeeTeachingHomeDepartment { get; set; }
        public string? EmployeeTeachingPercentFulltime { get; set; }

        // Student Information
        public string? StudentPriorName { get; set; }
        public string? StudentBannerId { get; set; }
        public string? StudentStatus { get; set; }
        public string? StudentPrimaryMajor { get; set; }
        public string? StudentAllMajors { get; set; }
        public string? StudentRegistrationStatus { get; set; }
        public string? StudentClassLevel { get; set; }
        public string? StudentClassOf { get; set; }

        // IAM Information
        public string? PPSId { get; set; }
        public string? OFullName { get; set; }
        public bool IsHSEmployee { get; set; }
        public bool IsFaculty { get; set; }
        public bool IsStaff { get; set; }
        public bool IsExternal { get; set; }

        // IAM Associations
        public string? AssociationsTitle { get; set; }
        public string? AssociationsTitleCode { get; set; }
        public string? AssociationsDepartment { get; set; }
        public string? AssociationsDepartmentCode { get; set; }
        public string? AssociationsAdminDepartment { get; set; }
        public string? AssociationsAdminDepartmentAbbrev { get; set; }
        public string? AssociationsAdminDepartmentCode { get; set; }
        public string? AssociationsAppointmentDepartment { get; set; }
        public string? AssociationsAppointmentDepartmentAbbrev { get; set; }
        public string? AssociationsAppointmentDepartmentCode { get; set; }
        public string? AssociationsPositionType { get; set; }
        public string? AssociationsEmployeeClass { get; set; }
        public string? AssociationsPercentFulltime { get; set; }
        public DateTime? AssociationsStartDate { get; set; }
        public DateTime? AssociationsEndDate { get; set; }
        public List<CorePerson> IamPeople { get; set; } = new List<CorePerson>();
        public List<EmployeeAssociation> IamAssociations { get; set; } = new List<EmployeeAssociation>();

        // System Roles and Permissions
        public List<SystemRole> SystemRoles { get; set; } = new List<SystemRole>();
        public List<SystemPermission> SystemPermissions { get; set; } = new List<SystemPermission>();

        // UC Path Information
        public List<string> UCPathFlags { get; set; } = new List<string>();
        public string? UCPathJobCode { get; set; }
        public string? UCPathJobDescription { get; set; }
        public string? UCPathDepartmentId { get; set; }
        public string? UCPathDepartmentDescription { get; set; }
        public string? UCPathJobStatus { get; set; }
        public string? UCPathEmployeeStatus { get; set; }
        public string? UCPathJobStatusDescription { get; set; }
        public DateTime? UCPathPositionEffectiveDate { get; set; }
        public DateTime? UCPathExpectedEndDate { get; set; }
        public decimal? UCPathFTE { get; set; }
        public string? UCPathUnion { get; set; }
        public string? UCPathReportsToName { get; set; }
        public string? UCPathReportsToPosition { get; set; }
        public List<UCPathResult> UCPathHistory { get; set; } = new List<UCPathResult>();

        // ID Cards, Keys, Loans
        public List<IDCardResult> IDCards { get; set; } = new List<IDCardResult>();
        public List<KeyResult> Keys { get; set; } = new List<KeyResult>();
        public List<LoanResult> Loans { get; set; } = new List<LoanResult>();

        // Instinct Information
        public string? InstinctId { get; set; }
        public string? InstinctUsername { get; set; }
        public List<string> InstinctRoles { get; set; } = new List<string>();
        public string? InstinctStatus { get; set; }
        public DateTime? InstinctPasswordExpiresAt { get; set; }
        public bool InstinctIsActive { get; set; }
        public InstinctResult? InstinctInfo { get; set; }

        // Active Directory Information
        public string? ADDisplayName { get; set; }
        public string? ADMail { get; set; }
        public string? ADSamAccountName { get; set; }
        public string? ADUserPrincipalName { get; set; }
        public string? ADDistinguishedName { get; set; }
        public List<string> ADMemberOf { get; set; } = new List<string>();

        // Permission flags for view logic
        public bool CanViewDirectoryDetail { get; set; }
        public bool CanViewStudentID { get; set; }
        public bool CanViewIAM { get; set; }
        public bool CanViewRoles { get; set; }
        public bool CanViewUCPath { get; set; }
        public bool CanViewUCPathDetail { get; set; }
        public bool CanViewIDCards { get; set; }
        public bool CanViewKeys { get; set; }
        public bool CanViewLoans { get; set; }
        public bool CanViewInstinct { get; set; }
        public bool CanViewADGroups { get; set; }
        public bool IsOwnPage { get; set; }
        public bool ShowPhoneLinks { get; set; }
        public bool HasAltPhoto { get; set; }
    }

    public class SystemRole
    {
        public string? System { get; set; }
        public string? DisplayName { get; set; }
    }

    public class SystemPermission
    {
        public string? Category { get; set; }
        public string? Permission { get; set; }
        public int Count { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
