using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class UcpathMissingPerson
{
    public string PpsId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Note { get; set; }
}
