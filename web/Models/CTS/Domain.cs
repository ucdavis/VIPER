using System;
using System.Collections.Generic;

namespace Viper.Models.CTS;

public partial class Domain
{
    public int DomainId { get; set; }

    public int Name { get; set; }

    public int Order { get; set; }

    public virtual ICollection<Competency> Competencies { get; set; } = new List<Competency>();
}
