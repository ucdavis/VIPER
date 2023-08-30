using System.Collections.Generic;
using System.Net.Mail;
using System.Reflection;
using System.Net.Http;
using System.Linq;
using System.Text.Json;
using System.Text;
using System.Net;
using Microsoft.Extensions.Caching.Memory;

namespace Viper.Classes.Utilities
{
    /// <summary>
    /// A class to handle sending http requests to an endpoint that is protected by an F5 access policy. The
    /// process for making a request requires creating a session and sending cookies in the headers in order for
    /// the F5 to allow the request.
    /// 
    /// The .NET HttpClient will automatically store cookies sent by the F5. This makes this class much simpler than the CF equivalent.
    /// However, the F5 will send a TCP RST if you try to post without a valid session, so we still need this wrapper.
    /// </summary>
    public class F5HttpRequest
    {
        private static readonly HttpClient _httpClient = new();

        public async Task<HttpResponseMessage> Send(HttpRequestMessage request, int attemptNumber = 0)
        {  
            HttpResponseMessage? response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception)
            {
                response = await HandleConnectionFail(request, attemptNumber);
            }

            //Did I get nothing? Return a generic error.
            if(response == null)
            {
                return GetErrorResponse();
            }
            
            return response;
        }

        private async Task<HttpResponseMessage?> HandleConnectionFail(HttpRequestMessage request, int attemptNumber)
        {
            //if we try a post and get a connection close error, we need to start a session with the F5
            if (request.Method != HttpMethod.Get && attemptNumber == 0)
            {
                //Try a send to the original url, with a get and empty body.
                HttpRequestMessage newRequest = new()
                {
                    RequestUri = request.RequestUri,
                    Method = HttpMethod.Get
                };
                
                //don't care about the response
                await Send(newRequest);
                return await Send(await CopyHttpRequest(request), 1);
            }

            return null;
        }

        private static async Task<HttpRequestMessage> CopyHttpRequest(HttpRequestMessage request)
        {
            HttpRequestMessage newRequest = new(request.Method, request.RequestUri)
            {
                Version = request.Version,
            };

            //Copy content
            if (request.Content != null)
            {
                var ms = new MemoryStream();
                await request.Content.CopyToAsync(ms);
                ms.Position = 0;
                newRequest.Content = new StreamContent(ms);

                // Copy the content headers
                foreach (var h in request.Content.Headers)
                    newRequest.Content.Headers.Add(h.Key, h.Value);
            }

            foreach (KeyValuePair<string, object?> option in request.Options)
                newRequest.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
                newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return newRequest;
        }

        private static HttpResponseMessage GetErrorResponse()
        {
            HttpResponseMessage response = new()
            {
                StatusCode = System.Net.HttpStatusCode.InternalServerError
            };
            var error = new { errorMessage = "An error occurred." };
            response.Content = new StringContent(JsonSerializer.Serialize(error));
            return response;
        }
    }
}
