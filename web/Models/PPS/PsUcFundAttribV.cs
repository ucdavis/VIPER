using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsUcFundAttribV
{
    public string Setid { get; set; } = null!;

    public string FundCode { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string UcEverify { get; set; } = null!;

    public string UcFederal { get; set; } = null!;

    public string UcSponsored { get; set; } = null!;

    public string UcStateFund { get; set; } = null!;

    public string UcGeneral { get; set; } = null!;

    public string UcSponsorName { get; set; } = null!;

    public string UcAwardType { get; set; } = null!;

    public string UcFedFlowThru { get; set; } = null!;

    public string UcSponsorTypeOt { get; set; } = null!;

    public string UcAttribute1 { get; set; } = null!;

    public string UcAttribute2 { get; set; } = null!;

    public string UcAttribute3 { get; set; } = null!;

    public string UcCaptype { get; set; } = null!;

    public DateTime? UcFundEndDt { get; set; }

    public string UcFundTypeEncum { get; set; } = null!;

    public DateTime? Lastupddttm { get; set; }

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
