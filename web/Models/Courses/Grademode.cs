using System;
using System.Collections.Generic;

namespace Viper.Models.Courses;

public partial class Grademode
{
    public string GmodePkey { get; set; } = null!;

    public string GmodeTermCode { get; set; } = null!;

    public string GmodeCrn { get; set; } = null!;

    public string? GmodeCode { get; set; }
}
