using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class Domain
{
    public int DomainId { get; set; }

    public string Name { get; set; } = null!;

    public int Order { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Competency> Competencies { get; set; } = new List<Competency>();
}
