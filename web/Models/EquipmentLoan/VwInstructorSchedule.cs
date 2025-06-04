using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class VwInstructorSchedule
{
    public int InstructorScheduleId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string FullName { get; set; } = null!;

    public string MothraId { get; set; } = null!;

    public string? MailId { get; set; }

    public string? Role { get; set; }

    public bool Evaluator { get; set; }

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
