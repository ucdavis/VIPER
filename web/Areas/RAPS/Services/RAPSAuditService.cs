using Microsoft.EntityFrameworkCore;
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
        public IUserHelper UserHelper;
        private readonly RAPSSecurityService _securityService;

        public RAPSAuditService(RAPSContext context)
        {
            _context = context;
            UserHelper = new UserHelper();
            _securityService = new(_context);
        }

        /// <summary>
        /// Search the audit table and return AuditLog entries. AuditLog entries are TblLog with additional fields (because TblLog does
        /// not have foreign keys for permission, role, member, group ids
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="auditType"></param>
        /// <param name="modBy"></param>
        /// <param name="modifiedUser"></param>
        /// <param name="roleId"></param>
        /// <param name="permissionId"></param>
        /// <param name="search">Searches modified user names, modifier names, role and permission names, detail, comment and audit action</param>
        /// <returns></returns>
        public async Task<List<AuditLog>> GetAuditEntries(DateOnly? startDate = null, DateOnly? endDate = null, string? auditType = null,
            string? modBy = null, string? modifiedUser = null, int? roleId = null,
            int? permissionId = null, string? search = null, string? instance = null)
        {
            if (search != null)
            {
                search = search.ToLower();
            }
            return await (
                from log in _context.TblLogs
                join modByUser in _context.VwAaudUser
                    on log.ModBy equals modByUser.LoginId
                join tblPermission in _context.TblPermissions
                    on log.PermissionId equals tblPermission.PermissionId
                    into perms
                from permission in perms.DefaultIfEmpty()
                join tblRole in _context.TblRoles
                    on log.RoleId equals tblRole.RoleId
                    into roles
                from role in roles.DefaultIfEmpty()
                join vwAaudUser in _context.VwAaudUser
                    on log.MemberId equals vwAaudUser.MothraId
                    into members
                from member in members.DefaultIfEmpty()
                where (search == null
                    || ((log.Detail ?? "").Contains(search))
                    || (log.Audit.Contains(search))
                    || ((log.Comment ?? "").Contains(search))
                    || (log.ModBy.Contains(search))
                    || ((modByUser.DisplayFullName ?? "").Contains(search))
                    || ((log.MemberId ?? "").Contains(search))
                    || ((member.DisplayFullName ?? "").Contains(search))
                    || ((role.Role ?? "").Contains(search))
                    || ((permission.Permission ?? "").Contains(search))
                    )
                    && (instance == null
                        || (
                            role.Role == null
                            || ((instance.StartsWith("VMACS.") || instance == "VIPERForms") && role.Role.StartsWith(instance))
                            || (
                                !(instance.StartsWith("VMACS.") || instance == "VIPERForms")
                                && !role.Role.StartsWith("VMACS.") 
                                && !role.Role.StartsWith("VIPERForms")
                                )
                        )
                    )
                    && (instance == null
                        || (
                            permission.Permission == null
                            || (instance.StartsWith("VMACS.") && permission.Permission.StartsWith("VMACS"))
                            || (instance == "VIPERForms" && permission.Permission.StartsWith("VIPERForms"))
                            || (
                                !(instance.StartsWith("VMACS.") || instance == "VIPERForms")
                                && !permission.Permission.StartsWith("VMACS") 
                                && !permission.Permission.StartsWith("VIPERForms")
                                )
                        )
                    )
                    && (startDate == null || log.ModTime >= ((DateOnly)startDate).ToDateTime(new TimeOnly(0, 0, 0)))
                    && (endDate == null || log.ModTime <= ((DateOnly)endDate).ToDateTime(new TimeOnly(0, 0, 0)))
                    && (auditType == null || log.Audit == auditType)
                    && (modBy == null || log.ModBy == modBy)
                    && (modifiedUser == null || log.MemberId == modifiedUser)
                    && (roleId == null || log.RoleId == roleId)
                    && (permissionId == null || log.PermissionId == permissionId)
                select new AuditLog
                {
                    AuditRecordId = log.AuditRecordId,
                    MemberId = log.MemberId,
                    RoleId = log.RoleId,
                    PermissionId = log.PermissionId,
                    ModTime = log.ModTime,
                    ModBy = log.ModBy,
                    Audit = log.Audit,
                    Comment = log.Comment,
                    Detail = log.Detail,
                    MemberName = member.DisplayLastName != null ? member.DisplayLastName + ", " + member.DisplayFirstName : null,
                    ModByUserName = modByUser.DisplayLastName + ", " + modByUser.DisplayFirstName,
                    Permission = permission.Permission,
                    Role = (!string.IsNullOrEmpty(role.DisplayName) ? role.DisplayName : role.Role)
                } into record
                orderby record.ModTime descending
                select record)
                .Take(1000)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetMemberRolesAndPermissionHistory(string instance, string memberId, DateOnly startDate)
        {
            List<AuditLog> auditEntries = await GetAuditEntries(instance: instance, modifiedUser: memberId, startDate: startDate);
            bool canModifyPermissions = _securityService.IsAllowedTo("RevertPermissions", instance);
            List<string> auditTypes = new() { "AddRoleForMember", "DelRoleForMember", "UpdateRoleForMember" };
            if(canModifyPermissions)
            {
                auditTypes.AddRange(new List<string>() { "CreateMemberPermission", "UpdateMemberPermission", "DelPermissionForMember" });
            }
            auditEntries = auditEntries.Where(a => auditTypes.Contains(a.Audit)).ToList();

            //keep track of roles and permissions that were created/updated/removed from this user
            Dictionary<string, List<string>> actionsPerformedOnObject = new();
            foreach(AuditLog auditLog in auditEntries)
            {
                if(auditLog?.RoleId != null || auditLog?.PermissionId != null)
                {
                    string key = auditLog?.RoleId != null 
                        ? "role-" + auditLog.RoleId 
                        : "permission-" + auditLog!.PermissionId;
                    if (actionsPerformedOnObject.ContainsKey(key))
                    {
                        List<string> moreRecentActions = actionsPerformedOnObject[key];
                        bool undone = false;
                        switch(auditLog.Audit)
                        {
                            case "DelRoleForMember": 
                                undone = moreRecentActions.Contains("AddRoleForMember") || moreRecentActions.Contains("UpdateRoleForMember"); break;
                            case "AddRoleForMember":
                                undone = moreRecentActions.Contains("DelRoleForMember"); break;
                            case "UpdateRoleForMember":
                                undone = moreRecentActions.Contains("UpdateRoleForMember") || moreRecentActions.Contains("DelRoleForMember"); break;
                            case "DelPermissionForMember":
                                undone = moreRecentActions.Contains("CreateMemberPermission") || moreRecentActions.Contains("UpdateMemberPermission"); break;
                            case "CreateMemberPermission":
                                undone = moreRecentActions.Contains("DelPermissionForMember"); break;
                            case "UpdateMemberPermission":
                                undone = moreRecentActions.Contains("UpdateMemberPermission") || moreRecentActions.Contains("DelPermissionForMember"); break;
                            default: break;
                        }
                        auditLog.Undone = undone;
                    }
                    else
                    {
                        //nothing done on this role or permission before
                        actionsPerformedOnObject.Add(key, new List<string>() { auditLog.Audit });
                    }
                }
            }

            return auditEntries;
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
                ModBy = UserHelper.GetCurrentUser()?.LoginId ?? "__system",
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
                    Detail += (!string.IsNullOrEmpty(Detail) ? "," : "")
                        + "\"StartDate\":\"" + memberPermission.StartDate.Value.ToString("yyyyMMdd") + "\"";
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
                ModBy = UserHelper.GetCurrentUser()?.LoginId ?? "__system"
            };
            _context.Add(tblLog);
        }
    }
}
