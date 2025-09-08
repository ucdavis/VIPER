using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class TimeDailyDV
{
    public decimal DlyTmDimKey { get; set; }

    public DateTime ClndrDt { get; set; }

    public string ClndrDayNm { get; set; } = null!;

    public string ClndrMthNm { get; set; } = null!;

    public decimal ClndrDayOfMthNum { get; set; }

    public decimal ClndrDayOfYrNum { get; set; }

    public decimal ClndrWkOfYrNum { get; set; }

    public decimal ClndrMthOfYrNum { get; set; }

    public string ClndrLastDayMthInd { get; set; } = null!;

    public decimal ClndrMthEndDtKey { get; set; }

    public string ClndrYrMth { get; set; } = null!;

    public string ClndrQtrNm { get; set; } = null!;

    public string ClndrYrQtr { get; set; } = null!;

    public string ClndrHalfNm { get; set; } = null!;

    public string ClndrYrHalf { get; set; } = null!;

    public decimal ClndrYr { get; set; }

    public string WkndInd { get; set; } = null!;

    public decimal WrkdayCnt { get; set; }

    public string HldyNm { get; set; } = null!;

    public string CmpsHldyInd { get; set; } = null!;

    public decimal FsclDayOfPerNum { get; set; }

    public decimal FsclDayOfYrNum { get; set; }

    public decimal FsclWkOfYrNum { get; set; }

    public decimal FsclPerOfYrNum { get; set; }

    public string FsclLastDayPerInd { get; set; } = null!;

    public decimal FsclPerEndDtKey { get; set; }

    public string FsclYrPer { get; set; } = null!;

    public string FsclQtrNm { get; set; } = null!;

    public string FsclYrQtr { get; set; } = null!;

    public string FsclHalfNm { get; set; } = null!;

    public string FsclYrHalf { get; set; } = null!;

    public string FsclYr { get; set; } = null!;

    public string AcadQtrNm { get; set; } = null!;

    public string AcadYrQtr { get; set; } = null!;

    public decimal AcadDayNumQtr { get; set; }

    public decimal AcadWkNumQtr { get; set; }

    public decimal AcadDayNumYr { get; set; }

    public decimal AcadWkNumYr { get; set; }

    public string AcadLastDayQtrInd { get; set; } = null!;

    public decimal AcadQtrEndDtKey { get; set; }

    public decimal AcadYr { get; set; }

    public string LdgrYrMth { get; set; } = null!;

    public decimal LymLinearNum { get; set; }

    public string AcadYrStr { get; set; } = null!;

    public string ClndrYrStr { get; set; } = null!;

    public string FsclYrStr { get; set; } = null!;
}
