using System;
using System.Collections.Generic;

namespace Viper.Models.RAPS;

public partial class TblRole
{
    public int RoleId { get; set; }

    public string Role { get; set; } = null!;

    public string? ViewName { get; set; }

    public string? Description { get; set; }

    public byte Application { get; set; }

    /// <summary>
    /// How frequently to update the role membership (0=Never, 1=Daily, 2=Hourly)
    /// </summary>
    public byte UpdateFreq { get; set; }

    public string? AccessCode { get; set; }

    public string? DisplayName { get; set; }

    public bool AllowAllUsers { get; set; }

    public virtual ICollection<OuGroupRole> OuGroupRoles { get; set; } = new List<OuGroupRole>();

    public virtual ICollection<RoleTemplateRole> RoleTemplateRoles { get; set; } = new List<RoleTemplateRole>();

    public virtual ICollection<TblAppRole> TblAppRoles { get; set; } = new List<TblAppRole>();

    public virtual ICollection<TblRoleMember> TblRoleMembers { get; set; } = new List<TblRoleMember>();

    public virtual ICollection<TblRolePermission> TblRolePermissions { get; set; } = new List<TblRolePermission>();
}
