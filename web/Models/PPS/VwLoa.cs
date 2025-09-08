using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class VwLoa
{
    public string? Emplid { get; set; }

    public string Name { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public DateTime BgnDt { get; set; }

    public DateTime EndDt { get; set; }

    public DateTime? ReturnDt { get; set; }

    public DateTime LastDateWorked { get; set; }

    public DateTime? PayBeginDt { get; set; }

    public DateTime? PayEndDt { get; set; }

    public DateTime? UcPayReturnDt { get; set; }

    public string Action { get; set; } = null!;

    public string ActionDescription { get; set; } = null!;

    public string ActionReason { get; set; } = null!;

    public string ReasonDescription { get; set; } = null!;

    public string UcAbsReason { get; set; } = null!;

    public string? UcReasonDescription { get; set; }

    public string WfStatus { get; set; } = null!;

    public string StatusDescription { get; set; } = null!;
}
