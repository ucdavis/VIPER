using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class VwTerm
{
    public int? TermCode { get; set; }

    public int? AcademicYear { get; set; }

    public string Description { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string TermType { get; set; } = null!;

    public bool CurrentTerm { get; set; }

    public bool? CurrentTermMulti { get; set; }
}
