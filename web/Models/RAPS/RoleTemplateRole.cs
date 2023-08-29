using System;
using System.Collections.Generic;

namespace Viper.Models.RAPS;

public partial class RoleTemplateRole
{
    public int RoleTemplateRoleId { get; set; }

    public int RoleTemplateRoleRoleId { get; set; }

    public int RoleTemplateTemplateId { get; set; }

    public DateTime ModTime { get; set; }

    public string ModBy { get; set; } = null!;

    public virtual TblRole Role { get; set; } = null!;

    public virtual RoleTemplate RoleTemplateTemplate { get; set; } = null!;
}
