using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class VwStipend
{
    public string Emplid { get; set; } = null!;

    public decimal EmplRcd { get; set; }

    public DateTime Effdt { get; set; }

    public DateTime? EarningsEndDt { get; set; }

    public decimal AddlSeq { get; set; }

    public string Erncd { get; set; } = null!;

    public string Deptid { get; set; } = null!;

    public string Jobcode { get; set; } = null!;

    public string PositionNbr { get; set; } = null!;

    public decimal OthPay { get; set; }

    public string AddlPayFrequency { get; set; } = null!;

    public string PayPeriod1 { get; set; } = null!;

    public string PayPeriod2 { get; set; } = null!;

    public string PayPeriod3 { get; set; } = null!;

    public string PayPeriod4 { get; set; } = null!;

    public string PayPeriod5 { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string NameDisplay { get; set; } = null!;

    public string Name { get; set; } = null!;

    public double? PsAddlPaySvmSeqNum { get; set; }

    public double? PsAddlPaySvmSeqMrf { get; set; }
}
