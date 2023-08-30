﻿using Amazon.Runtime.Internal.Transform;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using Viper.Areas.RAPS.Models;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Services
{
    public class VMACSExport
    {
        private readonly Classes.SQLContext.RAPSContext _RAPSContext;

        public IUserHelper UserHelper;
        private readonly bool _onProduction;
        private readonly string _credentials;
        private static readonly F5HttpRequest _f5HttpRequest= new();
        private readonly Dictionary<string, string> _vmacsServers = new()
        {
            { "vmth-test", "https://vmacs-test.vetmed.ucdavis.edu" },
            { "vmth-dev", "https://vmacs-dev.vetmed.ucdavis.edu" },
            { "vmth-prod", "https://vmacs-vmth.vetmed.ucdavis.edu" },
            { "vmth-qa", "https://vmacs-qa.vetmed.ucdavis.edu" },
            { "vmth-train", "https://vmacs-train.vetmed.ucdavis.edu" },
            { "vmlf-prod", "https://vmacs-vmlf.vetmed.ucdavis.edu" },
            { "vmlf-qa", "https://vmacs-qa-vmlf.vetmed.ucdavis.edu" },
            { "ucvmcsd-prod", "https://vmacs-sd.vetmed.ucdavis.edu" },
            { "ucvmcsd-qa", "https://vmacs-qa-ucvmcsd.vetmed.ucdavis.edu" }
        };
        private static readonly string apiPermissionURL = "/Vmacs2/rest/raps/permission";

        public VMACSExport(bool onProduction, RAPSContext RAPSContext, string credentials)
        {
            //_HttpRequest = new F5HttpRequest();
            _RAPSContext = RAPSContext;
            _onProduction = onProduction;
            _credentials = credentials;
            UserHelper = new UserHelper();
        }

        /// <summary>
        /// Export to multiple instances of VMACS. Intended to be used by routines (can set mothraId for audit log)
        /// </summary>
        /// <param name="instances">VMTH,VMLF,UCVMCSD</param>
        /// <param name="loginid">LoginId of specific user to push</param>
        /// <param name="roleIds">RoleIds of specific role(s) to push</param>
        /// <param name="mothraId">MothraId of logged in user, or hard coded user to record in log</param>
        /// <param name="action">Action for log</param>
        /// <param name="debugOnly">If true, don't send, just log</param>
        public async Task<List<string>> ExportToInstances(string instances, string? mothraId, string? loginid = null, string? roleIds = null,
#pragma warning disable IDE0060 // Remove unused parameter
                string action = "", bool debugOnly = false)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            _ = mothraId ?? UserHelper.GetCurrentUser()?.MothraId;
            List<string> messages = new();
            foreach(string instance in instances.Split(","))
            {
                if (!string.IsNullOrEmpty(instance))
                {
                    await ExportToVMACS(instance: instance, loginId: loginid, roleIds: roleIds, messages: messages, debugOnly: debugOnly);
                }
            }
            return messages;
        }

        /// <summary>
        /// Export to a single instance, and optionally a specific server. Note that this is the method to allow a specific server to be targeted.
        /// </summary>
        /// <param name="instance">VMTH,VMLF,UCVMCSD</param>
        /// <param name="server">test,dev,prod,qa,train</param>
        /// <param name="loginId">A specific user login id to only export that user</param>
        /// <param name="roleIds">Only export users with these role ids</param>
        /// <param name="debugOnly">if true, don't send, just log</param>
        public async Task<List<string>> ExportToVMACS(string instance, string? server = null, string? loginId = null, 
            string? roleIds = null, List<string>? messages = null, bool debugOnly = false)
        {
            messages ??= new List<string>();
            server ??= GetDefaultServer();
            string Url = GetServerUrl(instance, server);
            if(Url.Length > 0)
            {
                Url += apiPermissionURL;
                string rolePrefix = "VMACS." + instance;
                var userList = GetUsers(rolePrefix, loginId ?? "", roleIds ?? "");
                VMACSExportData exportData = new()
                {
                    Users = GetExportUsers(userList, rolePrefix),
                    Permissions = GetPermissions(rolePrefix)
                };

                RecordMessage(messages, "User/Roles Retrieved: " + userList.Count);
                RecordMessage(messages, "Permissions Retrieved: " + exportData.Permissions.Count);
                RecordMessage(messages, "VMACS Export Data: " + JsonSerializer.Serialize(exportData));

                if (!debugOnly)
                {
                    using StringContent exportContent = new(JsonSerializer.Serialize(exportData), Encoding.UTF8, "application/json");
                    using HttpRequestMessage request = new()
                    {
                        RequestUri = new Uri(Url),
                        Method = HttpMethod.Post,
                        Content = exportContent
                    };
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", GetAuthorization());
                    
                    RecordMessage(messages, "Sending data to: " + Url);
                    
                    try
                    {
                        using HttpResponseMessage response = await _f5HttpRequest.Send(request);
                        RecordMessage(messages, "Response Status: " + response.StatusCode);
                        VmacsResponse vmacsResponse = await ParseResponse(response);
                        RecordMessage(messages, "Response: " + JsonSerializer.Serialize(vmacsResponse));
                    }
                    catch (Exception ex) {
                        HttpHelper.Logger.Log(NLog.LogLevel.Warn, ex);
                        RecordMessage(messages, "Error: " + ex.Message + " " + ex?.StackTrace);
                        if(ex?.InnerException != null)
                        {
                            RecordMessage(messages, "Error: " + ex.InnerException.Message + " " + ex.InnerException?.StackTrace);
                        }
                    }
                }
            }

            return messages;
        }

        private string GetAuthorization()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(_credentials);
            return System.Convert.ToBase64String(bytes);
        }

        private static async Task<VmacsResponse> ParseResponse(HttpResponseMessage response)
        {
            VmacsResponse vmacsResponse = new()
            {
                Success = response.IsSuccessStatusCode
            };
            string responseBody = await response.Content.ReadAsStringAsync();
            try
            {
                JsonSerializer.Deserialize<VmacsResponse>(responseBody);
            }
            catch (Exception)
            {
                vmacsResponse.Success = false;
                vmacsResponse.ErrorMessage = "Invalid response from VMACS: " + responseBody;
            }
            return vmacsResponse;
        }

        private List<UserList> GetUsers(string rolePrefix, string loginId="", string roleIds="")
        {
            var users = from user in _RAPSContext.VwAaudUser
                         join rm in _RAPSContext.TblRoleMembers on user.MothraId equals rm.MemberId
                         join role in _RAPSContext.TblRoles on rm.RoleId equals role.RoleId
                         where (rm.StartDate == null || rm.StartDate <= DateTime.Now)
                         && (rm.EndDate == null || rm.EndDate > DateTime.Now)
                         && (user.Current || user.Future)
                         && (role.Role.StartsWith(rolePrefix))
                         select new UserList()
                         {
                             LoginId = user.LoginId,
                             MothraId = user.MothraId,
                             MailId = user.MailId,
                             AccessCode = role.AccessCode,
                             RoleId = role.RoleId
                         };
            if (roleIds.Length > 0)
            {
                List<int> roleIdList = new(roleIds.Split(",").Select(int.Parse));
                users = users.Where(row => roleIdList.Contains(row.RoleId));
            }
            if (loginId.Length > 0)
            {
                users = users.Where(row => row.LoginId.Equals(loginId));
            }

            return users
                .OrderBy(u => u.LoginId)
                .ThenBy(u => u.RoleId)
                .ToList();
        }

        private List<VMACSExportPermission> GetPermissions(string rolePrefix)
        {
            var permissions = _RAPSContext.TblPermissions
                .Where(p => p.Permission.StartsWith("VMACS."))
                .Include(p => p.TblRolePermissions
                    .Where(rp => rp.Role.Role.StartsWith(rolePrefix))
                    .OrderBy(rp => rp.Role.AccessCode))
                .ThenInclude(rp => rp.Role)
                .OrderBy(p => p.Permission)
                .ThenBy(p => p.PermissionId)
                .ToList();

            List<VMACSExportPermission> vmacsExportPermissions = new();
            foreach (var permission in permissions)
            {
                if(permission.TblRolePermissions.Count == 0)
                {
                    continue;
                }
                string accessCodes = "";
                foreach (var rolePermission in permission.TblRolePermissions)
                {
                    accessCodes += rolePermission.Role.AccessCode;
                }
                var accessCodeArray = accessCodes.ToArray();
                Array.Sort(accessCodeArray);
                accessCodes = new string(accessCodes);
                
                var permissionSplit = permission.Permission.Split(".");
                vmacsExportPermissions.Add(new VMACSExportPermission()
                {
                    Id = permission.PermissionId,
                    Group = permissionSplit[1],
                    Permission = permissionSplit[^1],
                    AccessCodes = accessCodes
                });
            }
            return vmacsExportPermissions;
        }

        private List<VMACSExportUser> GetExportUsers(List<UserList> users, string rolePrefix)
        {
            List<VMACSExportUser> exportUsers = new();
            string instance = rolePrefix.Split(".")[1];

            //load all permissions for all users and store in a dictionary by trimmed mothra id (trimming because fake users can have spaces in their mothra ids)
            var userPermissions = _RAPSContext.GetVMACSUserPermissionsResult.FromSql($"dbo.usp_getVMACSUserPermissions @Instance = {instance}")
                .ToList();
            Dictionary<string, string> permissionsByMemberId = new();
            foreach(var userPermissionList in userPermissions)
            {
                permissionsByMemberId[userPermissionList.MemberId.Trim()] = userPermissionList.PermissionIds ?? "";
            }

            string lastUser = "";
            foreach(var user in users)
            {
                //loop over multiple records for each user, each row containing a different role
                if(lastUser != user.LoginId)
                {
                    string permissionIdList = permissionsByMemberId.ContainsKey(user.MothraId.Trim())
                        ? permissionsByMemberId[user.MothraId.Trim()]
                        : "";
                    exportUsers.Add(new VMACSExportUser()
                    {
                        CasLogin = user.LoginId,
                        Email = user.MailId + "@ucdavis.edu",
                        AccessCodes = "",
                        //get permissions for this user from the dictionary created above
                        PermissionIds = permissionIdList.Length > 0
                            ? permissionIdList.Split(",").Select(int.Parse).ToList()
                            : new List<int>()
                    });
                    lastUser = user.LoginId;
                }

                exportUsers.Last().AccessCodes += user.AccessCode;
            }

            //alpha sort the access codes
            foreach(var user in exportUsers)
            {
                var accessCodeArray = user.AccessCodes.ToArray();
                Array.Sort(accessCodeArray, StringComparer.Ordinal);
                user.AccessCodes = new string(accessCodeArray);
            }

            return exportUsers;
        }

        private string GetDefaultServer()
        {
            return _onProduction ? "prod" : "qa";
        }

        private string GetServerUrl(string instance, string server)
        {
            string key = instance.ToLower() + "-" + server.ToLower();
            return _vmacsServers.ContainsKey(key) ? _vmacsServers[key] : "";
        }

        private static void RecordMessage(List<string> messages, string message)
        {
            messages.Add(message);
            HttpHelper.Logger.Log(NLog.LogLevel.Debug, message);
        }

        private class UserList
        {
            public required string LoginId { get; set; }
            public required string MothraId { get; set; }
            public required string MailId { get; set;}
            public string AccessCode { get; set; } = string.Empty;
            public int RoleId { get; set; }
        }

        private class VmacsResponse
        {
            public bool Success { get; set; } = false;
            public string ErrorMessage { get; set; } = string.Empty;
            public int NumErrors { get; set; }
            public int NumSkippedUsers { get; set; }
            public int NumTotalUsers { get; set; }
            public int NumUsersWithAuthChanged { get; set; }
            public int NumUsersWithOptionChanged { get; set; }
            public int NumUsersWithPermChanged { get; set; }
        }
    }
}
