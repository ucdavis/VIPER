using System;
using System.Collections.Generic;

namespace Viper.Models.Courses;

public partial class CurrentAndFutureTermCodesInCurrentAcademicYear
{
    public string TermCode { get; set; } = null!;

    public string TermAcademicYear { get; set; } = null!;

    public string TermTermType { get; set; } = null!;
}
