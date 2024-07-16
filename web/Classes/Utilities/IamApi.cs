using Azure;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.Json;
using System.Web;
using Viper.Models.IAM;

namespace Viper.Classes.Utilities
{
    public class IamApi
    {
        private const string apiBase = "https://iet-ws.ucdavis.edu/api/";
        private const int defaultPerPage = 100;
        private static readonly List<string> reservedParamKeys = new List<string>() { "key", "v" };
        private const string version = "1.0";
        private readonly IHttpClientFactory _factory;

        /*
        private static HttpClient _httpClient = new()
        {
            BaseAddress = new Uri(apiBase),
        };
        */

        public IamApi(IHttpClientFactory factory)
        {
            //_httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            //_httpClient.Timeout = TimeSpan.FromSeconds(60);
            _factory = factory;
        }

        public async Task<Models.IAM.Response<Ids>> GetIamIds(string iamId)
        {
            return await Send<Ids>("iam/people/ids/search", new NameValueCollection() { { "iamId", iamId } });
        }

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
        private async Task<Models.IAM.Response<T>> Send<T>(string url, NameValueCollection requestParams)
        {
            var paramArray = (
                from key in requestParams.AllKeys.Where(k => k != null && !reservedParamKeys.Contains(k))
                from value in requestParams.GetValues(key) ?? Array.Empty<string>()
                select string.Format(
                    "{0}={1}",
                    HttpUtility.UrlEncode(key),
                    HttpUtility.UrlEncode(value))
                ).ToList();
            paramArray.Add(string.Format("{0}={1}", "key", GetApiKey()));
            paramArray.Add(string.Format("{0}={1}", "v", version));
            var builder = new UriBuilder(apiBase + url)
            {
                Query = "?" + string.Join("&", paramArray)
            };

            var client = GetHttpClient();
            var response = await client.GetAsync(builder.ToString());
            return await ParseResponse<T>(response);
        }

        /// <summary>
        /// Parse the JSON response into the simplified Response<T> object
        /// </summary>
        /// <typeparam name="T">The type of object from IAM</typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        private async Task<Models.IAM.Response<T>> ParseResponse<T>(HttpResponseMessage? response)
        {
            IEnumerable<T>? data = null;
            string? errorMessage = null;

            try
            {
                if (response == null)
                {
                    errorMessage = "No response.";
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var content = Newtonsoft.Json.JsonConvert.DeserializeObject<IntermediateResponse<T>>(responseContent);
                    if (content == null)
                    {
                        errorMessage = "Could not parse response.";
                    }
                    else
                    {
                        switch (content.ResponseStatus)
                        {
                            case 0: break;//success
                            case 1: errorMessage = "Response Code 1: No Results."; break;
                            case 2: errorMessage = "Response Code 2: Invalid Key."; break;
                            case 3: errorMessage = "Response Code 3: IAM Errors."; break;
                            case 4: errorMessage = "Response Code 4: Data Error."; break;
                            case 5: errorMessage = "Response Code 5: Missing Search Parameters."; break;
                            default: errorMessage = "Response Code 1: Unknown Response Code: " + content.ResponseStatus + "."; break;
                        }
                        data = content.ResponseData?.Results;

                        if (!string.IsNullOrEmpty(content.ResponseDetails))
                        {
                            errorMessage += (" " + content.ResponseDetails).Trim();
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                errorMessage = "Invalid response: " + ex.Message + ".";
            }

            return new Models.IAM.Response<T>()
            {
                Data = data,
                ErrorMessage = errorMessage
            };
        }

        private string GetApiKey()
        {
            return HttpHelper.GetSetting<string>("Credentials", "IamApiToken") ?? "";
        }

        private void logRequest()
        {

        }

        /// <summary>
        /// Response from IAM comes in this format. The ParseResponse method will take this format and simplify it.
        /// https://ucdavis.jira.com/wiki/spaces/IAM/pages/688849434/IAM+Web+Services+IAM-WS#IAMWebServices(IAM-WS)-BasicResponseFormat
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class IntermediateResponse<T>
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
        private class DataArray<T>
        {
            public IEnumerable<T> Results { get; set; } = new List<T>();
        }
    }
}
