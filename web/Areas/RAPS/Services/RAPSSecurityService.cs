using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Polly;
using System.Linq.Expressions;
using System.Security.Cryptography;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Services
{
    public class RAPSSecurityService
    {
		private readonly UserWrapper _userWrapper = new UserWrapper();
		private readonly RAPSContext _context;

        public RAPSSecurityService(RAPSContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Check that the instance is valid
        /// </summary>
        /// <param name="Instance">The instance</param>
        /// <returns>true if the instance is valid, false otherwise</returns>
        static public bool IsValidInstance(string Instance)
        {
            switch(Instance)
            {
                // VIPER,VIPERForms,VMACS.VMTH,VMACS.VMLF,VMACS.UCVMCSD
                case "VIPER": return true;
                case "VIPERForms": return true;
                case "VMACS.VMTH": return true;
                case "VMACS.VMLF": return true;
                case "VMACS.UCVMCSD": return true;
                default: break;
            }
            return false;
        }

        /// <summary>
        /// Check to see if the instance is a VMACS instance
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <returns>True if the instance is a VMACS instance</returns>
        static public bool IsVMACSInstance(string instance)
        {
            return instance.StartsWith("VMACS.");
        }

        static public Expression<Func<TblRole, bool>> FilterRolesToInstance(string instance)
        {
            return r =>
                instance.ToUpper() == "VIPER"
                ? !r.Role.ToUpper().StartsWith("VMACS.") && !r.Role.ToUpper().StartsWith("VIPERFORMS")
                : r.Role.StartsWith(instance);
        }

        /// <summary>
        /// Check that the role belongs to the given instance by checking the role name
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="role">The role</param>
        /// <returns>true if the role belongs to the instance, false otherwise</returns>
        public bool RoleBelongsToInstance(string instance, TblRole role)
        {
            string roleName = role.Role.ToUpper();
            if(instance.ToUpper() == "VIPER")
            {
                return !roleName.StartsWith("VMACS.") && !roleName.StartsWith("VIPERFORMS");
            }
            return roleName.StartsWith(instance.ToUpper());
        }

        /// <summary>
        /// Check that the permission belongs to the given instance by checking the permission name
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="permission">The permission</param>
        /// <returns>true if the permission belongs to the instance, false otherwise</returns>
        public bool PermissionBelongsToInstance(string instance, TblPermission permission)
        {
            string permissionName = permission.Permission.ToUpper();
            if(instance.ToUpper() == "VIPER")
            {
                return !permissionName.StartsWith("VMACS") && !permissionName.StartsWith("VIPERFORMS");
            }
            if(instance.ToUpper().StartsWith("VMACS"))
            {
                return permissionName.StartsWith("VMACS");
            }
            return permissionName.StartsWith(instance.ToUpper());
        }

        public bool PermissionBelongsToInstance(string instance, string permission)
        {
            string permissionName = permission.ToUpper();
            if (instance.ToUpper() == "VIPER")
            {
                return !permissionName.StartsWith("VMACS") && !permissionName.StartsWith("VIPERFORMS");
            }
            if (instance.ToUpper().StartsWith("VMACS"))
            {
                return permissionName.StartsWith("VMACS");
            }
            return permissionName.StartsWith(instance.ToUpper());
        }

        /// <summary>
        /// Check that a user is allowed to perform an action, optionally checking the instance they are in
        /// Having certain permissions allows the user to do things in the VMACS instances, but not Viper, e.g. the helpdesk 
        /// cannot assign roles in the VIPER instance
        /// </summary>
        /// <param name="action">The action to take</param>
        /// <param name="instance">The instance the user is in</param>
        /// <returns>true if they have access, false otherwise</returns>
        public bool IsAllowedTo(string action, string? instance = null)
        {
            if(_userWrapper.HasPermission(_context, _userWrapper.GetCurrentUser(), "RAPS.Admin"))
            {
                return true;
            }
            switch(action)
            {
                //special cases where having a permission grants access to certain instances
                case "ViewAllRoles":
                    return instance  != null && IsVMACSInstance(instance) && _userWrapper.HasPermission(_context, _userWrapper.GetCurrentUser(), "RAPS.ViewRoles");
                case "AccessInstance":
                    return instance != null 
                        && (
                            IsVMACSInstance(instance) && _userWrapper.HasPermission(_context, _userWrapper.GetCurrentUser(), "RAPS.ViewRoles")
                            || GetControlledRoleIds(_userWrapper.GetCurrentUser()?.MothraId).Count() > 0
                            );
                case "RSOP":
                    return instance != null && IsVMACSInstance(instance) && _userWrapper.HasPermission(_context, _userWrapper.GetCurrentUser(), "RAPS.RSOP");
                case "ViewHistory":
                    return instance != null && IsVMACSInstance(instance) && _userWrapper.HasPermission(_context, _userWrapper.GetCurrentUser(), "RAPS.EditRoleMembership");
                case "EditRoleMembership":
                    return instance != null && IsVMACSInstance(instance) && _userWrapper.HasPermission(_context, _userWrapper.GetCurrentUser(), "RAPS.EditRoleMembership");
                default:
                    //by default, check the action against the user having the permission RAPS.<action> 
                    return _userWrapper.HasPermission(_context, _userWrapper.GetCurrentUser(), "RAPS." + action);
            }
        }

        /// <summary>
        /// Check to see if the user can perform the action on a particular role.
        /// </summary>
        /// <param name="action">Action to perform</param>
        /// <param name="Instance">Instance the user is working with</param>
        /// <param name="Role">The specific role, used to check if the user has access to this particular role</param>
        /// <returns>true/false</returns>
        public bool IsAllowedTo(string action, string Instance, TblRole Role)
        {
            AaudUser? User = _userWrapper.GetCurrentUser();
            if (_userWrapper.HasPermission(_context, User, "RAPS.Admin"))
            {
                return true;
            }

            switch (action)
            {
                //TODO: Update for VMACS instances + EditRoleMembership
                case "EditRoleMembership":
                    List<int> controlledRoleIds = GetControlledRoleIds(User?.MothraId);
                    return controlledRoleIds.Contains(Role.RoleId)
                            || _userWrapper.HasPermission(_context, User, "RAPS.EditRoleMembership");
                default:
                    return false;
            }
        }

        /// <summary>
        /// Get the list of App Roles (Delegate Roles) assigned to the user
        /// </summary>
        /// <param name="userId">User mothra id</param>
        /// <returns>A list of delegate roles the user is assigned to</returns>
        public List<TblRole> GetAppRolesForUser(string? userId)
        {
            List<TblRole> roles = _context.TblRoles
                    .Include(r => r.TblRoleMembers)
                    .Include(r => r.ChildRoles)
                        .ThenInclude(cr => cr.Role)
                    .Where(r => r.Application == 1)
                    .Where(r => r.TblRoleMembers.Any(rm => rm.MemberId == userId))
                    .ToList();
            return roles;
        }

        /// <summary>
        /// Get the role ids the user controls (can assign other users to) through delegate roles
        /// </summary>
        /// <param name="userId">User mothra id</param>
        /// <returns>List of roleIds the user controls</returns>
        public List<int> GetControlledRoleIds(string? userId)
        {
            List<TblRole> controlledRoles = GetAppRolesForUser(userId);
            List<int> controlledRoleIds = new List<int>();
            foreach (TblRole controlledRole in controlledRoles)
            {
                foreach (TblAppRole childRole in controlledRole.ChildRoles)
                {
                    controlledRoleIds.Add(childRole.Role.RoleId);
                }
            }
            return controlledRoleIds;
        }

        /// <summary>
        /// Get the default instance based on the users permissions
        /// </summary>
        /// <returns>Viper if they can access Viper, VMACS.VMTH otherwise</returns>
        public string GetDefaultInstanceForUser()
        {
            if(IsAllowedTo("AccessInstance", "VIPER"))
            {
                return "VIPER";
            }
            return "VMACS.VMTH";
        }

        /// <summary>
        /// Get a role, checking to ensure it's in the given instance
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="roleId">The id of the role</param>
        /// <returns>The role if it's in the given instance, null otherwise</returns>
        public TblRole? GetRoleInInstance(string instance, int roleId)
        {
            var tblRole = _context.TblRoles.Find(roleId);
            return tblRole != null && RoleBelongsToInstance(instance, tblRole)
                ? tblRole
                : null;
        }

        /// <summary>
        /// Get a permission, checking to ensure it's in the given instance
        /// </summary>
        /// <param name="instance">The instanec</param>
        /// <param name="permissionId">The id of the permission</param>
        /// <returns>The permission, if it's in the given instance, null otherwise</returns>
        public TblPermission? GetPermissionInInstance(string instance, int permissionId)
        {
            var tblpermission = _context.TblPermissions.Find(permissionId);
            return tblpermission != null && PermissionBelongsToInstance(instance, tblpermission)
                ? tblpermission
                : null;
        }
    }
}
