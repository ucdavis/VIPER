using Microsoft.Extensions.Caching.Memory;
using System;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;
using VIPER;

namespace Viper
{
    /// <summary>
    /// Utility class (static) with methods for easily get a user from AAUD and find roles and permissions from RAPS
    /// </summary>
    public static class UserHelper
    {
        #region public static IEnumerable<Viper.Models.RAPS.TblRole> GetRoles(RAPSContext rapsContext, AaudUser user)
        /// <summary>
        /// Returns a list of roles for the user
        /// </summary>
        /// <param name="rapsContext">Dependency injection of the context</param>
        /// <param name="user">Must pass an AaudUser object</param>
        /// <returns>Enumerable list of roles for the user</returns>
        public static IEnumerable<TblRole> GetRoles(RAPSContext rapsContext, AaudUser user)
        {
            var result =
                        (from role in rapsContext.TblRoles
                         join memberRoles in rapsContext.TblRoleMembers
                             on role.RoleId equals memberRoles.RoleId
                         where memberRoles.MemberId == user.MothraId
                         select role).ToList();

            return result;
        }
        #endregion

        #region public static bool IsInRole(RAPSContext rapsContext, AaudUser user, string roleName)
        /// <summary>
        /// Check if the user is assigned to a role
        /// </summary>
        /// <param name="rapsContext">Dependency injection of the context</param>
        /// <param name="user">Must pass an AaudUser object</param>
        /// <param name="roleName">The name of the role to check</param>
        /// <returns>Whether or not the user is in the role specified</returns>
        public static bool IsInRole(RAPSContext rapsContext, AaudUser user, string roleName)
        {
            var result =
                        (from role in rapsContext.TblRoles
                         join memberRoles in rapsContext.TblRoleMembers
                             on role.RoleId equals memberRoles.RoleId
                         where role.Role.ToLower() == roleName.ToLower()
                         select role).FirstOrDefault();

            if (result != null)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        #endregion

        #region public static IEnumerable<TblPermission> GetAssignedPermissions(RAPSContext rapsContext, AaudUser user)
        /// <summary>
        /// Returns a list of permission items assigned directly to the user
        /// </summary>
        /// <param name="rapsContext">Dependency injection of the context</param>
        /// <param name="user">Must pass an AaudUser object</param>
        /// <returns>Enumerable list of permission items assigned directly to the user</returns>
        public static IEnumerable<TblPermission> GetAssignedPermissions(RAPSContext rapsContext, AaudUser user)
        {
            var result =
                        (from permission in rapsContext.TblPermissions
                         join memberPermissions in rapsContext.TblMemberPermissions
                             on permission.PermissionId equals memberPermissions.PermissionId
                         where memberPermissions.MemberId == user.MothraId
                         select permission).ToList();

            return result;
        }
        #endregion

        #region public static IEnumerable<TblPermission> GetInheritedPermissions(RAPSContext rapsContext, AaudUser user)
        /// <summary>
        /// Returns a list of permission items assigned to the user from roles
        /// </summary>
        /// <param name="rapsContext">Dependency injection of the context</param>
        /// <param name="user">Must pass an AaudUser object</param>
        /// <returns>Enumerable list of permission items assigned to the user from roles</returns>
        public static IEnumerable<TblPermission> GetInheritedPermissions(RAPSContext rapsContext, AaudUser user)
        {
            var result =
                        (from permission in rapsContext.TblPermissions
                         join rolePermissions in rapsContext.TblRolePermissions
                             on permission.PermissionId equals rolePermissions.PermissionId
                         join memberRole in rapsContext.TblRoleMembers
                             on rolePermissions.RoleId equals memberRole.RoleId
                         where memberRole.MemberId == user.MothraId
                         select permission).ToList();

            return result;
        }
        #endregion

        #region public static IEnumerable<TblPermission> GetAllPermissions(RAPSContext rapsContext, AaudUser user)
        /// <summary>
        /// Returns a list of all permission items assigned to the user either from roles or directly
        /// </summary>
        /// <param name="rapsContext">Dependency injection of the context</param>
        /// <param name="user">Must pass an AaudUser object</param>
        /// <returns>Returns a list of all permission items assigned to the user either from roles or directly</returns>
        public static IEnumerable<TblPermission> GetAllPermissions(RAPSContext rapsContext, AaudUser user)
        {
            var assigned = GetAssignedPermissions(rapsContext, user);
            var inherited = GetInheritedPermissions(rapsContext, user);
            return assigned.Union(inherited);
        }
        #endregion

        #region public static bool IsInRole(RAPSContext rapsContext, AaudUser user, string roleName)
        /// <summary>
        /// Check if the user is assigned to a role
        /// </summary>
        /// <param name="rapsContext">Dependency injection of the context</param>
        /// <param name="user">Must pass an AaudUser object</param>
        /// <param name="roleName">The name of the role to check</param>
        /// <returns>Whether or not the user is in the role specified</returns>
        public static bool HasPermission(RAPSContext rapsContext, AaudUser user, string permissionName)
        {
            var perrmissions = GetAllPermissions(rapsContext, user);

            TblPermission test = new TblPermission();

            if (perrmissions.Any(p => p.Permission.ToLower() == permissionName.ToLower()))
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        #endregion

        #region public static AaudUser? GetByLoginId(AAUDContext aaudContext, string? loginId)
        /// <summary>
        /// Load a AaudUser object by a loginid (campus user name)
        /// </summary>
        /// <param name="aaudContext">Dependency injection of the context</param>
        /// <param name="loginId">The login id of the user we are loading</param>
        /// <returns>An AaudUser object for the given loginid</returns>
        public static AaudUser? GetByLoginId(AAUDContext aaudContext, string? loginId)
        {
            if (loginId != null)
            {
                string userLoginId = loginId.ToLower();
                AaudUser? user = null;

                // if we have already cached this user as an UCD user then create a Viper user from that object
                if (HttpHelper.Cache != null)
                {
                    user = HttpHelper.Cache.Get<AaudUser>("AaudUser-" + userLoginId);
                }

                if (user != null)
                {
                    return user;
                }
                else if (HttpHelper.Cache != null)
                {
                    user = HttpHelper.Cache.GetOrCreate("AaudUser-" + userLoginId, entry =>
                    {
                        AaudUser? aaudUser = aaudContext.AaudUsers.FirstOrDefault(m => m.LoginId == loginId);
                        if (aaudUser != null)
                        {
                            return aaudUser;
                        }
                        else
                        {
                            return user;
                        }

                    });
                }
            }

            return null;
        }
        #endregion
    }
}
