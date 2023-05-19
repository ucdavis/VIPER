using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;

namespace Viper.Classes
{
	public class UserWrapper : IUserWrapper
	{
		public IEnumerable<TblRole> GetRoles(RAPSContext rapsContext, AaudUser user)
		{
			return UserHelper.GetRoles(rapsContext, user);
		}

		public bool IsInRole(RAPSContext rapsContext, AaudUser user, string roleName)
		{
			return UserHelper.IsInRole(rapsContext, user, roleName);
		}

		public IEnumerable<TblPermission> GetAssignedPermissions(RAPSContext rapsContext, AaudUser user, Boolean deny = false)
		{
			return UserHelper.GetAssignedPermissions(rapsContext, user, deny);
		}

		public IEnumerable<TblPermission> GetAllPermissions(RAPSContext rapsContext, AaudUser user)
		{
			return UserHelper.GetAllPermissions(rapsContext, user);
		}

		public bool HasPermission(RAPSContext? rapsContext, AaudUser? user, string permissionName)
		{
			return UserHelper.HasPermission(rapsContext, user, permissionName);
		}

		public AaudUser? GetByLoginId(AAUDContext? aaudContext, string? loginId)
		{
			return UserHelper.GetByLoginId(aaudContext, loginId);
		}

		public AaudUser? GetCurrentUser()
		{
			return UserHelper.GetCurrentUser();
		}

		public AaudUser? GetTrueCurrentUser()
		{
			return UserHelper.GetTrueCurrentUser();
		}

		public bool IsEmulating()
		{
			return UserHelper.IsEmulating();
		}

	}

	public interface IUserWrapper
	{
		IEnumerable<TblRole> GetRoles(RAPSContext rapsContext, AaudUser user);

		bool IsInRole(RAPSContext rapsContext, AaudUser user, string roleName);

		IEnumerable<TblPermission> GetAssignedPermissions(RAPSContext rapsContext, AaudUser user, Boolean deny = false);

		IEnumerable<TblPermission> GetAllPermissions(RAPSContext rapsContext, AaudUser user);

		bool HasPermission(RAPSContext? rapsContext, AaudUser? user, string permissionName);

		AaudUser? GetByLoginId(AAUDContext? aaudContext, string? loginId);

		AaudUser? GetCurrentUser();

		AaudUser? GetTrueCurrentUser();

		bool IsEmulating();
	}
}
