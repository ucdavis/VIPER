using System.ComponentModel.DataAnnotations.Schema;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    public class Group : GroupAddEdit
    {
        public ICollection<GroupRole> GroupRoles { get; set; } = new List<GroupRole>();

        public int? GroupRoleId { get; set; } = 0;
        public int? GroupRoleMemberCount { get; set; } = 0;
        public bool BoxSyncEnabled { get; set; } = false;

        //for AD3 managed groups
        public new string? DisplayName { get; set; } = null!;

        public string FriendlyName
        {
            get
            {
                if (Name.IndexOf(",") > 4)
                {
                    return Name.Substring(3, Name.IndexOf(",") - 3);
                }
                return Name;
            }
        }

        public bool IsOuGroup
        {
            get
            {
                return Name.ToLower().Contains("dc=ou");
            }
        }

        public Group(OuGroup group)
        {
            GroupId = group.OugroupId;
            Name = group.Name;
            Description = group.Description;
            GroupRoles = new List<GroupRole>();
            if(group?.OuGroupRoles != null)
            {
                foreach(OuGroupRole gr in group.OuGroupRoles)
                {
                    GroupRoles.Add(new GroupRole(gr));
                    if(gr.IsGroupRole)
                    {
                        GroupRoleId = gr.RoleId;
                        GroupRoleMemberCount = gr?.Role?.TblRoleMembers?.Count;
                    }
                }
            }
        }
    }
}
