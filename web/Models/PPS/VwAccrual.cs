using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class VwAccrual
{
    public string Emplid { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal UcCurrBal { get; set; }

    public decimal UcPrevBal { get; set; }

    public decimal UcAccrLimit { get; set; }

    public decimal PinNum { get; set; }

    public string PinNm { get; set; } = null!;

    public string PinType { get; set; } = null!;

    public string Descr { get; set; } = null!;

    public DateTime? Asofdate { get; set; }

    public int? AccrualMonth { get; set; }

    public int? AccrualYear { get; set; }

    public string Type { get; set; } = null!;

    public long? AccrualIdx { get; set; }
}
