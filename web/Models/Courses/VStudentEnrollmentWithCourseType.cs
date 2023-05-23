using System;
using System.Collections.Generic;

namespace Viper.Models.Courses;

public partial class VStudentEnrollmentWithCourseType
{
    public string? RosterTermCode { get; set; }

    public string BaseinfoSubjCode { get; set; } = null!;

    public string BaseinfoCrseNumb { get; set; } = null!;

    public string BaseinfoSeqNumb { get; set; } = null!;

    public string BaseinfoTitle { get; set; } = null!;

    public string Blocktype { get; set; } = null!;

    public string? RosterPidm { get; set; }

    public string? RosterCrn { get; set; }

    public double? RosterUnit { get; set; }

    public string? RosterGradeMode { get; set; }

    public string RosterPkey { get; set; } = null!;

    public string? UcdipcBeginTerm { get; set; }

    public string? UcdipcEndTerm { get; set; }
}
