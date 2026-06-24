using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class VwAccrualsCdm
{
    public decimal? EmpDKey { get; set; }

    public string? EmpId { get; set; }

    public string? EmpPrmyFullNm { get; set; }

    public decimal? AbsCalDKey { get; set; }

    public decimal? AbsRsltFEmpRecNum { get; set; }

    public decimal? AbsRsltFSeq8Num { get; set; }

    public decimal? AbsRsltFCalcRsltValRt { get; set; }

    public decimal? AbsRsltFCalcValRt { get; set; }

    public decimal? AbsRsltFUserAdjstValRt { get; set; }

    public decimal PinNum { get; set; }

    public string PinNm { get; set; } = null!;

    public string PinTypeCd { get; set; } = null!;

    public string PinDesc { get; set; } = null!;

    public DateTime AbsCalPymtDt { get; set; }

    public decimal PinDKey { get; set; }

    public decimal AccrualMonth { get; set; }

    public decimal AccrualYear { get; set; }

    public string Type { get; set; } = null!;

    public long? AccrualIdx { get; set; }
}
