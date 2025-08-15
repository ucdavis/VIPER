using Newtonsoft.Json;
using NuGet.Protocol;
using System.Security.Cryptography;
using System.Text;
using Viper.Areas.RAPS.Models.Uinform;

namespace Viper.Areas.RAPS.Services
{
    public class UinformService
    {
        private static readonly HttpClient _httpClient = new();
        //private readonly NLog.Logger _logger;
        private readonly string _apiBase;
        private static readonly List<HttpMethod> _allowedHttpMethods = new()
        {
            HttpMethod.Get,
            HttpMethod.Post,
            HttpMethod.Put
        };
        //this is added to an extension attrbiute to mark a group as being "ours"
        private static readonly string _ourGroupIdentifier = "SVMManagedGroup";

        public UinformService()
        {
            //_logger = HttpHelper.Logger;
            _apiBase = (HttpHelper.Environment?.IsProduction() ?? false)
                ? "https://ws.uinform.ucdavis.edu/"
                : "https://ws.uinform-test.ucdavis.edu/";
        }

        /// <summary>
        /// Get a list of managed groups. Can return all groups we control, or search by string or owner.
        /// </summary>
        /// <param name="search">Search the extension attribute for a string</param>
        /// <param name="ownerSamAccountName">Get all groups owned by a SamAccountName</param>
        /// <param name="ownerGuid">Get all groups owned by a Guid</param>
        /// <returns>List of Managed groups</returns>
        /// <exception cref="Exception"></exception>
        public async Task<List<ManagedGroup>> GetManagedGroups(string? search = null, string? ownerSamAccountName = null, string? ownerGuid = null)
        {
            string url = "ManagedGroups/";
            HttpMethod method = HttpMethod.Get;
            string body = "";

            if (!string.IsNullOrEmpty(search))
            {
                url += "Search";
                method = HttpMethod.Post;
                body = new SearchClass() { Value = search }.ToJson();
            }
            else if (!string.IsNullOrEmpty(ownerSamAccountName))
            {
                url += "sam/" + ownerSamAccountName;
            }
            else if (!string.IsNullOrEmpty(ownerGuid))
            {
                url += "owner/" + ownerGuid;
            }
            else
            {
                url += "Search";
                method = HttpMethod.Post;
                body = new SearchClass() { Value = _ourGroupIdentifier }.ToJson();
            }

            var response = await SendRequest<List<ManagedGroup>>(url, method, body);
            return response?.ResponseObject ?? new List<ManagedGroup>();
        }

        /// <summary>
        /// Get a single managed group by distinguished name or guid
        /// </summary>
        /// <param name="distinguishedName"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public async Task<ManagedGroup> GetManagedGroup(string? distinguishedName = null, string? guid = null)
        {
            string url = "ManagedGroups/";
            if (!string.IsNullOrEmpty(distinguishedName))
            {
                url += "dn/" + distinguishedName;
            }
            else if (!string.IsNullOrEmpty(guid))
            {
                url += "guid/" + guid;
            }

            var response = await SendRequest<ManagedGroup>(url, HttpMethod.Get);
            return response?.ResponseObject ?? new ManagedGroup();
        }

        /// <summary>
        /// Create a managed group. Does not return anything because the group creation is scheduled and is done by uInform asynchronously
        /// </summary>
        /// <param name="groupName">The name of the group. Must start with SVM- and be unique within AD3.</param>
        /// <param name="displayName"></param>
        /// <param name="description"></param>
        /// <param name="maxMembers">max members, or 0 for no max</param>
        /// <returns></returns>
        public async Task CreateManagedGroup(string groupName, string displayName, string description, int maxMembers = 0)
        {
            if (!groupName.StartsWith("SVM-"))
            {
                groupName = "SVM-" + groupName;
            }
            ManagedGroupAddEdit newGroup = new()
            {
                GroupName = groupName,
                DisplayName = displayName,
                Description = description,
                MaxMembers = maxMembers
            };
            _ = await SendRequest<ManagedGroup>("ManagedGroups", HttpMethod.Post, newGroup.ToJson());
        }

        /// <summary>
        /// Update the managed group with the given guid. Does not return anything because the group creation is scheduled and is done by uInform asynchronously
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="displayName"></param>
        /// <param name="description"></param>
        /// <param name="maxMembers">Max members, or 0 for no max</param>
        /// <returns></returns>
        public void UpdateManagedGroup(string guid, string displayName, string description, int maxMembers = 0)
        {
            ManagedGroupAddEdit newGroup = new()
            {
                DisplayName = displayName,
                Description = description,
                MaxMembers = maxMembers
            };
            _ = SendRequest<ManagedGroup>("ManagedGroups/" + guid, HttpMethod.Put, newGroup.ToJson());
        }

        /// <summary>
        /// Get members of the group with the given guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public async Task<List<AdUser>> GetGroupMembers(string guid)
        {
            var response = await SendRequest<List<AdUser>>($"ManagedGroups/{guid}/members", HttpMethod.Get);
            return response?.ResponseObject ?? new List<AdUser>();
        }

