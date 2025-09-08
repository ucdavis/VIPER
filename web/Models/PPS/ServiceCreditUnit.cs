using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class ServiceCreditUnit
{
    public int ServiceCreditUnitId { get; set; }

    public string ServiceCreditDeptCode { get; set; } = null!;

    public string ServiceCreditDeptName { get; set; } = null!;

    public string ServiceCreditCaoMailid { get; set; } = null!;

    public string ServiceCreditCaoMothraid { get; set; } = null!;
}
