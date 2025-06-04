using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class VwWeek
{
    public int WeekId { get; set; }

    public DateTime DateStart { get; set; }

    public DateTime DateEnd { get; set; }

    public bool ExtendedRotation { get; set; }

    public int TermCode { get; set; }

    public bool? StartWeek { get; set; }
}
