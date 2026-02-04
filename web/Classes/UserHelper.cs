using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Viper.Classes.SQLContext;
using Viper.Models;
using Viper.Models.AAUD;
using Viper.Models.RAPS;
using Web.Authorization;

namespace Viper
{
    /// <summary>
    /// Utility class (static) with methods for easily get a user from AAUD and find roles and permissions from RAPS
    /// </summary>
    public class UserHelper : IUserHelper
    {
        #region public ClientData GetClientData()
        /// <summary>
        /// Returns client data for the current context
        /// </summary>
        /// <returns>ClientData object</returns>
        public ClientData GetClientData()
        {
            return new ClientData
            {
                IP = HttpHelper.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = HttpHelper.HttpContext?.Request.Headers["User-Agent"].ToString(),
                Referrer = HttpHelper.HttpContext?.Request.Headers["Referer"].ToString()
            };

        }
        #endregion


        #region public IEnumerable<Viper.Models.RAPS.TblRole> GetRoles(RAPSContext rapsContext, AaudUser user)
        /// <summary>
        /// Returns a list of roles for the user
        /// </summary>
        /// <param name="rapsContext">Dependency injection of the context</param>
        /// <param name="user">Must pass an AaudUser object</param>
        /// <returns>Enumerable list of roles for the user</returns>
        public IEnumerable<TblRole> GetRoles(RAPSContext rapsContext, AaudUser user)
        {
            var result = new List<TblRole>();

            if (HttpHelper.Cache != null && rapsContext != null)
            {

                result = HttpHelper.Cache.GetOrCreate("Roles-" + user.LoginId, entry =>
                {
                    return (from role in rapsContext.TblRoles
                            join memberRoles in rapsContext.TblRoleMembers
                                on role.RoleId equals memberRoles.RoleId
                            where memberRoles.MemberId == user.MothraId
                            && (memberRoles.StartDate == null || memberRoles.StartDate <= DateTime.Today)
                            && (memberRoles.EndDate == null || memberRoles.EndDate >= DateTime.Today)
                            select role).ToList();
                });

                if (result != null)
                {
                    return result;
                }
                else
                {
                    return new List<TblRole>();
                }

            }
            else
            {
                return result;
            }

        }
        #endregion

        #region public bool IsInRole(RAPSContext rapsContext, AaudUser user, string roleName)
        /// <summary>
        /// Check if the user is assigned to a role
        /// </summary>
        /// <param name="rapsContext">Dependency injection of the context</param>
        /// <param name="user">Must pass an AaudUser object</param>
        /// <param name="roleName">The name of the role to check</param>
        /// <returns>Whether or not the user is in the role specified</returns>
        public bool IsInRole(RAPSContext rapsContext, AaudUser user, string roleName)
        {
            if (user.LoginId == HttpHelper.HttpContext?.User?.Identity?.Name)
            {
                var claims = HttpHelper.HttpContext?.User?.Claims;

                if (claims != null)
                {

                    foreach (var claim in claims)
                    {
                        if (claim.Type == ClaimTypes.Role && claim.Value == roleName)
                        {
                            return true;
                        }

                    }

                }

                return false;
            }
            else
            {
                var roles = GetRoles(rapsContext, user);
                return roles.Any(role => role.Role.Equals(roleName, StringComparison.OrdinalIgnoreCase));

            }

        }
        #endregion

        #region public IEnumerable<TblPermission> GetAssignedPermissions(RAPSContext rapsContext, AaudUser user)
        /// <summary>
        /// Returns a list of permission items assigned directly to the user
        /// </summary>
        /// <param name="rapsContext">Dependency injection of the context</param>
        /// <param name="user">Must pass an AaudUser object</param>
        /// <param name="deny">if true, return assigned permissions with deny flag set</param>
        /// <returns>Enumerable list of permission items assigned directly to the user</returns>
        public IEnumerable<TblPermission> GetAssignedPermissions(RAPSContext rapsContext, AaudUser user, Boolean deny = false)
        {
            var result = new List<TblPermission>();

            if (HttpHelper.Cache != null && rapsContext != null)
            {
                result = HttpHelper.Cache.GetOrCreate("PermissionsAssigned-" + user.LoginId + "-" + deny, entry =>
                {
                    return (from permission in rapsContext.TblPermissions
                            join memberPermissions in rapsContext.TblMemberPermissions
                                on permission.PermissionId equals memberPermissions.PermissionId
                            where memberPermissions.MemberId == user.MothraId
                            && memberPermissions.Access == (deny ? 0 : 1)
                            && (memberPermissions.StartDate == null || memberPermissions.StartDate <= DateTime.Today)
                            && (memberPermissions.EndDate == null || memberPermissions.EndDate >= DateTime.Today)
                            select permission).ToList();
                });

                if (result != null)
                {
                    return result;
                }
                else
                {
                    return new List<TblPermission>();
                }

            }
            else
            {
                return result;
            }

        }
        #endregion

