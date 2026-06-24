using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class JobHistoryDetail
{
    public int Recordid { get; set; }

    public string EmpId { get; set; } = null!;

    public string? EmpPpsUid { get; set; }

    public int RecNum { get; set; }

    public int SeqNum { get; set; }

    public DateTime EffDt { get; set; }

    public DateTime? EndDt { get; set; }

    public int SvmSeqNum { get; set; }

    public bool SvmCurFlag { get; set; }

    public string CurFlag { get; set; } = null!;

    public string JobCd { get; set; } = null!;

    public string StatCd { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string DeptCd { get; set; } = null!;

    public decimal AnnlRt { get; set; }

    public decimal FtePct { get; set; }

    public string? ActionCd { get; set; }

    public string? ActionRsnCd { get; set; }

    public DateTime? ActionEffDt { get; set; }

    public DateTime InsertDttm { get; set; }

    public DateTime UpdateDttm { get; set; }

    public DateTime SrcDttm { get; set; }

    public DateTime FirstSeen { get; set; }

    public DateTime LastSeen { get; set; }
}
