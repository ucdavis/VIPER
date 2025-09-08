using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class Key
{
    public int KeyId { get; set; }

    public string KeyNumber { get; set; } = null!;

    public string? AccessDescription { get; set; }

    public DateTime CreatedDate { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public string? ManagedBy { get; set; }

    public bool? BuildingMaster { get; set; }

    public bool? Submaster { get; set; }

    public bool? Grandmaster { get; set; }

    public bool? Restricted { get; set; }

    public string? RestrictedContact { get; set; }

    public bool? Deleted { get; set; }

    public string? Notes { get; set; }
}
