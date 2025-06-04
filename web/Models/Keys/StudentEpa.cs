using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class StudentEpa
{
    public int StudentEpaId { get; set; }

    public int EpaId { get; set; }

    public int LevelId { get; set; }

    public int EncounterId { get; set; }

    public string? Comment { get; set; }

    public virtual Encounter Encounter { get; set; } = null!;

    public virtual Epa Epa { get; set; } = null!;

    public virtual Level Level { get; set; } = null!;
}
