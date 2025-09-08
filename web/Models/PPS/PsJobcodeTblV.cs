using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsJobcodeTblV
{
    public string Setid { get; set; } = null!;

    public string Jobcode { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string EffStatus { get; set; } = null!;

    public string Descr { get; set; } = null!;

    public string Descrshort { get; set; } = null!;

    public string JobFunction { get; set; } = null!;

    public string SetidSalary { get; set; } = null!;

    public string SalAdminPlan { get; set; } = null!;

    public string Grade { get; set; } = null!;

    public decimal Step { get; set; }

    public string ManagerLevel { get; set; } = null!;

    public decimal SurveySalary { get; set; }

    public string SurveyJobCode { get; set; } = null!;

    public string UnionCd { get; set; } = null!;

    public decimal RetroRate { get; set; }

    public decimal RetroPercent { get; set; }

    public string CurrencyCd { get; set; } = null!;

    public decimal StdHours { get; set; }

    public string StdHrsFrequency { get; set; } = null!;

    public string CompFrequency { get; set; } = null!;

    public string WorkersCompCd { get; set; } = null!;

    public string JobFamily { get; set; } = null!;

    public string RegTemp { get; set; } = null!;

    public string DirectlyTipped { get; set; } = null!;

    public string MedChkupReq { get; set; } = null!;

    public string FlsaStatus { get; set; } = null!;

    public string Eeo1code { get; set; } = null!;

    public string Eeo4code { get; set; } = null!;

    public string Eeo5code { get; set; } = null!;

    public string Eeo6code { get; set; } = null!;

    public string EeoJobGroup { get; set; } = null!;

    public string UsSocCd { get; set; } = null!;

    public string Ipedsscode { get; set; } = null!;

    public string UsOccCd { get; set; } = null!;

    public string AvailTelework { get; set; } = null!;

    public string FunctionCd { get; set; } = null!;

    public string TrnProgram { get; set; } = null!;

    public string Company { get; set; } = null!;

    public string BargUnit { get; set; } = null!;

    public string EncumberIndc { get; set; } = null!;

    public string PosnMgmtIndc { get; set; } = null!;

    public string EgAcademicRank { get; set; } = null!;

    public string EgGroup { get; set; } = null!;

    public string EncumbSalOptn { get; set; } = null!;

    public decimal EncumbSalAmt { get; set; }

    public DateTime? LastUpdateDate { get; set; }

    public string RegRegion { get; set; } = null!;

    public decimal SalRangeMinRate { get; set; }

    public decimal SalRangeMidRate { get; set; }

    public decimal SalRangeMaxRate { get; set; }

    public string SalRangeCurrency { get; set; } = null!;

    public string SalRangeFreq { get; set; } = null!;

    public string JobSubFunc { get; set; } = null!;

    public string Lastupdoprid { get; set; } = null!;

    public DateTime? Lastupddttm { get; set; }

    public string KeyJobcode { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public string CanNocCd { get; set; } = null!;

    public double? JobcodeSvmSeqNum { get; set; }

    public double? JobcodeSvmSeqMrf { get; set; }

    public string? JobcodeSvmIsMostrecent { get; set; }
}
