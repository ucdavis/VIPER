using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Cryptography;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Services
{
    public class RAPSSecurityService
    {
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

        static public bool IsVMACSInstance(string Instance)
        {
            return Instance.StartsWith("VMACS.");
        }

        public bool IsAllowedTo(string action, string? Instance)
        {
            if(UserHelper.HasPermission(_context, UserHelper.GetCurrentUser(), "RAPS.Admin"))
            {
                return true;
            }
            switch(action)
            {
                case "ViewAllRoles":
                    return Instance != null && IsVMACSInstance(Instance) && UserHelper.HasPermission(_context, UserHelper.GetCurrentUser(), "RAPS.ViewRoles");
                //case "EditRoleMembership": return UserHelper.HasPermission(_context, UserHelper.GetCurrentUser(), "RAPS.EditRoleMembership");
                default:
                    return UserHelper.HasPermission(_context, UserHelper.GetCurrentUser(), "RAPS." + action);
            }
        }

        public bool IsAllowedTo(string action, string Instance, TblRole Role)
        {
            AaudUser User = UserHelper.GetCurrentUser();
            if (UserHelper.HasPermission(_context, User, "RAPS.Admin"))
            {
                return true;
            }

            switch (action)
            {
                case "EditRoleMembership":
                    List<int> controlledRoleIds = GetControlledRoleIds(User.MothraId);
                    return controlledRoleIds.Contains(Role.RoleId)
                            || UserHelper.HasPermission(_context, User, "RAPS.EditRoleMembership");
                default:
                    return false;
            }
        }

        public List<TblRole> GetAppRolesForUser(string userId)
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

        public List<int> GetControlledRoleIds(string userId)
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
    }

}
