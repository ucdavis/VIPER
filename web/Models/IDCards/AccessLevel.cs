using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class AccessLevel
{
    public int AccessLevelId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int LenelId { get; set; }

    public string LenelName { get; set; } = null!;

    public string? Criteria { get; set; }

    public bool Assignable { get; set; }

    public bool Autoassignable { get; set; }
}
