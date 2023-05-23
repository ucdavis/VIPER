using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class RelationshipsAudit
{
    public int AuditId { get; set; }

    public int? AuditRelTypeId { get; set; }

    public string? AuditModBy { get; set; }

    public DateTime? AuditModDate { get; set; }

    public string? AuditChange { get; set; }

    public string? AuditRelParentMothraId { get; set; }

    public string? AuditRelChildMothraId { get; set; }

    public virtual RelationshipType? AuditRelType { get; set; }
}
