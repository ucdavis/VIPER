using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class Competency
{
    public int CompetencyId { get; set; }

    public int DomainId { get; set; }

    public int? ParentId { get; set; }

    public string Number { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool CanLinkToStudent { get; set; }

    public int Order { get; set; }

    public virtual ICollection<BundleCompetency> BundleCompetencies { get; set; } = new List<BundleCompetency>();

    public virtual ICollection<CompetencyMapping> CompetencyMappings { get; set; } = new List<CompetencyMapping>();

    public virtual ICollection<CompetencyOutcome> CompetencyOutcomes { get; set; } = new List<CompetencyOutcome>();

    public virtual ICollection<CourseCompetency> CourseCompetencies { get; set; } = new List<CourseCompetency>();

    public virtual Domain Domain { get; set; } = null!;

    public virtual ICollection<Competency> InverseParent { get; set; } = new List<Competency>();

    public virtual Competency? Parent { get; set; }

    public virtual ICollection<SessionCompetency> SessionCompetencies { get; set; } = new List<SessionCompetency>();

    public virtual ICollection<StudentCompetency> StudentCompetencies { get; set; } = new List<StudentCompetency>();
}
