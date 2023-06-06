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
            TblLog tblLog = new TblLog() { RoleId = role.RoleId, ModTime = DateTime.Now, ModBy = UserHelper.GetCurrentUser()?.LoginId };
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
            TblLog tblLog = new TblLog() 
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
            TblLog tblLog = new TblLog()
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
            TblLog tblLog = new TblLog()
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
    }
}
