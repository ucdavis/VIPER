using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class ReportField
{
    public int ReportFieldId { get; set; }

    public int ReportId { get; set; }

    public int FormFieldId { get; set; }

    public int? Order { get; set; }

    public string? FilterOperation { get; set; }

    public string? FilterValue { get; set; }

    public virtual FormField FormField { get; set; } = null!;

    public virtual Report Report { get; set; } = null!;
}
