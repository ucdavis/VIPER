using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class CourseCompetency
{
    public int CourseCompetencyId { get; set; }

    public int CourseId { get; set; }

    public int CompetencyId { get; set; }

    public int LevelId { get; set; }

    public int Order { get; set; }

    public virtual Competency Competency { get; set; } = null!;

    public virtual Level Level { get; set; } = null!;
}
