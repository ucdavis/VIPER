using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class VwStudentSchedule
{
    public int StudentScheduleId { get; set; }

    public int PersonId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string FullName { get; set; } = null!;

    public string MothraId { get; set; } = null!;

    public string? MailId { get; set; }

    public int? Pidm { get; set; }

    public bool? NotGraded { get; set; }

    public bool? Incomplete { get; set; }

    public bool? MakeUp { get; set; }

    public bool? NotEnrolled { get; set; }

    public DateTime DateStart { get; set; }

    public DateTime DateEnd { get; set; }

    public int WeekId { get; set; }

    public int RotationId { get; set; }

    public string RotationName { get; set; } = null!;

    public string Abbreviation { get; set; } = null!;

    public string? SubjCode { get; set; }

    public string? CrseNumb { get; set; }

    public int ServiceId { get; set; }

    public string? ServiceName { get; set; }
}
