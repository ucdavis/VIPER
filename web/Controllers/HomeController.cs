using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Xml.Linq;
using Viper.Models;
using Web.Authorization;
using Microsoft.Extensions.Options;
using Viper.Classes;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Viper.Models.AAUD;
using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Http.Extensions;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace Viper.Controllers
{    
    public class HomeController : Controller
    {
        private readonly Classes.SQLContext.AAUDContext _aAUDContext;
        private readonly XNamespace _ns = "http://www.yale.edu/tp/cas";
        private const string _strTicket = "ticket";
        private readonly IHttpClientFactory _clientFactory;
        private readonly CasSettings _settings;
        private readonly List<string> _casAttributesToCapture = new List<string>() { "authenticationDate", "credentialType" };

        public HomeController(IHttpClientFactory clientFactory, IOptions<CasSettings> settingsOptions, Classes.SQLContext.AAUDContext aAUDContext)
        {
            this._clientFactory = clientFactory;
            this._settings = settingsOptions.Value;
            this._aAUDContext = aAUDContext;
        }
        /// <summary>
        /// VIPER 2 home page
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [SearchName(FriendlyName = "Viper 2 Homepage")]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Login function -- redirects to CAS, no VIEW
        /// </summary>
        [Route("/[action]")]
        [AllowAnonymous]
        [SearchExclude]
        public IActionResult Login()
        {
            Uri url = new Uri(Request.GetDisplayUrl());
            string baseURl = url.GetLeftPart(UriPartial.Authority);
            string returnURL = HttpHelper.GetRootURL().Replace(baseURl, "");

            if (!string.IsNullOrEmpty(Request.Query["ReturnUrl"]))
            {
                returnURL = Request.Query["ReturnUrl"].ToString();
            }

            var authorizationEndpoint = _settings.CasBaseUrl + "login?service=" + WebUtility.UrlEncode(BuildRedirectUri(new PathString("/CasLogin")) + "?ReturnUrl=" + WebUtility.UrlEncode(returnURL));

            return new RedirectResult(authorizationEndpoint);
        }

        /// <summary>
        /// CAS Login function -- redirects to original page, no VIEW
        /// </summary>
        [Route("/[action]")]
        [AllowAnonymous]
        [SearchExclude]
        public async Task<IActionResult> CasLogin()
        {
            return await AuthenticateCasLogin();
        }

        //TODO - consider implementing IP restrictions on this method to only allow emulation from in school or on VPN locations
        /// <summary>
        /// Emulate a user
        /// </summary>
        /// <param name="loginId">The login id of the user we are emulating</param>
        [Route("/[action]/{loginId}")]
        [Authorize(Policy = "2faAuthentication")]
        [Permission(Allow = "SVMSecure.SU")]
        public IActionResult EmulateUser(string loginId)
        {
            AaudUser? emulatedUser = UserHelper.GetByLoginId(_aAUDContext, loginId);

            string? trueLoginId = UserHelper.GetCurrentUser()?.LoginId;

            if (emulatedUser != null && trueLoginId != null)
            {
                var protector = HttpHelper.DataProtectionProvider?.CreateProtector("Viper.Emulation", trueLoginId);

                if (protector != null && emulatedUser.LoginId != null)
                {
                    string? encryptedEmulatedLoginId = protector?.Protect(emulatedUser.LoginId);

                    // set emulating cached item to expire after 30 minutes of inactivity
                    HttpHelper.Cache?.Set(ClaimsTransformer.EmulationCacheNamePrefix + trueLoginId, encryptedEmulatedLoginId, (new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(30))));
                    return new RedirectResult("~/");
                }

            }

            return new RedirectResult("~/Error");

        }

        /// <summary>
        /// Clears the emulation cache for the user
        /// </summary>
        [Route("/[action]")]
        public IActionResult ClearEmulation()
        {
            AaudUser? user = UserHelper.GetTrueCurrentUser();
            string? trueLoginId = user?.LoginId;

            if (trueLoginId != null && HttpHelper.Cache != null)
            {
                HttpHelper.Cache.Remove(ClaimsTransformer.EmulationCacheNamePrefix + trueLoginId);
            }

            return new RedirectResult("~/");
        }

        /// <summary>
        /// Clears the cache
        /// </summary>
        [Route("/[action]")]
        [Authorize(Roles = "VMDO SVM-IT", Policy = "2faAuthentication")]
        public IActionResult ClearCache()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var entries = HttpHelper.Cache?.GetType().GetField("_entries", flags)?.GetValue(HttpHelper.Cache);
            var cacheEntries = entries as IDictionary;

            if (cacheEntries != null)
            {
                foreach(string key in cacheEntries.Keys)
                {
                    HttpHelper.Cache?.Remove(key);
                }

            }

            return new RedirectResult("~/");
        }

        /// <summary>
        /// Error page (not used in Development)
        /// </summary>
        [Route("/[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [SearchExclude]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// StatusCode or 403 page. Example path: /Error/404
        /// </summary>
        /// <param name="statusCode"></param>
        [Route("/[action]/{statusCode}")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [SearchExclude]
        public IActionResult Error(int? statusCode = null)
        {
            ViewBag.errorMessage = HttpContext.Items["ErrorMessage"];

            if (statusCode.HasValue)
            {
                var viewName = statusCode.ToString();
                switch (statusCode)
                {
                    case 403:
                        Response.StatusCode = 403;
                        viewName = statusCode.ToString();
                        break;
                    default:
                        viewName = "StatusCode";
                        break;
                }

                return View(viewName, (HttpStatusCode)statusCode.Value);
            }
            return View();
        }

        /// <summary>
        /// Logout function -- redirects to CAS logout, no VIEW
        /// </summary>
        /// <returns></returns>
        [Route("/[action]")]
        [SearchExclude]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return new RedirectResult(_settings.CasBaseUrl + "logout");
        }

        /// <summary>
        /// Testing the connection to AWS Secrets Manager
        /// </summary>
        /// <returns></returns>
        [Route("/[action]")]
        [SearchExclude]
        public IActionResult AWSTest()
        {
            try
            {
                var request = new GetParameterRequest()
                {
                    Name = "/" + HttpHelper.Environment?.EnvironmentName + "/ConnectionStrings/TestSecret",
                    WithDecryption = true
                };

                using (var client = new AmazonSimpleSystemsManagementClient())
                {
                    var response = client.GetParameterAsync(request);
                    ViewData["TestResult"] = ($"{request.Name} value is: {response.Result.Parameter.Value}");
                }
            }
            catch (Exception ex)
            {
                ViewData["TestResult"] = ex.Message;
            }

            try
            {
                if (HttpHelper.Settings != null)
                {
                    var TestConnectionString = HttpHelper.Settings["ConnectionStrings:TestSecret"];
                    ViewData["ConfigResult"] = TestConnectionString?.Trim();
                }
            }
            catch (Exception ex)
            {
                ViewData["ConfigResult"] = ex.Message;
            }

            return View();
        }

        /// <summary>
        /// Utility function for creating redirect URLs
        /// </summary>
        /// <param name="targetPath"></param>
        /// <returns>Compiled URL</returns>
        private string BuildRedirectUri(string targetPath)
        {          
            return HttpHelper.GetRootURL() + targetPath;
        }

        /// <summary>
        /// Processes the CAS login and sets the user
        /// </summary>
        private async Task<IActionResult> AuthenticateCasLogin()
        {
            // get ticket & service
            string? ticket = Request.Query[_strTicket];
            string? returnUrl = Request.Query["ReturnUrl"];
            string service = WebUtility.UrlEncode(BuildRedirectUri(Request.Path) + "?ReturnUrl=" + WebUtility.UrlEncode(returnUrl));

            var client = _clientFactory.CreateClient("CAS");

            try
            {
                var response = await client.GetAsync(_settings.CasBaseUrl + "p3/serviceValidate?ticket=" + ticket + "&service=" + service, HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var doc = XDocument.Parse(responseBody);

                var serviceResponse = doc.Element(_ns + "serviceResponse");
                var successNode = serviceResponse?.Element(_ns + "authenticationSuccess");
                var userNode = successNode?.Element(_ns + "user");
                var validatedUserName = userNode?.Value;

                // uncomment this line temporarily if you ever have issues with users getting unexpected 403(Access Denied) errors in the logs
                // uncommenting this line will log what CAS is sending. When the user in question logs in while trying to access our site
                //HttpHelper.Logger.Log(NLog.LogLevel.Information, "CAS response: " + doc.ToString());

                if (!string.IsNullOrEmpty(validatedUserName))
                {
                    var claimsIdentity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, validatedUserName), new Claim(ClaimTypes.NameIdentifier, validatedUserName), new Claim(ClaimTypes.AuthenticationMethod, "CAS") }, CookieAuthenticationDefaults.AuthenticationScheme);

                    XElement? attributesNode = successNode?.Element(_ns + "attributes");
                    if (attributesNode != null)
                    {
                        foreach (string attributeName in _casAttributesToCapture)
                        {
                            foreach (var element in attributesNode.Elements(_ns + attributeName))
                            {
                                claimsIdentity.AddClaim(new Claim(element.Name.LocalName, element.Value));
                            }
                        }
                    }

                    var user = new ClaimsPrincipal(claimsIdentity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user);

                    return new LocalRedirectResult(!String.IsNullOrWhiteSpace(returnUrl) ? returnUrl : "/");
                }
            }
            catch (TaskCanceledException ex) {// usually caused because the user aborts the page load (HttpContext.RequestAborted)
				HttpHelper.Logger.Log(NLog.LogLevel.Info, "TaskCanceledException: " + ex.Message.ToString());
			} 

            return new ForbidResult();
        }
    }
}