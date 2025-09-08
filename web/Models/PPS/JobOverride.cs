using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class JobOverride
{
    public int RecordId { get; set; }

    public string AddedBy { get; set; } = null!;

    public DateTime AddedOn { get; set; }

    public string LastmodifiedBy { get; set; } = null!;

    public DateTime LastmodifiedOn { get; set; }

    public string Emplid { get; set; } = null!;

    public int EmplRcd { get; set; }

    public string Deptid { get; set; } = null!;

    public string Jobcode { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public int? Effseq { get; set; }

    public string Jobstatus { get; set; } = null!;

    public string? Comment { get; set; }
}
