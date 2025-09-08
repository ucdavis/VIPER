using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class VwUserinfoUser
{
    public int AaudUserId { get; set; }

    public string ClientId { get; set; } = null!;

    public string? IamId { get; set; }

    public string MothraId { get; set; } = null!;

    public string? LoginId { get; set; }

    public string? MailId { get; set; }

    public string? SpridenId { get; set; }

    public string? Pidm { get; set; }

    public string? EmployeeId { get; set; }

    public string? PpsId { get; set; }

    public int? VmacsId { get; set; }

    public string? VmcasId { get; set; }

    public string? UnexId { get; set; }

    public int? MivId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string DisplayLastName { get; set; } = null!;

    public string DisplayFirstName { get; set; } = null!;

    public string? DisplayMiddleName { get; set; }

    public string DisplayFullName { get; set; } = null!;

    public bool CurrentStudent { get; set; }

    public bool FutureStudent { get; set; }

    public bool CurrentEmployee { get; set; }

    public bool FutureEmployee { get; set; }

    public int? StudentTerm { get; set; }

    public int? EmployeeTerm { get; set; }

    public string? StudentPKey { get; set; }

    public string? EmployeePKey { get; set; }

    public int Current { get; set; }

    public int Future { get; set; }

    public bool? Ross { get; set; }

    public DateTime? Added { get; set; }

    public DateTime? Inactivated { get; set; }

    public string? StudentsPKey { get; set; }

    public string? StudentsTermCode { get; set; }

    public string? StudentsClientid { get; set; }

    public string? StudentsMajorCode1 { get; set; }

    public string? StudentsDegreeCode1 { get; set; }

    public string? StudentsCollCode1 { get; set; }

    public string? StudentsLevelCode1 { get; set; }

    public string? StudentsMajorCode2 { get; set; }

    public string? StudentsDegreeCode2 { get; set; }

    public string? StudentsCollCode2 { get; set; }

    public string? StudentsLevelCode2 { get; set; }

    public string? StudentsClassLevel { get; set; }

    public string? EmpPKey { get; set; }

    public string? EmpTermCode { get; set; }

    public string? EmpClientid { get; set; }

    public string? EmpHomeDept { get; set; }

    public string? EmpAltDeptCode { get; set; }

    public string? EmpSchoolDivision { get; set; }

    public string? EmpCbuc { get; set; }

    public string? EmpStatus { get; set; }

    public string? EmpPrimaryTitle { get; set; }

    public string? EmpTeachingTitleCode { get; set; }

    public string? EmpTeachingHomeDept { get; set; }

    public decimal? EmpTeachingPercentFulltime { get; set; }

    public string? EmpEffortTitleCode { get; set; }

    public string? EmpEffortHomeDept { get; set; }

    public string? FlagsPKey { get; set; }

    public string? FlagsTermCode { get; set; }

    public string? FlagsClientid { get; set; }

    public bool? FlagsStudent { get; set; }

    public bool? FlagsAcademic { get; set; }

    public bool? FlagsStaff { get; set; }

    public bool? FlagsTeachingFaculty { get; set; }

    public bool? FlagsWosemp { get; set; }

    public bool? FlagsConfidential { get; set; }

    public bool? FlagsSvmPeople { get; set; }

    public bool? FlagsSvmStudent { get; set; }

    public bool? FlagsRossStudent { get; set; }

    public string? Emplid { get; set; }

    public string? Name { get; set; }

    public string? EmailAddr { get; set; }

    public string? CountryCode { get; set; }

    public string? CitizenshipStatusCode { get; set; }

    public string? CitizenshipStatus { get; set; }

    public string? Country { get; set; }

    public DateTime? Birthdate { get; set; }

    public DateTime? OrigHireDt { get; set; }

    public string? EmpWosempFlg { get; set; }

    public string? EmpAcdmcFlg { get; set; }

    public string? EmpAcdmcSenateFlg { get; set; }

    public string? EmpAcdmcFederationFlg { get; set; }

    public string? EmpTeachingFacultyFlg { get; set; }

    public string? EmpLadderRankFlg { get; set; }

    public string? EmpMspFlg { get; set; }

    public string? EmpMspCareerFlg { get; set; }

    public string? EmpMspCareerPartialyrFlg { get; set; }

    public string? EmpMspCntrctFlg { get; set; }

    public string? EmpMspCasualFlg { get; set; }

    public string? EmpMspSeniorMgmtFlg { get; set; }

    public string? EmpSspFlg { get; set; }

    public string? EmpSspCareerFlg { get; set; }

    public string? EmpSspCareerPartialyrFlg { get; set; }

    public string? EmpSspCntrctFlg { get; set; }

    public string? EmpSspCasualFlg { get; set; }

    public string? EmpSspCasualRestrictedFlg { get; set; }

    public string? EmpSspPerDiemFlg { get; set; }

    public string? EmpFacultyFlg { get; set; }

    public DateTime? Effdt { get; set; }

    public decimal? EmplRcd { get; set; }

    public decimal? Effseq { get; set; }

    public DateTime? ExpectedEndDate { get; set; }

    public string? UnionCd { get; set; }

    public string? Deptid { get; set; }

    public string? Jobcode { get; set; }

    public string? EmplStatus { get; set; }

    public decimal? AnnualRt { get; set; }

    public decimal? MonthlyRt { get; set; }

    public decimal? HourlyRt { get; set; }

    public decimal? Fte { get; set; }

    public string? Primaryindex { get; set; }

    public string? Action { get; set; }

    public DateTime? ActionDt { get; set; }

    public string? ManuallySeparated { get; set; }

    public string? JobcodeDesc { get; set; }

    public string? JobcodeShortDesc { get; set; }

    public string? Jobgroup { get; set; }

    public string? JobgroupDesc { get; set; }

    public string? FlsaStatus { get; set; }

    public string? JobcodeUnionCode { get; set; }

    public string? DeptCd { get; set; }

    public string? DeptDesc { get; set; }

    public string? DeptShortDesc { get; set; }

    public string? SubDivCd { get; set; }

    public string? SubDivDesc { get; set; }

    public string? JobStatus { get; set; }

    public string? JobStatusDesc { get; set; }

    public string? PositionNbr { get; set; }

    public DateTime? PositionEffdt { get; set; }

    public string? PositionStatus { get; set; }

    public string? PositionDesc { get; set; }

    public string? PositionShortDesc { get; set; }

    public string? PositionDeptid { get; set; }

    public string? PositionDeptDesc { get; set; }

    public string? PositionDeptShortDesc { get; set; }

    public string? Grade { get; set; }

    public string? ClassIndc { get; set; }

    public DateTime? PositionActionDt { get; set; }

    public string? PositionAction { get; set; }

    public string? ReportsTo { get; set; }

    public int? Isprimary { get; set; }

    public int? EffDateActive { get; set; }

    public string? JobStatus1 { get; set; }

    public DateTime? JobUpdateTimestamp { get; set; }

    public string? ReportsToName { get; set; }

    public string? ReportsToPosition { get; set; }
}
