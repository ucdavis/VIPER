using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsEarningsBalV
{
    public string? Deptid { get; set; }

    public string Emplid { get; set; } = null!;

    public string Company { get; set; } = null!;

    public string BalanceId { get; set; } = null!;

    public decimal BalanceYear { get; set; }

    public decimal BalanceQtr { get; set; }

    public decimal BalancePeriod { get; set; }

    public decimal EmplRcd { get; set; }

    public string SpclBalance { get; set; } = null!;

    public string Erncd { get; set; } = null!;

    public decimal HrsYtd { get; set; }

    public decimal HrsQtd { get; set; }

    public decimal HrsMtd { get; set; }

    public decimal GrsYtd { get; set; }

    public decimal GrsQtd { get; set; }

    public decimal GrsMtd { get; set; }

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
