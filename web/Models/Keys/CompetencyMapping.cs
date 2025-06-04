using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class CompetencyMapping
{
    public int CompetencyMappingId { get; set; }

    public int CompetencyId { get; set; }

    public int DvmCompetencyId { get; set; }

    public virtual Competency Competency { get; set; } = null!;
}
