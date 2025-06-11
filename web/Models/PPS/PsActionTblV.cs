using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsActionTblV
{
    public string Action { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string EffStatus { get; set; } = null!;

    public string ActionDescr { get; set; } = null!;

    public string ActionDescrshort { get; set; } = null!;

    public string Objectownerid { get; set; } = null!;

    public DateTime? Lastupddttm { get; set; }

    public string Lastupdoprid { get; set; } = null!;

    public string SystemDataFlg { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public string HrActionType { get; set; } = null!;

    public double? PsActionSvmSeqNum { get; set; }

    public double? PsActionSvmSeqMrf { get; set; }

    public string? PsActionSvmIsMostrecent { get; set; }
}
