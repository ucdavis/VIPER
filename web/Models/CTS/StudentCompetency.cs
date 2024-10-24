﻿using System;
using System.Collections.Generic;
using Viper.Models.VIPER;

namespace Viper.Models.CTS;

public partial class StudentCompetency
{
    public int StudentCompetencyId { get; set; }

    public int StudentUserId { get; set; }

    public int CompetencyId { get; set; }

    public int LevelId { get; set; }

    public int? EncounterId { get; set; }

    public int? CourseId { get; set; }

    public int? SessionId { get; set; }

    public int? VerifiedBy { get; set; }

    public DateTime? VerifiedTimestamp { get; set; }

    public bool? Deleted { get; set; }

    public int? BundleGroupId { get; set; }

    public int? BundleId { get; set; }

    public DateTime Added { get; set; }

    public DateTime? Updated { get; set; }

    public virtual Bundle? Bundle { get; set; }
    public virtual BundleCompetencyGroup? BundleGroup { get; set; }
    public virtual Person Person { get; set; } = null!;
    public virtual Competency Competency { get; set; } = null!;
    public virtual Level Level { get; set; } = null!;
    public virtual Encounter? Encounter { get; set; }
    public virtual Course? Course { get; set; }
    public virtual Session? Session { get; set; }
    public virtual Person? Verifier { get; set; }
}
