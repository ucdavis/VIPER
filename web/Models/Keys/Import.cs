using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class Import
{
    public string? KeyNumber { get; set; }

    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? Department { get; set; }

    public string? CutNumber { get; set; }

    public string? Location { get; set; }

    public DateTime? DateAssigned { get; set; }

    public DateTime? DateReturned { get; set; }

    public string? Building { get; set; }

    public string? Room { get; set; }

    public string? Comments { get; set; }
}
