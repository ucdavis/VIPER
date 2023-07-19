using System.Text.RegularExpressions;
using Viper.Areas.RAPS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Services
{
    public class RAPSAuditService
    {
        public enum AuditActionType
        {
            Create,
            Update,
            Delete
        }

        private readonly RAPSContext _context;
        public RAPSAuditService(RAPSContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Add audit entry for create/update/delete role
        /// </summary>
        /// <param name="role">The role. If creating should have ID created already.</param>
        /// <param name="actionType">Create Update or Delete</param>
        public void AuditRoleChange(TblRole role, AuditActionType actionType)
        {
            TblLog tblLog = new() { RoleId = role.RoleId, ModTime = DateTime.Now, ModBy = UserHelper.GetCurrentUser()?.LoginId };
            switch(actionType)
            {
                case AuditActionType.Create:
                    tblLog.Audit = "CreateRole";
                    tblLog.Detail = role.Role;
                    break;
                case AuditActionType.Update:
                    tblLog.Audit = "UpdateRole";
                    tblLog.Detail = "Name: " + role.Role;
                    if(!string.IsNullOrEmpty(role.DisplayName)) 
                    {
                        tblLog.Detail += " Display Name: " + role.DisplayName;
                    }
                    if(!string.IsNullOrEmpty(role.ViewName))
                    {
                        tblLog.Detail += " View: " + role.ViewName;
                    }
                    if(!string.IsNullOrEmpty(role.AccessCode))
                    {
                        tblLog.Detail += " Access Code: " + role.AccessCode;
                    }
                    tblLog.Detail += " Allow all users: " + (role.AllowAllUsers ? "1" : "0");
                    break;
                case AuditActionType.Delete:
                    tblLog.Audit = "DeleteRole";
                    tblLog.Detail = "DeleteRole - " + role.Role;
                    break;
            }
            _context.Add(tblLog);
        }

        /// <summary>
        /// Add audit entry for create/update/delete permission
        /// </summary>
        /// <param name="permission">The permission. If creating should have ID created already.</param>
        /// <param name="actionType">Create Update or Delete</param>
        public void AuditPermissionChange(TblPermission permission, AuditActionType actionType)
        {
            TblLog tblLog = new()
            { 
                PermissionId = permission.PermissionId, 
                Detail = permission.Permission,
                ModTime = DateTime.Now, 
                ModBy = UserHelper.GetCurrentUser()?.LoginId 
            };
            switch (actionType)
            {
                case AuditActionType.Create:
                    tblLog.Audit = "CreatePermission"; break;
                case AuditActionType.Update:
                    tblLog.Audit = "UpdatePermission"; break;
                case AuditActionType.Delete:
                    tblLog.Audit = "DeletePermission"; break;
            }
            _context.Add(tblLog);
        }

        /// <summary>
        /// Add audit entry for role membership changes
        /// </summary>
        /// <param name="roleMember">The rolemember object</param>
        /// <param name="actionType">Create Update or Delete</param>
        public void AuditRoleMemberChange(TblRoleMember roleMember, AuditActionType actionType, string? comment)
        {
            TblLog tblLog = new()
            {
                ModTime = DateTime.Now,
                ModBy = UserHelper.GetCurrentUser()?.LoginId,
                RoleId = roleMember.RoleId,
                MemberId = roleMember.MemberId,
                Comment = comment
            };
            if(actionType == AuditActionType.Create || actionType == AuditActionType.Update)
            {
                string Detail = ""; 
                if (roleMember.StartDate != null)
                {
                    Detail += "\"StartDate\":\"" + roleMember.StartDate.Value.ToString("yyyyMMdd") + "\"";
                }
                if (roleMember.EndDate != null)
                {
                    Detail += (!string.IsNullOrEmpty(Detail) ? "," : "") 
                            + "\"EndDate\":\"" + roleMember.EndDate.Value.ToString("yyyyMMdd") + "\"";
                }
                if(actionType == AuditActionType.Create && !string.IsNullOrEmpty(roleMember.ViewName))
                {
                    Detail += (!string.IsNullOrEmpty(Detail) ? "," : "") 
                            + "\"ViewName\":\"" + roleMember.ViewName + "\"";
                }
                if(!string.IsNullOrEmpty(Detail))
                {
                    Detail = "{" + Detail + "}";
                    tblLog.Detail = Detail;
                }
            }

            switch (actionType)
            {
                case AuditActionType.Create:
                    tblLog.Audit = "AddRoleForMember";
                    break;
                case AuditActionType.Update:
                    tblLog.Audit = "UpdateRoleForMember"; 
                    break;
                case AuditActionType.Delete:
                    tblLog.Audit = "DelRoleForMember"; 
                    break;
            }
            _context.Add(tblLog);
        }

        public void AuditRolePermissionChange(TblRolePermission rolePermission, AuditActionType actionType)
        {
            TblLog tblLog = new()
            {
                ModTime = DateTime.Now,
                ModBy = UserHelper.GetCurrentUser()?.LoginId,
                RoleId = rolePermission.RoleId,
                PermissionId = rolePermission.PermissionId,
                Detail = rolePermission.Access.ToString()
            };

            switch (actionType)
            {
                case AuditActionType.Create:
                    tblLog.Audit = "UpdateRolePermission";
                    break;
                case AuditActionType.Update:
                    tblLog.Audit = "UpdateRolePermission";
                    break;
                case AuditActionType.Delete:
                    tblLog.Audit = "DeletePermissionFromRole";
                    break;
            }
            _context.Add(tblLog);
        }

        public void AuditPermissionMemberChange(TblMemberPermission memberPermission, AuditActionType actionType) 
        {
            TblLog tblLog = new()
            {
                ModTime = DateTime.Now,
                ModBy = UserHelper.GetCurrentUser()?.LoginId,
                PermissionId = memberPermission.PermissionId,
                MemberId = memberPermission.MemberId
            };
            if (actionType == AuditActionType.Create || actionType == AuditActionType.Update)
            {
                string Detail = "Access:" + memberPermission.Access;
                if (memberPermission.StartDate != null)
                {
                    Detail += "\"StartDate\":\"" + memberPermission.StartDate.Value.ToString("yyyyMMdd") + "\"";
                }
                if (memberPermission.EndDate != null)
                {
                    Detail += (!string.IsNullOrEmpty(Detail) ? "," : "")
                            + "\"EndDate\":\"" + memberPermission.EndDate.Value.ToString("yyyyMMdd") + "\"";
                }
                tblLog.Detail = "{" + Detail + "}";
            }

            switch (actionType)
            {
                case AuditActionType.Create:
                    tblLog.Audit = "CreateMemberPermission";
                    break;
                case AuditActionType.Update:
                    tblLog.Audit = "UpdateMemberPermission";
                    break;
                case AuditActionType.Delete:
                    tblLog.Audit = "DelPermissionForMember";
                    break;
            }
            _context.Add(tblLog);
        }

        public void AuditGroupChange(OuGroup group, AuditActionType actionType)
        {
            TblLog tblLog = new()
            {
                OuGroupId = group.OugroupId,
                ModTime = DateTime.Now,
                ModBy = UserHelper.GetCurrentUser()?.LoginId,
                Detail = (actionType == AuditActionType.Create || actionType == AuditActionType.Update) 
                    ? group.Name 
                    : string.Format("Deleting {0}", group.Name)
            };

            switch (actionType)
            {
                case AuditActionType.Create:
                    tblLog.Audit = "CreateOuGroup";
                    break;
                case AuditActionType.Update:
                    tblLog.Audit = "UpdateOuGroup";
                    break;
                case AuditActionType.Delete:
                    tblLog.Audit = "DeleteOuGroup";
                    break;
            }
            _context.Add(tblLog);
        }

        public void AuditOuGroupRoleChange(OuGroupRole groupRole, AuditActionType actionType)
        {
            TblLog tblLog = new()
            {
                OuGroupId = groupRole.OugroupId,
                RoleId = groupRole.RoleId,
                ModTime = DateTime.Now,
                ModBy = UserHelper.GetCurrentUser()?.LoginId
            };

            switch (actionType)
            {
                case AuditActionType.Create:
                    tblLog.Audit = "AddRoleForOuGroup";
                    break;
                case AuditActionType.Delete:
                    tblLog.Audit = "DelRoleForOuGroup";
                    break;
            }
            _context.Add(tblLog);
        }

        public void AuditGroupMemberChange(GroupMember member, int groupId, string groupName, AuditActionType actionType)
        {
            string detail = actionType == AuditActionType.Create 
                ? "Adding " + member.LoginId + " " + member.DisplayName + " to "
                : "Removing " + member.LoginId + " " + member.DisplayName + " from ";
            TblLog tblLog = new()
            {
                OuGroupId = groupId,
                MemberId = member.MemberId,
                Audit = actionType == AuditActionType.Create ? "AddMemberToOuGroup" : "DelMemberFromOuGroup",
                Detail = detail + " group " + groupName + ".",
                ModTime = DateTime.Now,
                ModBy = UserHelper.GetCurrentUser()?.LoginId
            };
            _context.Add(tblLog);
        }
    }
}
