using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NLog;

namespace Viper
{
    /// <summary>
	/// HTTP Helper class (static) that provides access to a number of system-wide objects and JSON errors
	/// </summary>
	public static class HttpHelper
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static IHttpContextAccessor? httpContextAccessor;
        private static IAuthorizationService? authorizationService;
        private static IDataProtectionProvider? dataProtectionProvider;
        private static HtmlSanitizer? htmlSanitizer;

        /// <summary>
        /// Helper functions constructor (gets injected with the memeory cache object)
        /// </summary>
        /// <param name="memoryCache"></param>
        public static void Configure(IMemoryCache? memoryCache, IConfiguration? configurationSettings, IWebHostEnvironment env, IHttpContextAccessor? httpContextAccessor, IAuthorizationService? authorizationService, IDataProtectionProvider? dataProtectionProvider)
        {
            Cache = memoryCache;
            Settings = configurationSettings;
            Environment = env;
            HttpHelper.httpContextAccessor = httpContextAccessor;
            HttpHelper.authorizationService = authorizationService;
            HttpHelper.dataProtectionProvider = dataProtectionProvider;          

            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedAttributes.Add("class");
            HttpHelper.htmlSanitizer = sanitizer;
        }

        /// <summary>
        /// Get any appsettings.json settings
        /// </summary>
        public static IConfiguration? Settings { get; private set; }

        /// <summary>
        /// The current enviroment (Development, Staging, Production)
        /// </summary>
        public static IWebHostEnvironment? Environment { get; private set; }

        /// <summary>
        /// Get the current HttpContext
        /// </summary>
        public static HttpContext? HttpContext
        {
            get
            {
                if (httpContextAccessor != null)
                {
                    return httpContextAccessor.HttpContext;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Get the current memory cache
        /// </summary>
        public static IMemoryCache? Cache { get; private set; }

        /// <summary>
        /// Get the current logger
        /// </summary>
        public static Logger Logger { get { return logger; } }

        /// <summary>
        /// Get the authorization service
        /// </summary>
        public static IAuthorizationService? AuthorizationService { get { return authorizationService; } }

        /// <summary>
        /// Get the data protection service
        /// </summary>
        public static IDataProtectionProvider? DataProtectionProvider { get { return dataProtectionProvider; } }

        /// <summary>
        /// Get our default HTML sanitizer
        /// </summary>
        public static HtmlSanitizer? HtmlSanitizer { get { return htmlSanitizer; } }

        /// <summary>
        /// Gets the root URL including protocol and port for Viper.Net
        /// </summary>
        public static string GetRootURL()
        {
            string rootURL = String.Empty;

            HttpRequest? thisRequest = httpContextAccessor?.HttpContext?.Request;

            if (thisRequest != null)
            {
                Uri url = new(thisRequest.GetDisplayUrl());
                rootURL = url.GetLeftPart(UriPartial.Authority);

                if (url.AbsolutePath.ToString().StartsWith("/2/") && rootURL != null)
                {
                    rootURL += "/2";
                }

            }
            
            return rootURL ?? String.Empty;
        }
        /// <summary>
        /// Gets the root URL for ColdFusion Viper based off the enviroment
        /// </summary>
        public static string GetOldViperRootURL()
        {
            string oldViperURL = "https://viper.vetmed.ucdavis.edu";

            if (Environment?.EnvironmentName == "Development")
            {
                oldViperURL = "http://localhost";
            }
            else if (Environment?.EnvironmentName == "Test")
            {
                oldViperURL = "https://secure-test.vetmed.ucdavis.edu";
            }

            return oldViperURL;
        }

        /// <summary>
        /// Respond with a 500 error in JSON fromat
        /// </summary>
        /// <param name="ex">The exception to respond with</param>
        /// <returns>A JsonResult with the supplied error message and status code</returns>
        public static JsonResult JsonError(Exception ex)
        {

#if DEBUG
            return JsonErrorWithStatus(ex.Message, details: ex.StackTrace);
#else
			return JsonErrorWithStatus(ex.Message);
#endif
        }

        /// <summary>
        /// Respond with a 500 error in JSON fromat
        /// </summary>
        /// <param name="message">The error message</param>
        /// <returns>A JsonResult with the supplied error message and status code</returns>
        public static JsonResult JsonError(string message)
        {
            return JsonErrorWithStatus(message);
        }

        /// <summary>
        /// Respond with a error in JSON fromat and the specified status code
        /// </summary>
        /// <param name="message">The error message to send</param>
        /// <param name="statusCode">The HTTP status code to use in the response (defaults to 500)</param>
        /// <returns>A JsonResult with the supplied error message and status code</returns>
        public static JsonResult JsonErrorWithStatus(string message, int statusCode = StatusCodes.Status500InternalServerError, string? details = null)
        {
            return new JsonResult(new { message, details })
            {
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Helper function to return a setting, including a null check for Settings
        /// </summary>
        /// <typeparam name="T">Type of setting to be returned, e.g. string</typeparam>
        /// <param name="section">Section for Settings.GetSection()</param>
        /// <param name="setting">Setting for section.GetValue<T>()</param>
        /// <returns></returns>
        public static T? GetSetting<T>(string section, string setting)
        {
            return Settings == null
                ? default(T)
                : Settings.GetSection(section).GetValue<T>(setting);
        }
    }
}
