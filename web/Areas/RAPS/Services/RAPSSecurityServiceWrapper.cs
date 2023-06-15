using System.Linq.Expressions;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Services
{
    public class RAPSSecurityServiceWrapper : IRAPSSecurityServiceWrapper
    {
        private readonly RAPSSecurityService _RAPSSecurityService;

        public RAPSSecurityServiceWrapper(RAPSSecurityService myDependency)
        {
            _RAPSSecurityService = myDependency;
        }

        static public bool IsValidInstance(string Instance)
        {
            return RAPSSecurityService.IsValidInstance(Instance);
        }

        static public bool IsVMACSInstance(string Instance)
        {
            return RAPSSecurityService.IsVMACSInstance(Instance);
        }

        static public Expression<Func<TblRole, bool>> FilterRolesToInstance(string instance)
        {
            return RAPSSecurityService.FilterRolesToInstance(instance);
        }

        public bool IsAllowedTo(string action, string? Instance)
        {
            return _RAPSSecurityService.IsAllowedTo(action, Instance);
        }

        public bool IsAllowedTo(string action, string Instance, TblRole Role)
        {
            return _RAPSSecurityService.IsAllowedTo(action, Instance, Role);
        }

        public List<TblRole> GetAppRolesForUser(string? userId)
        {
            return _RAPSSecurityService.GetAppRolesForUser(userId);
        }

        public virtual List<int> GetControlledRoleIds(string? userId)
        {
            return _RAPSSecurityService.GetControlledRoleIds(userId);
        }
    }

    public interface IRAPSSecurityServiceWrapper
    {
        static bool IsValidInstance(string Instance) => throw new NotImplementedException();

        static bool IsVMACSInstance(string Instance) => throw new NotImplementedException();

        static Expression<Func<TblRole, bool>> FilterRolesToInstance(string instance) => throw new NotImplementedException();

        bool IsAllowedTo(string action, string? Instance);

        bool IsAllowedTo(string action, string Instance, TblRole Role);

        List<TblRole> GetAppRolesForUser(string? userId);

        List<int> GetControlledRoleIds(string? userId);
    }
}
