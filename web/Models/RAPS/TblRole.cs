using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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

    [NotMapped]
    public string FriendlyName { get
        {
            if(DisplayName != null) return DisplayName;
            if (Role.Length > 15 && Role.Substring(0, 15) == "RAPS.Groups.CN=")
            {
                string groupName = Role.Substring(15, Role.Length - 15);
                if (groupName.IndexOf(",") > 0)
                {
                    groupName = groupName.Substring(0, groupName.IndexOf(","));
                }
                return "RAPS Role for Group " + groupName;
            }
            return Role;
        } 
    }

    [NotMapped]
    public string Instance { get
        {
            if(Role.ToLower().StartsWith("vmacs."))
            {
                return Role.Split(".")[0] + "." + Role.Split(".")[1];
            }
            return Role.ToLower().StartsWith("viperforms") ? "VIPERForms" : "VIPER";
        }       
    }

    public virtual ICollection<OuGroupRole> OuGroupRoles { get; set; } = new List<OuGroupRole>();

    public virtual ICollection<RoleTemplateRole> RoleTemplateRoles { get; set; } = new List<RoleTemplateRole>();

    public virtual ICollection<TblAppRole> AppRoles { get; set; } = new List<TblAppRole>();

    public virtual ICollection<TblAppRole> ChildRoles { get; set; } = new List<TblAppRole>();

    public virtual ICollection<TblRoleMember> TblRoleMembers { get; set; } = new List<TblRoleMember>();

    public virtual ICollection<TblRolePermission> TblRolePermissions { get; set; } = new List<TblRolePermission>();
}
