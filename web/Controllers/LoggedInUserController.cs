using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Controllers
{
    [Route("/loggedInUser")]
    [Permission(Allow = "SVMSecure")]
    public class LoggedInUserController : ApiController
    {
        private readonly IAntiforgery _antiforgery;

        public LoggedInUserController(IAntiforgery antiforgery)
        {
            _antiforgery = antiforgery;
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
                    Token = _antiforgery.GetAndStoreTokens(HttpContext).RequestToken
                };
        }
    }
}
