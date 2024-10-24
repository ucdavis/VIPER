﻿using System;
using System.Collections.Generic;

namespace Viper.Models.CTS;

public partial class SessionCompetency
{
    public int SessionCompetencyId { get; set; }

    public int SessionId { get; set; }

    public int CompetencyId { get; set; }

    public int LevelId { get; set; }

    public int? RoleId { get; set; }

    public int Order { get; set; }

    public virtual Session Session { get; set; } = null!;
    public virtual Competency Competency { get; set; } = null!;
    public virtual Level Level { get; set; } = null!;
    public virtual Role? Role { get; set; }
}
