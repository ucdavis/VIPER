using System;
using System.Collections.Generic;

namespace Viper.Models.CTS;

public partial class CompetencyOutcome
{
    public int CompetencyOutcomeId { get; set; }

    public int CompetencyId { get; set; }

    public int OutcomeId { get; set; }
}
