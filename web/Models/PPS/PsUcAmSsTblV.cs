using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsUcAmSsTblV
{
    public string? Deptid { get; set; }

    public string Emplid { get; set; } = null!;

    public DateTime? Asofdate { get; set; }

    public decimal PinNum { get; set; }

    public decimal UcPrevBal { get; set; }

    public decimal UcPrdTaken { get; set; }

    public decimal UcPrdAccrual { get; set; }

    public decimal UcPrdAdjusted { get; set; }

    public decimal UcCurrBal { get; set; }

    public decimal UcAccrLimit { get; set; }

    public string UcAprMaxInd { get; set; } = null!;

    public decimal OrderNum { get; set; }

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? UcAmSvmSeqNum { get; set; }

    public string? UcAmSvmPrimary { get; set; }
}
