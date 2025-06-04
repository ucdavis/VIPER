using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class RelationType
{
    public int RelTypeId { get; set; }

    public string RelTypeDescription { get; set; } = null!;

    public string RelTypeType { get; set; } = null!;
}