        #region public IEnumerable<TblPermission> GetInheritedPermissions(RAPSContext rapsContext, AaudUser user)
        /// <summary>
        /// Returns a list of permission items assigned to the user from roles
        /// </summary>
        /// <param name="rapsContext">Dependency injection of the context</param>
        /// <param name="user">Must pass an AaudUser object</param>
        /// <param name="deny">if true, return permissions with deny flag set on tblRolePermissions</param>
        /// <returns>Enumerable list of permission items assigned to the user from roles</returns>
        public static IEnumerable<TblPermission> GetInheritedPermissions(RAPSContext rapsContext, AaudUser user, Boolean deny = false)
        {
            List<TblPermission>? result = null;

            if (HttpHelper.Cache != null && rapsContext != null)
            {
                result = HttpHelper.Cache.GetOrCreate("PermissionsInherited-" + user.LoginId + "-" + deny, entry =>
                {
                    return (from permission in rapsContext.TblPermissions
                            join rolePermissions in rapsContext.TblRolePermissions
                                on permission.PermissionId equals rolePermissions.PermissionId
                            join memberRole in rapsContext.TblRoleMembers
                                on rolePermissions.RoleId equals memberRole.RoleId
                            where memberRole.MemberId == user.MothraId
                            && rolePermissions.Access == (deny ? 0 : 1)
                            && (memberRole.StartDate == null || memberRole.StartDate <= DateTime.Today)
                            && (memberRole.EndDate == null || memberRole.EndDate >= DateTime.Today)
                            select permission).ToList();
                });
            }

            return result ?? new List<TblPermission>();
        }
        #endregion

        #region public IEnumerable<TblPermission> GetAllPermissions(RAPSContext rapsContext, AaudUser user)
        /// <summary>
        /// Returns a list of all permission items assigned to the user either from roles or directly
        /// </summary>
        /// <param name="rapsContext">Dependency injection of the context</param>
        /// <param name="user">Must pass an AaudUser object</param>
        /// <returns>Returns a list of all permission items assigned to the user either from roles or directly</returns>
        public IEnumerable<TblPermission> GetAllPermissions(RAPSContext rapsContext, AaudUser user)
        {
            var assignedDeny = GetAssignedPermissions(rapsContext, user, true);
            var inheritedDeny = GetInheritedPermissions(rapsContext, user, true);

            var assigned = GetAssignedPermissions(rapsContext, user);
            var inherited = GetInheritedPermissions(rapsContext, user);
            return assigned
                .Union(inherited)
                .Except(assignedDeny.Union(inheritedDeny));
        }
        #endregion

        #region public bool HasPermission(RAPSContext? rapsContext, AaudUser? user, string permissionName)
        /// <summary>
        /// Check if the user is assigned to a role
        /// </summary>
        /// <param name="rapsContext">Dependency injection of the context</param>
        /// <param name="user">Must pass an AaudUser object</param>
        /// <param name="roleName">The name of the role to check</param>
        /// <returns>Whether or not the user is in the role specified</returns>
        public bool HasPermission(RAPSContext? rapsContext, AaudUser? user, string permissionName)
        {
            if (rapsContext != null && user != null)
            {
                var permissions = GetAllPermissions(rapsContext, user);

                if (permissions.Any(p => p.Permission.ToLower() == permissionName.ToLower()))
                {
                    return true;
                }

            }

            return false;

        }
        #endregion

