using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PrimaryJobsDV
{
    public decimal PrmyJobsDKey { get; set; }

    public string PrmyJobsEmpId { get; set; } = null!;

    public string PrmyJobsPrmyJobAplctnCd { get; set; } = null!;

    public decimal PrmyJobsEmpRecNum { get; set; }

    public DateTime PrmyJobsEffDt { get; set; }

    public string PrmyJobsJobFlg { get; set; } = null!;

    public string PrmyJobsPrmy1Flg { get; set; } = null!;

    public string PrmyJobsPrmy2Flg { get; set; } = null!;

    public string PrmyJobsSrcCd { get; set; } = null!;

    public decimal PrmyJobsJobEffSeqNum { get; set; }

    public decimal PrmyJobsJobEmpRecNum { get; set; }

    public string PrmyJobsPrmyJobAplctnDesc { get; set; } = null!;

    public string DdwMd5Type2 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }

    public DateTime? PrmyJobsExprDt { get; set; }

    public double? PrmyJobsSvmSeqNum { get; set; }

    public double? PrmyJobsSvmSeqMrf { get; set; }

    public string? PrmyJobsSvmIsMostrecent { get; set; }
}
