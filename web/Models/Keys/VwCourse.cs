using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class VwCourse
{
    public int CourseId { get; set; }

    public string Status { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string AcademicYear { get; set; } = null!;

    public string? Crn { get; set; }

    public string? CourseNum { get; set; }
}
