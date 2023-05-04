using System;
using System.Collections.Generic;

namespace Viper.Models.RAPS;

public partial class RoleTemplate
{
    public int RoleTemplateId { get; set; }

    public string TemplateName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public virtual ICollection<RoleTemplateRole> RoleTemplateRoles { get; set; } = new List<RoleTemplateRole>();
}
