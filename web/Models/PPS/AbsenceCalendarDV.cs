using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class AbsenceCalendarDV
{
    public decimal AbsCalDKey { get; set; }

    public string AbsCalPaygrpCd { get; set; } = null!;

    public string AbsCalId { get; set; } = null!;

    public string AbsCalRunType { get; set; } = null!;

    public string AbsCalPerId { get; set; } = null!;

    public DateTime AbsCalPymtDt { get; set; }

    public string AbsCalTrgtCalId { get; set; } = null!;

    public string AbsCalTlPerId { get; set; } = null!;

    public string AbsCalPyeSelctOptnFlg { get; set; } = null!;

    public string AbsCalPyeSelctPlFlg { get; set; } = null!;

    public string AbsCalPyeSelctRtoFlg { get; set; } = null!;

    public string AbsCalSelctCritOptnFlg { get; set; } = null!;

    public DateTime AbsCalCalctnThrghDt { get; set; }

    public string AbsCalPerDesc { get; set; } = null!;

    public string AbsCalPerShortDesc { get; set; } = null!;

    public DateTime AbsCalPerBegDt { get; set; }

    public DateTime AbsCalPerEndDt { get; set; }

    public string AbsCalPerFreqId { get; set; } = null!;

    public string AbsCalPyeSelctOptnDesc { get; set; } = null!;

    public string AbsCalSelctCritOptnDesc { get; set; } = null!;

    public string DdwMd5Type1 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }

    public double? AbsCalSvmSeqNum { get; set; }

    public double? AbsCalSvmSeqMrf { get; set; }

    public string? AbsCalSvmIsMostrecent { get; set; }
}
