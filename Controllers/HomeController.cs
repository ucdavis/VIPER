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
using VIPER;

namespace Viper.Controllers
{    
    public class HomeController : Controller
    {
        private readonly XNamespace _ns = "http://www.yale.edu/tp/cas";
        private const string StrTicket = "ticket";
        private readonly IHttpClientFactory _clientFactory;
        private readonly CasSettings settings;
        private readonly List<string> casAttributesToCapture = new List<string>() { "authenticationDate", "credentialType" };

        public HomeController(IHttpClientFactory clientFactory, IOptions<CasSettings> settingsOptions)
        {
            this._clientFactory = clientFactory;
            this.settings = settingsOptions.Value;
        }
        /// <summary>
        /// VIPER 2 home page
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Login function -- redirects to CAS, no VIEW
        /// </summary>
        [Route("/[action]")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            var authorizationEndpoint = settings.CasBaseUrl + "login?service=" + WebUtility.UrlEncode(BuildRedirectUri(Request, new PathString("/CasLogin")) + "?ReturnUrl=" + WebUtility.UrlEncode(Request.Query["ReturnUrl"]));

            return new RedirectResult(authorizationEndpoint);
        }

        /// <summary>
        /// CAS Login function -- redirects to original page, no VIEW
        /// </summary>
        [Route("/[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> CasLogin()
        {
            return await AuthenticateCasLogin();
        }

        /// <summary>
        /// Error page (not used in Development)
        /// </summary>
        [Route("/[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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
        public IActionResult Error(int? statusCode = null)
        {
            ViewBag.errorMessage = HttpContext.Items["ErrorMessage"];

            if (statusCode.HasValue)
            {
                var viewName = statusCode.ToString();
                switch (statusCode)
                {
                    case 403:
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
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return new RedirectResult(settings.CasBaseUrl + "logout");
        }

        /// <summary>
        /// Utility function for creating redirect URLs
        /// </summary>
        /// <param name="request"></param>
        /// <param name="targetPath"></param>
        /// <returns>Compiled URL</returns>
        private string BuildRedirectUri(HttpRequest request, string targetPath)
        {
            return request.Scheme + "://" + request.Host + request.PathBase + targetPath;
        }

        /// <summary>
        /// Processes the CAS login and sets the user
        /// </summary>
        private async Task<IActionResult> AuthenticateCasLogin()
        {
            // get ticket & service
            string? ticket = Request.Query[StrTicket];
            string? returnUrl = Request.Query["ReturnUrl"];

            string service = WebUtility.UrlEncode(BuildRedirectUri(Request, Request.Path) + "?ReturnUrl=" + WebUtility.UrlEncode(returnUrl));

            var client = _clientFactory.CreateClient("CAS");

            try
            {
                var response = await client.GetAsync(settings.CasBaseUrl + "p3/serviceValidate?ticket=" + ticket + "&service=" + service, HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var doc = XDocument.Parse(responseBody);

                var serviceResponse = doc.Element(_ns + "serviceResponse");
                var successNode = serviceResponse?.Element(_ns + "authenticationSuccess");
                var userNode = successNode?.Element(_ns + "user");
                var validatedUserName = userNode?.Value;

                // uncomment this line temporarily if you ever have issues with users getting unexpected 403(Access Denied) errors in the logs
                // uncommenting this line will log what CAS is sending. When the user in question logs in while trying to access our site
                //HttpHelper.Logger.Log(NLog.LogLevel.Warn, "CAS response: " + doc.ToString());

                if (!string.IsNullOrEmpty(validatedUserName))
                {
                    var claimsIdentity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, validatedUserName), new Claim(ClaimTypes.NameIdentifier, validatedUserName), new Claim(ClaimTypes.AuthenticationMethod, "CAS") }, CookieAuthenticationDefaults.AuthenticationScheme);

                    XElement? attributesNode = successNode?.Element(_ns + "attributes");
                    if (attributesNode != null)
                    {
                        foreach (string attributeName in casAttributesToCapture)
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
				HttpHelper.Logger.Log(NLog.LogLevel.Debug, "TaskCanceledException: " + ex.Message.ToString());
			} 

            return new ForbidResult();
        }
    }
}