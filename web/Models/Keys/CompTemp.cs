using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class CompTemp
{
    public int CompetencyId { get; set; }

    public int DomainId { get; set; }

    public int? ParentId { get; set; }

    public string Number { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool CanLinkToStudent { get; set; }
}
