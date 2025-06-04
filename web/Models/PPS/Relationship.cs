using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class Relationship
{
    public int RelId { get; set; }

    public int RelSystem1 { get; set; }

    public int RelSystem2 { get; set; }

    public int RelRelTypeId { get; set; }

    public string? RelComment { get; set; }
}
