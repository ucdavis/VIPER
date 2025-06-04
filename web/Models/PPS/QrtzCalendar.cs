using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class QrtzCalendar
{
    public string SchedName { get; set; } = null!;

    public string CalendarName { get; set; } = null!;

    public byte[] Calendar { get; set; } = null!;
}
