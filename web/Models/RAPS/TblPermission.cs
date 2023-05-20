using System;
using System.Collections.Generic;

namespace Viper.Models.RAPS;

public partial class TblPermission
{
    public int PermissionId { get; set; }

    public string Permission { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<TblMemberPermission> TblMemberPermissions { get; set; } = new List<TblMemberPermission>();

    public virtual ICollection<TblRolePermission> TblRolePermissions { get; set; } = new List<TblRolePermission>();
}
