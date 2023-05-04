using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class DlStudent
{
    public string StudentsPKey { get; set; } = null!;

    public string StudentsTermCode { get; set; } = null!;

    public string StudentsClientid { get; set; } = null!;

    public string StudentsMajorCode1 { get; set; } = null!;

    public string StudentsDegreeCode1 { get; set; } = null!;

    public string StudentsCollCode1 { get; set; } = null!;

    public string StudentsLevelCode1 { get; set; } = null!;

    public string? StudentsMajorCode2 { get; set; }

    public string? StudentsDegreeCode2 { get; set; }

    public string? StudentsCollCode2 { get; set; }

    public string? StudentsLevelCode2 { get; set; }

    public string? StudentsClassLevel { get; set; }
}
