using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NLog;

namespace VIPER
{
    /// <summary>
	/// HTTP Helper class (static) that provides access to a number of system-wide objects and JSON errors
	/// </summary>
	public static class HttpHelper
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
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
            HttpHelper.Cache = memoryCache;
            HttpHelper.Settings = configurationSettings;
            HttpHelper.Environment = env;
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
    }
}
