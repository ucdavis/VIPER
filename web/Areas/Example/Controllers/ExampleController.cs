using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Web.Authorization;

namespace Viper.Areas.Example.Controllers
{
    [Area("Example")]
    [Route("[area]/[action]")]
    [Authorize(Roles = "VMDO SVM-IT", Policy = "2faAuthentication")]
    [Permission(Allow = "SVMSecure")]
    public class ExampleController : AreaController
    {
        private readonly AAUDContext _AaudContext;
        public IUserHelper UserHelper;

        public ExampleController(AAUDContext context)
        {
            _AaudContext = context;
            UserHelper = new UserHelper();
        }

        public NavMenu Nav()
        {
            var nav = new List<NavMenuItem>
            {
                new NavMenuItem() { MenuItemText = "Example Header", IsHeader = true },
                new NavMenuItem() { MenuItemText = "Home", MenuItemURL = "~/Index" },
                new NavMenuItem() { MenuItemText = "Students", MenuItemURL = "~/Students" }
            };
            return new NavMenu("Example Application", nav);
        }

        public IActionResult Index()
        {
            ViewData["ViperLeftNav"] = Nav();
            
            var currentUser = UserHelper.GetCurrentUser();
            if (currentUser != null)
            {
                ViewData["UserList"] = _AaudContext.AaudUsers
                    .Include(u => u.ExampleComment)
                    .Where(u => u.DisplayFirstName == currentUser.DisplayFirstName)
                    .OrderBy(u => u.DisplayLastName)
                    .ThenBy(u => u.MailId)
                    .ToList();
            }
            return View("~/Areas/Example/Views/Index.cshtml");
        }

        [HttpPost]
        public ActionResult Index(string comment, int aaudUserId)
        {
            ExampleComment? existing = _AaudContext.ExampleComments.Find(aaudUserId);
            if(existing != null)
            {
                existing.Comment = comment;
            }
            else
            {
                ExampleComment c = new()
                {
                    AaudUserId = aaudUserId,
                    Comment = comment
                };
                _AaudContext.Add(c);
            }
            _AaudContext.SaveChanges();
            return Redirect("~/Example/Index");
        }
    }
}
