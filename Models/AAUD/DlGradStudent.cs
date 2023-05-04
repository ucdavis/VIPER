using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class DlGradStudent
{
    public string GradStudentsPKey { get; set; } = null!;

    public string GradStudentsTermCode { get; set; } = null!;

    public string GradStudentsClientid { get; set; } = null!;

    public string GradStudentsMajorCode1 { get; set; } = null!;

    public string GradStudentsDegreeCode1 { get; set; } = null!;

    public string GradStudentsCollCode1 { get; set; } = null!;

    public string GradStudentsLevelCode1 { get; set; } = null!;

    public string? GradStudentsMajorCode2 { get; set; }

    public string? GradStudentsDegreeCode2 { get; set; }

    public string? GradStudentsCollCode2 { get; set; }

    public string? GradStudentsLevelCode2 { get; set; }

    public string? GradStudentsClassLevel { get; set; }
}
