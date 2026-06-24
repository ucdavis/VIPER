using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class Audit
{
    public int AuditId { get; set; }

    public int? IdcardId { get; set; }

    public string? IdcardLoginId { get; set; }

    public int? PqId { get; set; }

    public string? MothraId { get; set; }

    public string Action { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? Timestamp { get; set; }

    public string? IamId { get; set; }

    public bool? Tentative { get; set; }

    public int? IdcardNumber { get; set; }

    public int? AccessLevelId { get; set; }
}