        #region public AaudUser? GetByLoginId(AAUDContext aaudContext, string? loginId)
        /// <summary>
        /// Load a AaudUser object by a loginid (campus user name)
        /// </summary>
        /// <param name="aaudContext">Dependency injection of the context</param>
        /// <param name="loginId">The login id of the user we are loading</param>
        /// <returns>An AaudUser object for the given loginid</returns>
        public AaudUser? GetByLoginId(AAUDContext? aaudContext, string? loginId)
        {
            if (loginId != null)
            {
                try
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
                    else if (HttpHelper.Cache != null && aaudContext != null)
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

                        return user;
                    }
                }
                catch (Exception ex)
                {
                    HttpHelper.Logger.Error(ex);
                    return null;
                }

            }

            return null;
        }
        #endregion

        #region public AaudUser? GetCurrentUser()
        /// <summary>
        /// Gets the current logged in user
        /// </summary>
        /// <returns>An AaudUser object for the current user</returns>
        public AaudUser? GetCurrentUser()
        {
            try
            {
                AAUDContext? aaudContext = (AAUDContext?)HttpHelper.HttpContext?.RequestServices.GetService(typeof(AAUDContext));
                AaudUser? currentUser = GetByLoginId(aaudContext, HttpHelper.HttpContext?.User?.Identity?.Name);

                return currentUser;
            }
            catch (Exception ex)
            {
                HttpHelper.Logger.Error(ex);
                return null;
            }
        }
        #endregion

        #region public AaudUser? GetTrueCurrentUser()
        /// <summary>
        /// Gets the current true (underlying user if emulating) logged in user
        /// </summary>
        /// <returns>An AaudUser object for the true current user</returns>
        public AaudUser? GetTrueCurrentUser()
        {
            try
            {
                if (HttpHelper.HttpContext?.User != null)
                {
                    var claims = HttpHelper.HttpContext?.User.Claims.ToList();
                    AAUDContext? aaudContext = (AAUDContext?)HttpHelper.HttpContext?.RequestServices.GetService(typeof(AAUDContext));
                    AaudUser? trueUser = GetByLoginId(aaudContext, claims?.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value);

                    if (trueUser != null)
                    {
                        return trueUser;
                    }
                    else
                    {
                        return GetCurrentUser();
                    }


                }
                else { return GetCurrentUser(); }
            }
            catch (Exception ex)
            {
                HttpHelper.Logger.Error(ex);
                return null;
            }
        }
        #endregion

        #region public bool IsEmulating()
        /// <summary>
        /// Checks if the current user is emulating
        /// </summary>
        /// <returns>If emulating, returns true, if not: false</returns>
        public bool IsEmulating()
        {
            string? trueLoginId = GetTrueCurrentUser()?.LoginId;

            if (trueLoginId != null && HttpHelper.Cache != null)
            {
                string? encryptedEmulatedLoginId = HttpHelper.Cache.Get<string>(ClaimsTransformer.EmulationCacheNamePrefix + trueLoginId);

                return encryptedEmulatedLoginId != null;
            }

            return false;
        }
        #endregion

        public void ClearCachedRolesAndPermissions(AaudUser? user)
        {
            if (user != null && HttpHelper.Cache != null)
            {
                HttpHelper.Cache.Remove("Roles-" + user.LoginId);
                HttpHelper.Cache.Remove("PermissionsAssigned-" + user.LoginId + "-" + true);
                HttpHelper.Cache.Remove("PermissionsAssigned-" + user.LoginId + "-" + false);
                HttpHelper.Cache.Remove("PermissionsInherited-" + user.LoginId + "-" + false);
                HttpHelper.Cache.Remove("PermissionsInherited-" + user.LoginId + "-" + false);
            }

        }
    }

    public interface IUserHelper
    {
        ClientData GetClientData();

        IEnumerable<TblRole> GetRoles(RAPSContext rapsContext, AaudUser user);

        bool IsInRole(RAPSContext rapsContext, AaudUser user, string roleName);

        IEnumerable<TblPermission> GetAssignedPermissions(RAPSContext rapsContext, AaudUser user, Boolean deny = false);

        IEnumerable<TblPermission> GetAllPermissions(RAPSContext rapsContext, AaudUser user);

        bool HasPermission(RAPSContext? rapsContext, AaudUser? user, string permissionName);

        AaudUser? GetByLoginId(AAUDContext? aaudContext, string? loginId);

        AaudUser? GetCurrentUser();

        AaudUser? GetTrueCurrentUser();

        bool IsEmulating();

        void ClearCachedRolesAndPermissions(AaudUser? user);
    }
}
