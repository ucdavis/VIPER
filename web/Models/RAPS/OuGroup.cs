using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace Viper.Models.RAPS;

public partial class OuGroup
{
    public int OugroupId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<OuGroupRole> OuGroupRoles { get; set; } = new List<OuGroupRole>();
}
