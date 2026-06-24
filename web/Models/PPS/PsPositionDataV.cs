using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsPositionDataV
{
    public string PositionNbr { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string EffStatus { get; set; } = null!;

    public string Descr { get; set; } = null!;

    public string Descrshort { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string ActionReason { get; set; } = null!;

    public DateTime? ActionDt { get; set; }

    public string BusinessUnit { get; set; } = null!;

    public string Deptid { get; set; } = null!;

    public string Jobcode { get; set; } = null!;

    public string PosnStatus { get; set; } = null!;

    public DateTime? StatusDt { get; set; }

    public string BudgetedPosn { get; set; } = null!;

    public string ConfidentialPosn { get; set; } = null!;

    public string JobShare { get; set; } = null!;

    public string KeyPosition { get; set; } = null!;

    public decimal MaxHeadCount { get; set; }

    public string UpdateIncumbents { get; set; } = null!;

    public string ReportsTo { get; set; } = null!;

    public string ReportDottedLine { get; set; } = null!;

    public string Orgcode { get; set; } = null!;

    public string OrgcodeFlag { get; set; } = null!;

    public string Location { get; set; } = null!;

    public string MailDrop { get; set; } = null!;

    public string CountryCode { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Company { get; set; } = null!;

    public decimal StdHours { get; set; }

    public string StdHrsFrequency { get; set; } = null!;

    public string UnionCd { get; set; } = null!;

    public string Shift { get; set; } = null!;

    public string RegTemp { get; set; } = null!;

    public string FullPartTime { get; set; } = null!;

    public decimal MonHrs { get; set; }

    public decimal TuesHrs { get; set; }

    public decimal WedHrs { get; set; }

    public decimal ThursHrs { get; set; }

    public decimal FriHrs { get; set; }

    public decimal SatHrs { get; set; }

    public decimal SunHrs { get; set; }

    public string BargUnit { get; set; } = null!;

    public string Seasonal { get; set; } = null!;

    public string TrnProgram { get; set; } = null!;

    public string LanguageSkill { get; set; } = null!;

    public string ManagerLevel { get; set; } = null!;

    public string FlsaStatus { get; set; } = null!;

    public string RegRegion { get; set; } = null!;

    public string ClassIndc { get; set; } = null!;

    public string EncumberIndc { get; set; } = null!;

    public decimal Fte { get; set; }

    public string PositionPoolId { get; set; } = null!;

    public string EgAcademicRank { get; set; } = null!;

    public string EgGroup { get; set; } = null!;

    public string EncumbSalOptn { get; set; } = null!;

    public decimal EncumbSalAmt { get; set; }

    public string HealthCertificate { get; set; } = null!;

    public string SignAuthority { get; set; } = null!;

    public string AddsToFteActual { get; set; } = null!;

    public string SalAdminPlan { get; set; } = null!;

    public string Grade { get; set; } = null!;

    public decimal Step { get; set; }

    public string SupvLvlId { get; set; } = null!;

    public string IncludeSalplnFlg { get; set; } = null!;

    public DateTime? Lastupddttm { get; set; }

    public string Lastupdoprid { get; set; } = null!;

    public string SecClearanceType { get; set; } = null!;

    public string AvailTeleworkPos { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public string? GvtCyberSecCd { get; set; }

    public double? PosSvmSeqNum { get; set; }

    public double? PosSvmSeqMrf { get; set; }

    public string? PosSvmIsMostrecent { get; set; }
}
