using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class Level
{
    public int LevelId { get; set; }

    public string LevelName { get; set; } = null!;

    public string? Description { get; set; }

    public bool Active { get; set; }

    public int Order { get; set; }

    public bool Course { get; set; }

    public bool Clinical { get; set; }

    public bool Epa { get; set; }

    public bool Milestone { get; set; }

    public bool Dops { get; set; }

    public virtual ICollection<BundleCompetencyLevel> BundleCompetencyLevels { get; set; } = new List<BundleCompetencyLevel>();

    public virtual ICollection<CourseCompetency> CourseCompetencies { get; set; } = new List<CourseCompetency>();

    public virtual ICollection<Encounter> Encounters { get; set; } = new List<Encounter>();

    public virtual ICollection<MilestoneLevel> MilestoneLevels { get; set; } = new List<MilestoneLevel>();

    public virtual ICollection<SessionCompetency> SessionCompetencies { get; set; } = new List<SessionCompetency>();

    public virtual StudentCompetency? StudentCompetency { get; set; }

    public virtual ICollection<StudentEpa> StudentEpas { get; set; } = new List<StudentEpa>();
}
