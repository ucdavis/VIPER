using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class RelationshipType
{
    public int TypeId { get; set; }

    public string? TypeParentPhrase { get; set; }

    public string? TypeChildPhrase { get; set; }

    public bool? TypeActive { get; set; }

    public string? TypeAbbreviation { get; set; }

    public virtual ICollection<RelationshipsAudit> RelationshipsAudits { get; set; } = new List<RelationshipsAudit>();
}
