using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class EmployeeHistory
{
    public string EmpId { get; set; } = null!;

    public string EmpPpsUid { get; set; } = null!;

    public string EmpName { get; set; } = null!;

    public DateTime FirstSeen { get; set; }

    public DateTime FirstEffDt { get; set; }

    public DateTime FirstExprDate { get; set; }

    public string FirstRole { get; set; } = null!;

    public DateTime? LastSeen { get; set; }

    public DateTime? LastEffDt { get; set; }

    public DateTime? LastExprDt { get; set; }

    public string? LastRole { get; set; }

    public string? LastStatus { get; set; }
}
