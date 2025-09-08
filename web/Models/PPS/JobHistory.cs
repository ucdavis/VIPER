using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class JobHistory
{
    public int Recordid { get; set; }

    public string EmpId { get; set; } = null!;

    public string? EmpPpsUid { get; set; }

    public int RecNum { get; set; }

    public string JobCd { get; set; } = null!;

    public string JobGrp { get; set; } = null!;

    public DateTime FirstSeen { get; set; }

    public DateTime FirstEffDt { get; set; }

    public DateTime FirstEndDate { get; set; }

    public string FirstDept { get; set; } = null!;

    public string FirstTitle { get; set; } = null!;

    public DateTime? LastSeen { get; set; }

    public DateTime? LastEffDt { get; set; }

    public DateTime? LastEndDate { get; set; }

    public string? LastDept { get; set; }

    public string? LastTitle { get; set; }

    public string? LastStatus { get; set; }
}
