using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class VwPersonJobPosition
{
    public string Emplid { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? EmailAddr { get; set; }

    public string? CountryCode { get; set; }

    public string? CitizenshipStatusCode { get; set; }

    public string? CitizenshipStatus { get; set; }

    public string? Country { get; set; }

    public DateTime? Birthdate { get; set; }

    public DateTime? OrigHireDt { get; set; }

    public string? PpsId { get; set; }

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

    public DateTime Effdt { get; set; }

    public int? EmplRcd { get; set; }

    public int? Effseq { get; set; }

    public DateTime? ExpectedEndDate { get; set; }

    public string UnionCd { get; set; } = null!;

    public string Deptid { get; set; } = null!;

    public string Jobcode { get; set; } = null!;

    public string EmplStatus { get; set; } = null!;

    public decimal AnnualRt { get; set; }

    public decimal MonthlyRt { get; set; }

    public decimal HourlyRt { get; set; }

    public decimal Fte { get; set; }

    public string? Primaryindex { get; set; }

    public string Action { get; set; } = null!;

    public DateTime? ActionDt { get; set; }

    public string? ManuallySeparated { get; set; }

    public string JobcodeDesc { get; set; } = null!;

    public string JobcodeShortDesc { get; set; } = null!;

    public string Jobgroup { get; set; } = null!;

    public string JobgroupDesc { get; set; } = null!;

    public string FlsaStatus { get; set; } = null!;

    public string JobcodeUnionCode { get; set; } = null!;

    public string DeptCd { get; set; } = null!;

    public string DeptDesc { get; set; } = null!;

    public string DeptShortDesc { get; set; } = null!;

    public string SubDivCd { get; set; } = null!;

    public string SubDivDesc { get; set; } = null!;

    public string JobStatus { get; set; } = null!;

    public string JobStatusDesc { get; set; } = null!;

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

    public int Isprimary { get; set; }

    public int EffDateActive { get; set; }

    public string JobStatus1 { get; set; } = null!;

    public DateTime JobUpdateTimestamp { get; set; }
}
