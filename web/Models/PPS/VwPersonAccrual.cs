using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class VwPersonAccrual
{
    public string Emplid { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateTime? Asofdate { get; set; }

    public int? AccrualMonth { get; set; }

    public int? AccrualYear { get; set; }

    public decimal UcPrevBal { get; set; }

    public decimal UcPrdTaken { get; set; }

    public decimal UcPrdAccrual { get; set; }

    public decimal UcPrdAdjusted { get; set; }

    public decimal UcCurrBal { get; set; }

    public decimal UcAccrLimit { get; set; }

    public string UcAprMaxInd { get; set; } = null!;

    public decimal OrderNum { get; set; }

    public decimal PinNum { get; set; }

    public string PinCode { get; set; } = null!;

    public string PinNm { get; set; } = null!;

    public string PinType { get; set; } = null!;

    public string AccrualDesc { get; set; } = null!;

    public string Type { get; set; } = null!;

    public long? AccrualIdx { get; set; }
}
