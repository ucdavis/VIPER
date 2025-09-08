using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsxlatitemV
{
    public string Fieldname { get; set; } = null!;

    public string Fieldvalue { get; set; } = null!;

    public DateTime? Effdt { get; set; }

    public string EffStatus { get; set; } = null!;

    public string Xlatlongname { get; set; } = null!;

    public string Xlatshortname { get; set; } = null!;

    public DateTime? Lastupddttm { get; set; }

    public string Lastupdoprid { get; set; } = null!;

    public decimal Syncid { get; set; }

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? PsxSvmSeqNum { get; set; }

    public double? PsxSvmSeqMrf { get; set; }

    public string? PsxSvmIsMostrecent { get; set; }
}
