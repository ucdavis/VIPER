using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsUnionTblV
{
    public string UnionCd { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string EffStatus { get; set; } = null!;

    public string Descr { get; set; } = null!;

    public string Descrshort { get; set; } = null!;

    public string UnionLocalFlg { get; set; } = null!;

    public string BargUnit { get; set; } = null!;

    public string Country { get; set; } = null!;

    public string Address1 { get; set; } = null!;

    public string Address2 { get; set; } = null!;

    public string Address3 { get; set; } = null!;

    public string Address4 { get; set; } = null!;

    public string City { get; set; } = null!;

    public string HouseType { get; set; } = null!;

    public string County { get; set; } = null!;

    public string State { get; set; } = null!;

    public string Postal { get; set; } = null!;

    public string GeoCode { get; set; } = null!;

    public string ContactName { get; set; } = null!;

    public string CountryCode { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string UnionStewardName { get; set; } = null!;

    public DateTime? ContractBeginDt { get; set; }

    public DateTime? ContractEndDt { get; set; }

    public string VacationPlan { get; set; } = null!;

    public string SickPlan { get; set; } = null!;

    public decimal SdiAdminPct { get; set; }

    public string DisabilityIns { get; set; } = null!;

    public string LifeIns { get; set; } = null!;

    public decimal RetmtPickupPct { get; set; }

    public string FicaPickup { get; set; } = null!;

    public decimal CallbackMinHours { get; set; }

    public decimal CallbackRate { get; set; }

    public string Certified { get; set; } = null!;

    public string ClosedShop { get; set; } = null!;

    public decimal StdHours { get; set; }

    public decimal WorkDayHours { get; set; }

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? PsUnionSvmSeqNum { get; set; }

    public double? PsUnionSvmSeqMrf { get; set; }

    public string? PsUnionSvmIsMostrecent { get; set; }
}
