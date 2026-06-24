using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class Edbeffrpt
{
    public string EdbeffEmployeeId { get; set; } = null!;

    public string EdbeffEmpName { get; set; } = null!;

    public int? EdbeffApptNum { get; set; }

    public string? EdbeffTitleCode { get; set; }

    public string? EdbeffDistStep { get; set; }

    public int? EdbeffDistNum { get; set; }

    public short? EdbeffSeqNum { get; set; }

    public string? EdbeffFau { get; set; }

    public DateTime? EdbeffPayBeginDate { get; set; }

    public DateTime? EdbeffPayEndDate { get; set; }

    public string? EdbeffUnitDos { get; set; }

    public decimal? EdbeffUnitPayrate { get; set; }

    public decimal? EdbeffUnitPercent { get; set; }

    public decimal? EdbeffUnitStfAnnual { get; set; }

    public decimal? EdbeffUnitStfFte { get; set; }

    public string? EdbeffDistDos { get; set; }

    public decimal? EdbeffDistPayrate { get; set; }

    public decimal? EdbeffDistPercent { get; set; }

    public decimal? EdbeffDosAnnualRate { get; set; }

    public decimal? EdbeffActual { get; set; }

    public decimal? EdbeffGrossamt { get; set; }
}
