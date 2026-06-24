using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class AccessExpiration
{
    public int ExpiringId { get; set; }

    public string IamId { get; set; } = null!;

    public int AccessLevelId { get; set; }

    public DateTime ExpirationDate { get; set; }

    public DateTime? ExtendedTo { get; set; }

    public bool? ConfirmedRemoval { get; set; }

    public string? ModBy { get; set; }

    public DateTime ModTime { get; set; }

    public bool? Processed { get; set; }
}
