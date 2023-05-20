using System;
using System.Collections.Generic;

namespace Viper.Models.Courses;

public partial class DlSupportPerson
{
    public string SupportCourseId { get; set; } = null!;

    public string SupportPidm { get; set; } = null!;

    public string? SupportClientId { get; set; }

    public string SupportTermCode { get; set; } = null!;

    public string SupportRole { get; set; } = null!;
}
