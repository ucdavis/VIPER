using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Polly;
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

        static public bool IsVMACSInstance(string instance)
        {
            return instance.StartsWith("VMACS.");
        }

        public bool RoleBelongsToInstance(string instance, TblRole role)
        {
            string roleName = role.Role.ToUpper();
            if(instance.ToUpper() == "VIPER")
            {
                return !roleName.StartsWith("VMACS.") && !roleName.StartsWith("VIPERFORMS");
            }
            return roleName.StartsWith(instance.ToUpper());
        }

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

        public bool IsAllowedTo(string action, string? instance = null)
        {
            if(_userWrapper.HasPermission(_context, _userWrapper.GetCurrentUser(), "RAPS.Admin"))
            {
                return true;
            }
            switch(action)
            {
                case "ViewAllRoles":
                    return instance  != null && IsVMACSInstance(instance) && _userWrapper.HasPermission(_context, _userWrapper.GetCurrentUser(), "RAPS.ViewRoles");
                case "AccessInstnace":
                    return instance != null 
                        && (
                            IsVMACSInstance(instance) && _userWrapper.HasPermission(_context, _userWrapper.GetCurrentUser(), "RAPS.ViewRoles")
                            || GetControlledRoleIds(_userWrapper.GetCurrentUser()?.MothraId).Count() > 0
                            );
				//case "EditRoleMembership": return _userWrapper.HasPermission(_context, _userWrapper.GetCurrentUser(), "RAPS.EditRoleMembership");
				default:
                    return _userWrapper.HasPermission(_context, _userWrapper.GetCurrentUser(), "RAPS." + action);
            }
        }

        public bool IsAllowedTo(string action, string Instance, TblRole Role)
        {
            AaudUser? User = _userWrapper.GetCurrentUser();
            if (_userWrapper.HasPermission(_context, User, "RAPS.Admin"))
            {
                return true;
            }

            switch (action)
            {
                case "EditRoleMembership":
                    List<int> controlledRoleIds = GetControlledRoleIds(User?.MothraId);
                    return controlledRoleIds.Contains(Role.RoleId)
                            || _userWrapper.HasPermission(_context, User, "RAPS.EditRoleMembership");
                default:
                    return false;
            }
        }

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

        public string GetDefaultInstanceForUser()
        {
            if(IsAllowedTo("AccessInstance", "VIPER"))
            {
                return "VIPER";
            }
            return "VMACS.VMTH";
        }

        public TblRole? GetRoleInInstance(string instance, int roleId)
        {
            var tblRole = _context.TblRoles.Find(roleId);
            return tblRole != null && RoleBelongsToInstance(instance, tblRole)
                ? tblRole
                : null;
        }

        public TblPermission? GetPermissionInInstance(string instance, int permissionId)
        {
            var tblpermission = _context.TblPermissions.Find(permissionId);
            return tblpermission != null && PermissionBelongsToInstance(instance, tblpermission)
                ? tblpermission
                : null;
        }
    }
}
