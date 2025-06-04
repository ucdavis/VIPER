using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class Epa
{
    public int EpaId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? Order { get; set; }

    public bool Active { get; set; }

    public virtual ICollection<Encounter> Encounters { get; set; } = new List<Encounter>();

    public virtual ICollection<EpaService> EpaServices { get; set; } = new List<EpaService>();

    public virtual ICollection<StudentEpa> StudentEpas { get; set; } = new List<StudentEpa>();
}
