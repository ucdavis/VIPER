using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class VwLegacyCompetency
{
    public int DvmCompetencyId { get; set; }

    public string DvmCompetencyName { get; set; } = null!;

    public int? DvmCompetencyParentId { get; set; }

    public bool? DvmCompetencyActive { get; set; }
}
