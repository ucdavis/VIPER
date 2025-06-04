using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class VwServiceChief
{
    public int ServiceId { get; set; }

    public string? ServiceName { get; set; }

    public string MothraId { get; set; } = null!;

    public int PersonId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;
}
