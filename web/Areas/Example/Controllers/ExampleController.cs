using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        //this is our connection to the AAUD database
        private readonly AAUDContext _AaudContext;
        //it might be helpful to have a UserHelper object throughout this class
        public IUserHelper UserHelper;

        //When the application creates this controller, it will pass in the 
        //AAUD database context
        public ExampleController(AAUDContext context)
        {
            _AaudContext = context;
            UserHelper = new UserHelper();
        }

        public NavMenu Nav()
        {
            //the NavMenu and NavMenuItem classes are in the \Classes folder
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
            //the Viper layout looks for this variable, and displays the nav if found
            ViewData["ViperLeftNav"] = Nav();

            var currentUser = UserHelper.GetCurrentUser();
            if (currentUser != null)
            {
                //Let's get records from the aaudUsers table that have the same first name
                //as the logged in user
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
            //See if a comment exists for this user
            ExampleComment? existing = _AaudContext.ExampleComments.Find(aaudUserId);
            if (existing != null)
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