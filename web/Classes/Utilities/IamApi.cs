using NLog;
using NuGet.Protocol.Plugins;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.Pkcs;
using System.Text.Json;
using System.Web;
using Viper.Models.IAM;

namespace Viper.Classes.Utilities
{
    /// <summary>
    /// Class to retrieve data from IAM. Public methods should call Send or SendMultiple and return the Response. Send and SendMultiple
    /// encapsulate the logic for sending a request to IAM and parsing the response data.
    /// </summary>
    public class IamApi
    {
        private const string apiBase = "https://iet-ws.ucdavis.edu/api/";
        private static readonly List<string> reservedParamKeys = new() { "key", "v" };
        private const string version = "1.0";
        private readonly IHttpClientFactory _factory;
        private readonly Logger _logger = LogManager.GetLogger("IAM");

        public IamApi(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        /*
         * IAM Ids
         */
        /// <summary>
        /// Get Ids from IAM
        /// </summary>
        /// <param name="iamId"></param>
        /// <returns></returns>
        public async Task<Response<Ids>> GetIamIds(string iamId)
        {
            return await Send<Ids>("iam/people/ids/search", new NameValueCollection() { { "iamId", iamId } });
        }

        /// <summary>
        /// Get kerberos for a single user
        /// </summary>
        /// <param name="iamId"></param>
        /// <returns></returns>
        public async Task<Response<Kerberos>> GetKerberos(string iamId)
        {
            return await Send<Kerberos>("iam/people/prikerbacct/" + iamId);
        }

        /// <summary>
        /// Get kerberos info for one or more iam ids
        /// </summary>
        /// <param name="iamIds"></param>
        /// <returns></returns>
        public async Task<Response<Kerberos>> GetKerberos(List<string> iamIds)
        {
            var callList = new List<Tuple<string, NameValueCollection?>>();
            foreach (var iamId in iamIds)
            {
                callList.Add(new Tuple<string, NameValueCollection?>("iam/people/prikerbacct/" + iamId, null));
            }
            return await SendMultiple<Kerberos>(callList, true);
        }

        /*
         * Get People / contact info
         */
        /// <summary>
        /// Get people from a list of iam ids
        /// </summary>
        /// <param name="iamIds"></param>
        /// <returns></returns>
        public async Task<Response<CorePerson>> GetByIamIds(List<string> iamIds)
        {
            var callList = new List<Tuple<string, NameValueCollection?>>();
            foreach (var iamId in iamIds)
            {
                callList.Add(new Tuple<string, NameValueCollection?>("iam/people/" + iamId, null));
            }
            return await SendMultiple<CorePerson>(callList, true);
        }

        /// <summary>
        /// Search for a person or people by name or ids
        /// </summary>
        /// <param name="iamId"></param>
        /// <param name="mothraId"></param>
        /// <param name="employeeId"></param>
        /// <param name="studentId"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <returns></returns>
        public async Task<Response<CorePerson>> SearchForPerson(string? iamId = null, string? mothraId = null, string? employeeId = null, string? studentId = null,
            string? firstName = null, string? lastName = null)
        {
            var requestParams = new NameValueCollection();
            if (iamId != null)
            {
                requestParams.Add("iamId", iamId);
            }
            if (mothraId != null)
            {
                requestParams.Add("mothraId", mothraId);
            }
            if (employeeId != null)
            {
                requestParams.Add("employeeId", employeeId);
            }
            if (studentId != null)
            {
                requestParams.Add("studentId", studentId);
            }
            if (firstName != null)
            {
                requestParams.Add("dFirstName", firstName);
            }
            if (lastName != null)
            {
                requestParams.Add("dLastName", lastName);
            }
            return await Send<CorePerson>("iam/people/search", requestParams);
        }

        /// <summary>
        /// Given a name, search for people with either first or last matching the name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Response<CorePerson>> SearchForPersonByName(string name)
        {
            var callList = new List<Tuple<string, NameValueCollection?>>
            {
                new("iam/people/search", new NameValueCollection() { { "dFirstName", name } }),
                new("iam/people/search", new NameValueCollection() { { "dLastName", name } }),
                new("iam/people/search", new NameValueCollection() { { "oFirstName", name } }),
                new("iam/people/search", new NameValueCollection() { { "oLastName", name } })
            };
            return await SendMultiple<CorePerson>(callList, true);
        }

        /// <summary>
        /// Get contact info by email address
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<Response<ContactInfo>> GetContactInfoByEmail(string email)
        {
            if (!email.Contains("@"))
            {
                email += "@ucdavis.edu";
            }
            return await Send<ContactInfo>("iam/people/contactinfo/search", new NameValueCollection() { { "email", email } });
        }

        // <summary>
        /// Get contact info by iam id
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<Response<ContactInfo>> GetContactInfo(string iamId)
        {
            return await Send<ContactInfo>("iam/people/contactinfo/" + iamId, null);
        }

        // <summary>
        /// Get contact info by iam id list
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<Response<ContactInfo>> GetContactInfo(List<string> iamIds)
        {
            var callList = new List<Tuple<string, NameValueCollection?>>();
            foreach (var iamId in iamIds)
            {
                callList.Add(new Tuple<string, NameValueCollection?>("iam/people/contactinfo/" + iamId, null));
            }
            return await SendMultiple<ContactInfo>(callList, false);
        }

        /*
         * Associations - Employee, SIS, Directory
         */
        /// <summary>
        /// Get all employee associations for an individual
        /// </summary>
        /// <param name="iamId"></param>
        /// <returns></returns>
        public async Task<Response<EmployeeAssociation>> GetEmployeeAssociations(string iamId)
        {
            return await Send<EmployeeAssociation>("iam/associations/pps/" + iamId, null);
        }

        /// <summary>
        /// Get all employee associations for a department
        /// </summary>
        /// <param name="iamId"></param>
        /// <returns></returns>
        public async Task<Response<EmployeeAssociation>> GetEmployeeAssociationsByDept(string deptCode)
        {
            return await Send<EmployeeAssociation>("iam/associations/pps/search", new NameValueCollection() { { "apptDeptCode", deptCode } });
        }

        /// <summary>
        /// Get employee associations for multiple dept codes
        /// </summary>
        /// <param name="deptCode"></param>
        /// <returns></returns>
        public async Task<Response<EmployeeAssociation>> GetEmployeeAssociationsByDept(List<string> deptCodes)
        {
            var callList = new List<Tuple<string, NameValueCollection?>>();
            foreach (var deptCode in deptCodes)
            {
                callList.Add(new Tuple<string, NameValueCollection?>("iam/associations/pps/search", new NameValueCollection() { { "apptDeptCode", deptCode } }));
            }
            return await SendMultiple<EmployeeAssociation>(callList, false);
        }

        /// <summary>
        /// Get SIS associations by iamid
        /// </summary>
        /// <param name="iamId"></param>
        /// <returns></returns>
        public async Task<Response<SisAssociation>> GetSisAssociations(string iamId)
        {
            return await Send<SisAssociation>("iam/associations/sis/" + iamId, null);
        }

        /// <summary>
        /// Search SIS associations by level, class, college, major
        /// </summary>
        /// <param name="levelCode"></param>
        /// <param name="levelName"></param>
        /// <param name="classCode"></param>
        /// <param name="className"></param>
        /// <param name="collegeCode"></param>
        /// <param name="collegeName"></param>
        /// <param name="assocRank"></param>
        /// <param name="majorCode"></param>
        /// <param name="majorName"></param>
        /// <returns></returns>
        public async Task<Response<SisAssociation>> SearchSisAssociations(string? levelCode = null, string? levelName = null, string? classCode = null,
            string? className = null, string? collegeCode = null, string? collegeName = null,
            string? assocRank = null, string? majorCode = null, string? majorName = null)
        {
            var requestParams = new NameValueCollection();
            if (!string.IsNullOrEmpty(levelCode))
            {
                requestParams.Add("levelCode", levelCode);
            }
            if (!string.IsNullOrEmpty(levelName))
            {
                requestParams.Add("levelName", levelName);
            }
            if (!string.IsNullOrEmpty(classCode))
            {
                requestParams.Add("classCode", classCode);
            }
            if (!string.IsNullOrEmpty(className))
            {
                requestParams.Add("className", className);
            }
            if (!string.IsNullOrEmpty(collegeCode))
            {
                requestParams.Add("collegeCode", collegeCode);
            }
            if (!string.IsNullOrEmpty(collegeName))
            {
                requestParams.Add("collegeName", collegeName);
            }
            if (!string.IsNullOrEmpty(assocRank))
            {
                requestParams.Add("assocRank", assocRank);
            }
            if (!string.IsNullOrEmpty(majorCode))
            {
                requestParams.Add("majorCode", majorCode);
            }
            if (!string.IsNullOrEmpty(majorName))
            {
                requestParams.Add("majorName", majorName);
            }
            return await Send<SisAssociation>("iam/associations/sis/search", requestParams);
        }

        /// <summary>
        /// Get directory associations for a person
        /// </summary>
        /// <param name="iamId"></param>
        /// <returns></returns>
        public async Task<Response<DirectoryAssociation>> GetDirectoryAssociations(string iamId)
        {
            return await Send<DirectoryAssociation>("iam/associations/odr/" + iamId, null);
        }

        /*
         * Private functions that encapsulate the logic of sending data to IAM, processing the response,
         * logging requests, etc.
         */
        /// <summary>
        /// Create and return the HttpClient using the client factory
        /// </summary>
        /// <returns></returns>
        private HttpClient GetHttpClient()
        {
            var client = _factory.CreateClient();
            client.BaseAddress = new Uri(apiBase);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(60);
            return client;
        }

        /// <summary>
        /// Send a request and return the response.
        /// </summary>
        /// <typeparam name="T">The type of object from IAM</typeparam>
        /// <param name="url"></param>
        /// <param name="requestParams"></param>
        /// <returns></returns>
        private async Task<Response<T>> Send<T>(string url, NameValueCollection? requestParams = null) where T : IIamData
        {
            if (!url.StartsWith(apiBase))
            {
                url = apiBase + url;
            }
            LogRequest(url, requestParams);
            var paramArray = new List<string>();
            if (requestParams != null)
            {
                var requestParamList = (
                from key in requestParams.AllKeys.Where(k => k != null && !reservedParamKeys.Contains(k))
                from value in requestParams.GetValues(key) ?? Array.Empty<string>()
                select string.Format(
                    "{0}={1}",
                    HttpUtility.UrlEncode(key),
                    HttpUtility.UrlEncode(value))
                );
                paramArray = paramArray.Concat(requestParamList).ToList();
            }

            //add version and api key, and then add params to the url
            paramArray.Add(string.Format("{0}={1}", "key", GetApiKey()));
            paramArray.Add(string.Format("{0}={1}", "v", version));
            var builder = new UriBuilder(url)
            {
                Query = "?" + string.Join("&", paramArray)
            };
            url = builder.ToString();

            var client = GetHttpClient();
            var response = await client.GetAsync(url);
            var parsed = await ParseResponse<T>(response);
            return parsed;
        }

        /// <summary>
        /// Send multiple requests and return a single response. Used for paginated calls and getting single records for multiple users.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sendArguments"></param>
        /// <returns></returns>
        private async Task<Response<T>> SendMultiple<T>(List<Tuple<string, NameValueCollection?>> sendArguments, bool filterToUnique = false) where T : IIamData
        {
            List<Task<Response<T>>> resultList = new();
            foreach (var arg in sendArguments)
            {
                resultList.Add(Send<T>(arg.Item1, arg.Item2));
            }

            var taskResults = await Task.WhenAll(resultList);
            var r = new Response<T>();
            foreach (var t in taskResults)
            {
                if (!string.IsNullOrEmpty(t.ErrorMessage))
                {
                    r.ErrorMessage += (!string.IsNullOrEmpty(r.ErrorMessage) ? ", " : "") + t.ErrorMessage;
                }

                r.Data ??= new List<T>();

                if (t.Data != null && filterToUnique)
                {
                    t.Data = t.Data.Where(thisResultData => !r.Data.Any(existing => existing.FilterableId == thisResultData.FilterableId)).ToList();
                }
                if (t.Data != null)
                {
                    r.Data = r.Data.Concat(t.Data).ToList();
                }
            }
            return r;
        }

        /// <summary>
        /// Parse the JSON response into the simplified Response<T> object
        /// </summary>
        /// <typeparam name="T">The type of object from IAM</typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        static private async Task<Response<T>> ParseResponse<T>(HttpResponseMessage? response) where T : IIamData
        {
            var r = new Response<T>()
            {
                Data = null,
                ErrorMessage = null
            };

            try
            {
                if (response == null)
                {
                    r.ErrorMessage = "No response.";
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var content = Newtonsoft.Json.JsonConvert.DeserializeObject<IntermediateResponse<T>>(responseContent);
                    if (content == null)
                    {
                        r.ErrorMessage = "Could not parse response.";
                    }
                    else
                    {
                        switch (content.ResponseStatus)
                        {
                            case 0: break;//success
                            case 1: r.ErrorMessage = "Response Code 1: No Results."; break;
                            case 2: r.ErrorMessage = "Response Code 2: Invalid Key."; break;
                            case 3: r.ErrorMessage = "Response Code 3: IAM Errors."; break;
                            case 4: r.ErrorMessage = "Response Code 4: Data Error."; break;
                            case 5: r.ErrorMessage = "Response Code 5: Missing Search Parameters."; break;
                            default: r.ErrorMessage = "Unknown Response Code: " + content.ResponseStatus + "."; break;
                        }
                        r.Data = content.ResponseData?.Results;

                        if (!string.IsNullOrEmpty(content.ResponseDetails))
                        {
                            r.ErrorMessage += (" " + content.ResponseDetails).Trim();
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                r.ErrorMessage = "Invalid response: " + ex.Message + ".";
            }

            return r;
        }

        static private string GetApiKey()
        {
            return HttpHelper.GetSetting<string>("Credentials", "IamApiToken") ?? "";
        }

        private void LogRequest(string url, NameValueCollection? requestParams)
        {
            string msg = string.Format("IAM Request to {0}", url);
            if (requestParams != null)
            {
                var requestParamList = (
                    from key in requestParams.AllKeys.Where(k => k != null && !reservedParamKeys.Contains(k))
                    from value in requestParams.GetValues(key) ?? Array.Empty<string>()
                    select string.Format(
                        "{0}={1}",
                        HttpUtility.UrlEncode(key),
                        HttpUtility.UrlEncode(value))
                    ).ToList();
                msg += string.Format(" with params {0}", string.Join("&", requestParamList));
            }
            _logger.Info(msg);
        }

        /// <summary>
        /// Response from IAM comes in this format. The ParseResponse method will take this format and simplify it.
        /// https://ucdavis.jira.com/wiki/spaces/IAM/pages/688849434/IAM+Web+Services+IAM-WS#IAMWebServices(IAM-WS)-BasicResponseFormat
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class IntermediateResponse<T> where T : IIamData
        {
            public string ResponseDetails { get; set; } = string.Empty;
            public int ResponseStatus { get; set; }
#pragma warning disable CS0649 // Assigned by JsonConvert.DeserializeObject
            public DataArray<T>? ResponseData;
#pragma warning restore CS0649 // Field 'IamApi.IntermediateResponse<T>.ResponseData' is never assigned to, and will always have its default value null
        }

        /// <summary>
        /// Data array is an object with a single key - results - that contains an array of data (even for things that return one record)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class DataArray<T> where T : IIamData
        {
            public IEnumerable<T> Results { get; set; } = new List<T>();
        }
    }
}
