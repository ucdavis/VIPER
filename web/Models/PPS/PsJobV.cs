using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsJobV
{
    public string Emplid { get; set; } = null!;

    public decimal EmplRcd { get; set; }

    public DateTime Effdt { get; set; }

    public decimal Effseq { get; set; }

    public string PerOrg { get; set; } = null!;

    public string Deptid { get; set; } = null!;

    public string Jobcode { get; set; } = null!;

    public string PositionNbr { get; set; } = null!;

    public string SupervisorId { get; set; } = null!;

    public string HrStatus { get; set; } = null!;

    public string PositionOverride { get; set; } = null!;

    public string PosnChangeRecord { get; set; } = null!;

    public string EmplStatus { get; set; } = null!;

    public string Action { get; set; } = null!;

    public DateTime? ActionDt { get; set; }

    public string ActionReason { get; set; } = null!;

    public string Location { get; set; } = null!;

    public string TaxLocationCd { get; set; } = null!;

    public DateTime? JobEntryDt { get; set; }

    public DateTime? DeptEntryDt { get; set; }

    public DateTime? PositionEntryDt { get; set; }

    public string Shift { get; set; } = null!;

    public string RegTemp { get; set; } = null!;

    public string FullPartTime { get; set; } = null!;

    public string Company { get; set; } = null!;

    public string Paygroup { get; set; } = null!;

    public string BasGroupId { get; set; } = null!;

    public string EligConfig1 { get; set; } = null!;

    public string EligConfig2 { get; set; } = null!;

    public string EligConfig3 { get; set; } = null!;

    public string EligConfig4 { get; set; } = null!;

    public string EligConfig5 { get; set; } = null!;

    public string EligConfig6 { get; set; } = null!;

    public string EligConfig7 { get; set; } = null!;

    public string EligConfig8 { get; set; } = null!;

    public string EligConfig9 { get; set; } = null!;

    public string BenStatus { get; set; } = null!;

    public string BasAction { get; set; } = null!;

    public string CobraAction { get; set; } = null!;

    public string EmplType { get; set; } = null!;

    public string HolidaySchedule { get; set; } = null!;

    public decimal StdHours { get; set; }

    public string StdHrsFrequency { get; set; } = null!;

    public string OfficerCd { get; set; } = null!;

    public string EmplClass { get; set; } = null!;

    public string SalAdminPlan { get; set; } = null!;

    public string Grade { get; set; } = null!;

    public DateTime? GradeEntryDt { get; set; }

    public decimal Step { get; set; }

    public DateTime? StepEntryDt { get; set; }

    public string EarnsDistType { get; set; } = null!;

    public string CompFrequency { get; set; } = null!;

    public decimal Comprate { get; set; }

    public decimal ChangeAmt { get; set; }

    public decimal ChangePct { get; set; }

    public decimal AnnualRt { get; set; }

    public decimal MonthlyRt { get; set; }

    public decimal DailyRt { get; set; }

    public decimal HourlyRt { get; set; }

    public decimal AnnlBenefBaseRt { get; set; }

    public decimal ShiftRt { get; set; }

    public decimal ShiftFactor { get; set; }

    public string CurrencyCd { get; set; } = null!;

    public string BusinessUnit { get; set; } = null!;

    public string SetidDept { get; set; } = null!;

    public string SetidJobcode { get; set; } = null!;

    public string SetidLocation { get; set; } = null!;

    public string SetidSalary { get; set; } = null!;

    public string SetidEmplClass { get; set; } = null!;

    public string RegRegion { get; set; } = null!;

    public string DirectlyTipped { get; set; } = null!;

    public string FlsaStatus { get; set; } = null!;

    public string EeoClass { get; set; } = null!;

    public string UnionCd { get; set; } = null!;

    public string BargUnit { get; set; } = null!;

    public string GpPaygroup { get; set; } = null!;

    public string GpDfltEligGrp { get; set; } = null!;

    public string GpEligGrp { get; set; } = null!;

    public string GpDfltCurrttyp { get; set; } = null!;

    public string CurRtType { get; set; } = null!;

    public string GpDfltExrtdt { get; set; } = null!;

    public string GpAsofDtExgRt { get; set; } = null!;

    public string ClassIndc { get; set; } = null!;

    public string EncumbOverride { get; set; } = null!;

    public string FicaStatusEe { get; set; } = null!;

    public decimal Fte { get; set; }

    public string ProrateCntAmt { get; set; } = null!;

    public string PaySystemFlg { get; set; } = null!;

    public string LumpSumPay { get; set; } = null!;

    public string ContractNum { get; set; } = null!;

    public string JobIndicator { get; set; } = null!;

    public string BenefitSystem { get; set; } = null!;

    public decimal WorkDayHours { get; set; }

    public string ReportsTo { get; set; } = null!;

    public string JobDataSrcCd { get; set; } = null!;

    public string Estabid { get; set; } = null!;

    public string SupvLvlId { get; set; } = null!;

    public string SetidSupvLvl { get; set; } = null!;

    public string AbsenceSystemCd { get; set; } = null!;

    public string PoiType { get; set; } = null!;

    public DateTime? HireDt { get; set; }

    public DateTime? LastHireDt { get; set; }

    public DateTime? TerminationDt { get; set; }

    public DateTime? AsgnStartDt { get; set; }

    public DateTime? LstAsgnStartDt { get; set; }

    public DateTime? AsgnEndDt { get; set; }

    public string LdwOvr { get; set; } = null!;

    public DateTime? LastDateWorked { get; set; }

    public DateTime? ExpectedReturnDt { get; set; }

    public DateTime? ExpectedEndDate { get; set; }

    public string AutoEndFlg { get; set; } = null!;

    public DateTime? Lastupddttm { get; set; }

    public string Lastupdoprid { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public DateTime? UnionSeniorityDt { get; set; }

    public double? JobSvmSeqNum { get; set; }

    public double? JobSvmSeqMrf { get; set; }

    public string? JobSvmIsMostrecent { get; set; }

    public string? JobSvmPrimaryIdx { get; set; }

    public string? JobSvmManualSep { get; set; }
}
