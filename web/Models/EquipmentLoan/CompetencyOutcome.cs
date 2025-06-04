using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class CompetencyOutcome
{
    public int CompetencyOutcomeId { get; set; }

    public int CompetencyId { get; set; }

    public int OutcomeId { get; set; }

    public virtual Competency Competency { get; set; } = null!;
}
