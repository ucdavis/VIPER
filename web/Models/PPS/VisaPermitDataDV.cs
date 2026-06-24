using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class VisaPermitDataDV
{
    public decimal VisaPrmtDKey { get; set; }

    public decimal EmpDKey { get; set; }

    public string VisaDpndntId { get; set; } = null!;

    public string VisaCntryCd { get; set; } = null!;

    public string VisaPermitTypeCd { get; set; } = null!;

    public DateTime VisaEffDt { get; set; }

    public string VisaWrkPermitNum { get; set; } = null!;

    public string VisaWrkPermitStatCd { get; set; } = null!;

    public DateTime VisaStatDt { get; set; }

    public DateTime VisaIssuedDt { get; set; }

    public string VisaIssuedPlNm { get; set; } = null!;

    public decimal VisaDurtnTimeQty { get; set; }

    public string VisaDurtnTypeCd { get; set; } = null!;

    public DateTime VisaEntryDt { get; set; }

    public DateTime VisaExprDt { get; set; }

    public string VisaIssuingAuthtyNm { get; set; } = null!;

    public string VisaDurtnTypeDesc { get; set; } = null!;

    public string DdwMd5Type2 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }

    public double? VisaSvmSeqNum { get; set; }

    public double? VisaSvmSeqMrf { get; set; }

    public string? VisaSvmIsMostrecent { get; set; }
}
