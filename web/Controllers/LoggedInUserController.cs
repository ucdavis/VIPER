using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Controllers
{
    [Route("/api/loggedInUser")]
    //[Permission(Allow = "SVMSecure")]
    public class LoggedInUserController : ApiController
    {
        private readonly IAntiforgery _antiforgery;
        private readonly RAPSContext rapsContext;

        public LoggedInUserController(IAntiforgery antiforgery, RAPSContext rapsContext)
        {
            _antiforgery = antiforgery;
            this.rapsContext = rapsContext;
        }

        [HttpGet]
        public ActionResult<LoggedInUser?> GetLoggedInUser()
        {
            var userHelper = new UserHelper();
            var user = userHelper.GetCurrentUser();
            return user == null
                ? new LoggedInUser()
                : new LoggedInUser
                {
                    UserId = user.AaudUserId,
                    LoginId = user.LoginId,
                    MailId = user.MailId,
                    IamId = user.IamId,
                    MothraId = user.MothraId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = _antiforgery.GetAndStoreTokens(HttpContext).RequestToken,
                    Emulating = userHelper.IsEmulating()
                };
        }

        [HttpGet("permissions")]
        public ActionResult<List<string>> GetLoggedInUserPermissions(string? prefix = null)
        {
            var userHelper = new UserHelper();
            var user = userHelper.GetCurrentUser();
            return user == null
                ? new List<string>()
                : userHelper.GetAllPermissions(rapsContext, user)
                    .Where(p => prefix == null || p.Permission.StartsWith(prefix))
                    .Select(p => p.Permission)
                    .ToList();

        }
    }
}
