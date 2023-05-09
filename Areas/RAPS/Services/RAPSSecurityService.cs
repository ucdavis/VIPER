using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Cryptography;
using Viper.Classes.SQLContext;
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

        static public bool isValidInstance(string Instance)
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

        static public bool isVMACSInstance(string Instance)
        {
            return Instance.StartsWith("VMACS.");
        }

        public bool can(string action, string? Instance)
        {
            if(UserHelper.HasPermission(_context, UserHelper.GetCurrentUser(), "RAPS.Admin"))
            {
                return true;
            }
            switch(action)
            {
                case "ViewAllRoles":
                    return Instance != null && isVMACSInstance(Instance) && UserHelper.HasPermission(_context, UserHelper.GetCurrentUser(), "RAPS.ViewRoles");
                //case "EditRoleMembership": return UserHelper.HasPermission(_context, UserHelper.GetCurrentUser(), "RAPS.EditRoleMembership");
                default:
                    return UserHelper.HasPermission(_context, UserHelper.GetCurrentUser(), "RAPS." + action);
            }
        }

        public bool can(string action, string? Instance, TblRole Role)
        {
            if (UserHelper.HasPermission(_context, UserHelper.GetCurrentUser(), "RAPS.Admin"))
            {
                return true;
            }

            List<TblRole> roles = _context.TblRoles
                .Where((r => r.Application == 1))
                //.Include((r => r.TblAppRoles))
                .ToList();
            switch (action)
            {
                case "EditRoleMembership": 
                    return UserHelper.HasPermission(_context, UserHelper.GetCurrentUser(), "RAPS.EditRoleMembership");
                default:
                    return false;
            }
        }

        public List<TblRole> getAppRolesForUser(string userId)
        {
            List<TblRole> roles = _context.TblRoles
                    //.Include(rm => rm.Role)
                    .Include(r => r.TblRoleMembers)
                    .Include(r => r.ChildRoles)
                        .ThenInclude(cr => cr.Role)
                    .Where(r => r.Application == 1)
                    .Where(r => r.TblRoleMembers.Any(rm => rm.MemberId == userId))
                    .ToList();
            return roles;
        }
    }

}