        /// <summary>
        /// Add a user to a group
        /// </summary>
        /// <param name="groupGuid"></param>
        /// <param name="userGuid"></param>
        /// <returns></returns>
        public async Task AddGroupMember(string groupGuid, string userGuid)
        {
            await SendRequest<List<AdUser>>($"ManagedGroups/{groupGuid}/members", HttpMethod.Post, new AddRemoveMember()
            {
                UserGuid = userGuid,
                Action = "add"
            }.ToJson());
        }

        /// <summary>
        /// Remove a user from a group
        /// </summary>
        /// <param name="groupGuid"></param>
        /// <param name="userGuid"></param>
        /// <returns></returns>
        public async Task RemoveGroupMember(string groupGuid, string userGuid)
        {
            await SendRequest<List<AdUser>>($"ManagedGroups/{groupGuid}/members", HttpMethod.Post, new AddRemoveMember()
            {
                UserGuid = userGuid,
                Action = "remove"
            }.ToJson());
        }

        /// <summary>
        /// Get user by one of the identifiers
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="userPrincipalName"></param>
        /// <param name="mail"></param>
        /// <param name="samAccountName"></param>
        /// <returns></returns>
        public async Task<AdUser> GetUser(string? guid = null, string? userPrincipalName = null, string? mail = null, string? samAccountName = null)
        {
            string url = "AdUsers/";
            if (!string.IsNullOrEmpty(guid))
            {
                url += "guid/" + guid;
            }
            else if (!string.IsNullOrEmpty(userPrincipalName))
            {
                url += "upn/" + userPrincipalName;
            }
            else if (!string.IsNullOrEmpty(mail))
            {
                url += "mail/" + mail;
            }
            else if (!string.IsNullOrEmpty(samAccountName))
            {
                url += "sam/" + samAccountName;
            }
            var response = await SendRequest<AdUser>(url, HttpMethod.Get);
            return response?.ResponseObject ?? new AdUser();
        }

        /// <summary>
        /// Helper function to send a request. Create auth header and parses the response into a UinformResponse<T>
        /// </summary>
        /// <typeparam name="T">One of the uInform response object types</typeparam>
        /// <param name="url">URL to send the request</param>
        /// <param name="method">HTTP method (Get, Post, Put)</param>
        /// <param name="body">If needed, the JSON body</param>
        /// <returns></returns>
        private async Task<UinformResponse<T>?> SendRequest<T>(string url, HttpMethod method, string? body = null)
        {
            if (!url.StartsWith(_apiBase))
            {
                url = _apiBase + url;
            }

            if (!_allowedHttpMethods.Contains(method))
            {
                throw new Exception("Invalid Method: " + method.Method);
            }

            int epochTime = (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
            string publicKey = HttpHelper.GetSetting<string>("Credentials", "uInformPublicKey") ?? "";
            string sig = GetAuthSignature(method, publicKey, epochTime);
            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(publicKey + ":" + sig));

            using HttpRequestMessage request = new()
            {
                RequestUri = new Uri(url),
                Method = method
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("X-UTIMESTAMP", epochTime.ToString());
            using StringContent content = new(body ?? "", Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(body))
            {
                request.Content = content;
            }

            UinformResponse<T>? uInformResponse;
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                uInformResponse = JsonConvert.DeserializeObject<UinformResponse<T>>(responseBody);
            }
            else
            {
                uInformResponse = new UinformResponse<T>()
                {
                    Success = false,
                    Error = new()
                    {
                        Message = response.StatusCode.ToString()
                    }
                };
            }

            return uInformResponse;
        }

        private static string GetAuthSignature(HttpMethod method, string publicKey, int epochTime)
        {
            //get public and private key
            string? privateKey = HttpHelper.GetSetting<string>("Credentials", "uInformPrivateKey");

            //take "{METHOD}:{epochTime}:{publicKey}" and sign it with the privateKey, then convert to base64
            if (!string.IsNullOrEmpty(publicKey) && !string.IsNullOrEmpty(privateKey))
            {
                string toSign = method.Method.ToUpper() + ":" + epochTime.ToString() + ":" + publicKey;
                // Legacy API requires HMACSHA1 - third-party system constraint
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
                using var sha1 = new HMACSHA1(Encoding.ASCII.GetBytes(privateKey));
#pragma warning restore CA5350
                byte[] hashed = sha1.ComputeHash(Encoding.ASCII.GetBytes(toSign));
                return Convert.ToBase64String(hashed);
            }

            return "";
        }

        //Private classes used to communicate with the API
        private class SearchClass
        {
            public string Property { get; set; } = "ExtensionAttribute6";
            public string Value { get; set; } = string.Empty;
            public string MatchType { get; set; } = "LIKE";
        }

        private class ManagedGroupAddEdit
        {
            public string GroupName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int MaxMembers { get; set; } = 0;
            public string ExtensionAttribute6 { get; set; } = _ourGroupIdentifier;
        }

        private class AddRemoveMember
        {
            public string UserGuid { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
        }
    }

}
