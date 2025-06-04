using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class VwRotation
{
    public int RotId { get; set; }

    public int ServiceId { get; set; }

    public string Name { get; set; } = null!;

    public string Abbreviation { get; set; } = null!;

    public string? SubjectCode { get; set; }

    public string? CourseNumber { get; set; }
}
