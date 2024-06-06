using System;
using System.Collections.Generic;

namespace Viper.Models.CTS;

public partial class Domain
{
    public int DomainId { get; set; }

    public string Name { get; set; } = null!;

    public int Order { get; set; }

    public string? Description { get; set; } = null!;

    public virtual ICollection<Competency> Competencies { get; set; } = new List<Competency>();
}
